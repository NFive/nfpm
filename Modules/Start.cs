using CommandLine;
using JetBrains.Annotations;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Console = Colorful.Console;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Starts a local FiveM server.
	/// </summary>
	[UsedImplicitly]
	[Verb("start", HelpText = "Starts up an installed FiveM server.")]
	internal class Start
	{
		private Process process;

		[Option('w', "window", Default = false, Required = false, HelpText = "Start server in separate window.")]
		public bool Window { get; set; } = false;

		internal async Task<int> Main()
		{
			using (this.process = new Process
			{
				StartInfo = new ProcessStartInfo(Path.Combine(PathManager.FindServer(), PathManager.ServerFile), $"+set citizen_dir citizen +exec {PathManager.ConfigFile}")
				{
					UseShellExecute = this.Window,
					RedirectStandardOutput = !this.Window,
					RedirectStandardError = !this.Window,
					ErrorDialog = false,
					WorkingDirectory = PathManager.FindServer(),
				}
			})
			{
				Console.WriteLine("Starting server...", Color.Green);

				if (this.Window)
				{
					this.process.Start();
					return 0;
				}

				Console.WriteLine("Press Ctrl+C to exit");

				this.process.ErrorDataReceived += (s, e) => Console.WriteLine(e.Data, Color.Red);

				this.process.Start();
				this.process.BeginErrorReadLine();

				new Thread(() =>
				{
					char c;
					while (!this.process.HasExited && (c = (char)this.process.StandardOutput.Read()) >= 0)
					{
						Console.Write(c);
					}
				}).Start();

				this.process.WaitForExit();
			}

			return await Task.FromResult(0);
		}
	}
}
