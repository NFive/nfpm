using CommandLine;
using JetBrains.Annotations;
using System.Collections.Generic;
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
	internal class Startv2
	{
		private bool prompt = false;
		private Process process;
		private List<string> ignore = new List<string>
		{
			"INFO: No channel links found in configuration file.\r\n",
			"Couldn't find resource sessionmanager.\r\n",
			"Instantiated instance of script NFive.Server.ConfigurationManager.\r\n",
			"Started resource nfive\r\n",
			"Authenticating server license key...\r\n",
			"Sending heartbeat to live-internal.fivem.net:30110\r\n",
			"Server license key authentication succeeded. Welcome!\r\n"
		};

		internal async Task<int> Main()
		{
			using (this.process = new Process
			{
				StartInfo = new ProcessStartInfo(Path.Combine(PathManager.FindServer(), PathManager.ServerFile), $"+set citizen_dir citizen +exec {PathManager.ConfigFile}")
				{
					UseShellExecute = false,
					//CreateNoWindow = true,
					//RedirectStandardInput = true,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					ErrorDialog = false,
					WindowStyle = ProcessWindowStyle.Hidden,
					WorkingDirectory = PathManager.FindServer(),
				}
			})
			{
				Console.WriteLine("Starting server...", Color.Green);

				//this.process.ErrorDataReceived += (s, e) => Console.WriteLine(e.Data, Color.Red);

				this.process.Start();
				//this.process.BeginOutputReadLine();
				//this.process.BeginErrorReadLine();

				new Thread(() =>
				{
					char ch;
					string buffer = "";
					string command = "";
					bool prompt = false;

					while (!this.process.HasExited && (ch = (char)this.process.StandardOutput.Read()) >= 0)
					{
						buffer += ch;

						if (prompt)
						{
							if (buffer.StartsWith("cfx> "))
							{
								command += ch;
							}

							if (buffer.EndsWith("\r\n"))
							{
								buffer = "";
								command = "";
							}

							Console.Write(ch);
							continue;
						}

						if (buffer.StartsWith("cfx> "))
						{
							Console.Write(buffer);
							buffer = "";
							prompt = true;
							continue;
						}

						if (buffer.EndsWith("\r\n"))
						{
							if (this.ignore.Contains(buffer))
							{
								buffer = "";
								continue;
							}

							Console.Write(buffer);
							buffer = "";
						}

						//Console.Write(ch);
					}
				}).Start();

				this.process.WaitForExit();
				this.process.Kill();
			}

			return await Task.FromResult(0);
		}
	}
}
