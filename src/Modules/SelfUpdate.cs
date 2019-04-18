using CommandLine;
using NFive.PluginManager.Extensions;
using NFive.PluginManager.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Version = NFive.PluginManager.Adapters.Bintray.Version;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Update nfpm.
	/// </summary>
	[Verb("self-update", HelpText = "Update nfpm.")]
	internal class SelfUpdate : Module
	{
		public override async Task<int> Main()
		{
			var asm = Assembly.GetEntryAssembly();
			var name = Path.GetFileName(asm.Location);
			var file = Path.GetFullPath(asm.Location);
			var fileVersion = ((AssemblyFileVersionAttribute)Attribute.GetCustomAttribute(asm, typeof(AssemblyFileVersionAttribute), false)).Version;

			Console.WriteLine("Currently running ", $"{name} {fileVersion}".White());
			Console.WriteLine("Checking for updates...");

			var version = (await Version.Get("nfive/nfpm/nfpm")).Name;

			if (version == fileVersion)
			{
				Console.WriteLine(name.White(), " is up to date");

				return 0;
			}

			Console.WriteLine("Updating ", name.White(), " to ", version.White(), "...");

			using (var client = new WebClient())
			{
				var data = await client.DownloadDataTaskAsync($"https://dl.bintray.com/nfive/nfpm/{version}/nfpm.exe");

				try
				{
					File.Delete($"{file}.old");

					if (RuntimeEnvironment.IsWindows)
					{
						File.Move(file, $"{file}.old");
					}

					File.WriteAllBytes(file, data);
				}
				catch (UnauthorizedAccessException) when (RuntimeEnvironment.IsWindows)
				{
					var process = new Process
					{
						StartInfo = new ProcessStartInfo
						{
							FileName = file,
							Arguments = "self-update -q",
							Verb = "runas",
							CreateNoWindow = false,
							WindowStyle = ProcessWindowStyle.Hidden
						}
					};

					process.Start();
					process.WaitForExit();
				}
			}

			Console.WriteLine("Update successful");

			return 0;
		}

		internal static void Cleanup()
		{
			File.Delete($"{Path.GetFullPath(Assembly.GetEntryAssembly().Location)}.old");
		}
	}
}
