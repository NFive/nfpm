using CommandLine;
using NFive.PluginManager.Extensions;
using NFive.PluginManager.Utilities;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Connect to a running FiveM server over RCON.
	/// </summary>
	[Verb("rcon", HelpText = "Connect to a running FiveM server over RCON.")]
	internal class Rcon : Module
	{
		[Option('h', "host", Default = "localhost", Required = false, HelpText = "Remote server host.")]
		public string Host { get; set; }

		[Option('p', "port", Default = 30120, Required = false, HelpText = "Remote server port.")]
		public int Port { get; set; }

		[Option("password", Required = false, HelpText = "Remote server password, if unset will be prompted.")]
		public string Password { get; set; }

		[Option('t', "timeout", Required = false, Default = 5, HelpText = "Connection timeout in seconds.")]
		public int Timeout { get; set; }

		[Option('q', "quiet", Required = false, HelpText = "Less verbose output.")]
		public bool Quiet { get; set; } = false;

		[Value(0, Required = false, HelpText = "Command to run on the remote server, if unset will be interactive.")]
		public string Command { get; set; }

		internal override async Task<int> Main()
		{
			if (string.IsNullOrEmpty(this.Password))
			{
				this.Password = Input.Password("Password");

				System.Console.CursorTop = 0;
				System.Console.CursorLeft = 0;
				System.Console.Write(new string(' ', System.Console.WindowWidth - 1));
				System.Console.CursorLeft = 0;
			}

			if (!this.Quiet) Console.WriteLine("Connecting to ", $"{this.Host}:{this.Port}".White(), "...");

			var result = 1;

			using (var rcon = new Network.Rcon(Dns.GetHostEntry(this.Host).AddressList.First(a => a.AddressFamily == AddressFamily.InterNetwork), this.Port, this.Password))
			{
				if (this.Command == null)
				{
					var connected = !await RunCommand(rcon, "version", false);

					while (connected && !await RunCommand(rcon, Input.String("#"))) { }
				}
				else
				{
					result = await RunCommand(rcon, this.Command) ? 1 : 0;
				}
			}

			if (!this.Quiet) Console.WriteLine("Closing connection to server");

			return result;
		}

		private async Task<bool> RunCommand(Network.Rcon rcon, string command, bool output = true)
		{
			try
			{
				var response = await rcon.Command(command, TimeSpan.FromSeconds(Math.Max(this.Timeout, 1)));

				if (response.Equals($"Invalid password.{Environment.NewLine}", StringComparison.InvariantCulture) ||
					response.Equals($"The server must set rcon_password to be able to use this command.{Environment.NewLine}", StringComparison.InvariantCulture) ||
					response.StartsWith("No such command ", StringComparison.InvariantCulture))
				{
					Console.Write(response.DarkRed());

					return !response.StartsWith("No such command ", StringComparison.InvariantCulture);
				}

				if (output) Console.Write(response);

				return false;
			}
			catch (TimeoutException ex)
			{
				Console.WriteLine("Unable to communicate with ".DarkRed(), $"{this.Host}:{this.Port}".Red(), $": {ex.Message}".DarkRed());

				return true;
			}
		}
	}
}
