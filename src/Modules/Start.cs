using CommandLine;
using NFive.PluginManager.Extensions;
using NFive.PluginManager.Utilities;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Starts a local FiveM server.
	/// </summary>
	[Verb("start", HelpText = "Starts up an installed FiveM server.")]
	internal class Start : Module
	{
		private Process process;

		[Option('w', "window", Default = false, Required = false, HelpText = "Start server in separate window.")]
		public bool Window { get; set; } = false;

		public override async Task<int> Main()
		{
			var start = new ProcessStartInfo(Path.Combine(PathManager.FindServer(), PathManager.ServerFileWindows), $"+set citizen_dir citizen +exec {PathManager.ConfigFile}")
			{
				UseShellExecute = this.Window,
				RedirectStandardOutput = !this.Window,
				RedirectStandardError = !this.Window,
				ErrorDialog = false,
				WorkingDirectory = PathManager.FindServer()
			};

			if (!RuntimeEnvironment.IsWindows)
			{
				start = new ProcessStartInfo("sh", $"{Path.GetFullPath(Path.Combine(PathManager.FindServer(), "..", "..", "..", "run.sh"))} +exec {PathManager.ConfigFile}")
				{
					UseShellExecute = false,
					ErrorDialog = false,
					WorkingDirectory = PathManager.FindServer()
				};
			}

			using (this.process = new Process
			{
				StartInfo = start
			})
			{
				Console.WriteLine("Starting server...");

				if (this.Window)
				{
					this.process.Start();
					return 0;
				}

				Console.WriteLine("Press ", "Ctrl+C".Yellow(), " to exit");

				this.process.ErrorDataReceived += (s, e) => Console.WriteLine(e.Data);

				this.process.Start();

				if (RuntimeEnvironment.IsWindows)
				{
					this.process.BeginErrorReadLine();

					new Thread(() =>
					{
						char c;
						while (!this.process.HasExited && (c = (char)this.process.StandardOutput.Read()) >= 0)
						{
							System.Console.Write(c);
						}
					}).Start();
				}

				this.process.WaitForExit();
			}

			return await Task.FromResult(0);
		}
	}
}
