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

		[Option('S', "fivem-source", Required = false, HelpText = "Location of FiveM server core files.")]
		public string FiveMSource { get; set; } = "core";

		[Option('D', "fivem-data", Required = false, HelpText = "Location of FiveM server data files.")]
		public string FiveMData { get; set; } = "data";

		public override async Task<int> Main()
		{
			var serverDirectory = PathManager.FindServer(this.FiveMSource);
			var resourceDirectory = PathManager.FindResource(this.FiveMData);

			var start = new ProcessStartInfo(Path.Combine(serverDirectory, PathManager.ServerFileWindows), $@"+set citizen_dir {Path.Combine(serverDirectory, "citizen")} +exec {Path.Combine(resourceDirectory, "..", "..", PathManager.ConfigFile)}")
			{
				UseShellExecute = this.Window,
				RedirectStandardOutput = !this.Window,
				RedirectStandardError = !this.Window,
				ErrorDialog = false,
				WorkingDirectory = serverDirectory
			};

			if (!RuntimeEnvironment.IsWindows)
			{
				start = new ProcessStartInfo("sh", $"{Path.GetFullPath(Path.Combine(serverDirectory, FiveMData, "..", "..", "..", "run.sh"))} +exec {PathManager.ConfigFile}")
				{
					UseShellExecute = false,
					ErrorDialog = false,
					WorkingDirectory = PathManager.FindServer(FiveMSource)
				};
			}

			using (this.process = new Process
			{
				StartInfo = start
			})
			{
				Console.WriteLine("Starting server...");
				Console.WriteLine(resourceDirectory);

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
