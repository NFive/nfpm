using CommandLine;
using JetBrains.Annotations;
using NFive.PluginManager.Extensions;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NFive.PluginManager.Utilities;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Connect to a running FiveM server over RCON.
	/// </summary>
	[UsedImplicitly]
	[Verb("rcon", HelpText = "Connect to a running FiveM server over RCON.")]
	internal class Rcon
	{
		private Network.Rcon rcon;

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

		internal async Task<int> Main()
		{
			if (this.Password == null)
			{
				this.Password = Input.String("Password");
				Console.CursorTop--;
				Console.Write(new string(' ', Console.WindowWidth - 1));
				Console.CursorLeft = 0;
			}

			if (!this.Quiet) Console.WriteLine($"Connecting to {this.Host}:{this.Port}...");

			this.rcon = new Network.Rcon(Dns.GetHostEntry(this.Host).AddressList.First(a => a.AddressFamily == AddressFamily.InterNetwork), this.Port, this.Password);

			if (this.Command == null)
			{
				while (true)
				{
					if (await RunCommand(Input.String("#")))
					{
						return 1;
					}
				}
			}

			await RunCommand(this.Command);

			return 0;
		}

		private async Task<bool> RunCommand(string command)
		{
			var response = await this.rcon.Command(command, TimeSpan.FromSeconds(Math.Max(this.Timeout, 1)));

			var lines = response
				.Split('\n')
				.Select(l => l.TrimEnd("^7")) // Why FiveM? Why?
				.ToList();

			var output = string.Join(Environment.NewLine, lines); // Correct line endings

			if (lines.Any(l =>
				l.Equals("Invalid password.", StringComparison.InvariantCultureIgnoreCase) ||
				l.Equals("The server must set rcon_password to be able to use this command.", StringComparison.InvariantCultureIgnoreCase) ||
				l.StartsWith("No such command ", StringComparison.InvariantCultureIgnoreCase)
			))
			{
				Console.Write(output);

				return true;
			}

			Console.Write(output);

			return false;
		}
	}
}
