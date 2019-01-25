using System;
using CommandLine;
using JetBrains.Annotations;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Version = NFive.PluginManager.Adapters.Bintray.Version;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Update nfpm.
	/// </summary>
	[UsedImplicitly]
	[Verb("self-update", HelpText = "Update nfpm.")]
	internal class SelfUpdate
	{
		internal async Task<int> Main()
		{
			var module = Process.GetCurrentProcess().MainModule;
			var file = Path.GetFullPath(module.FileName);
			var name = Path.GetFileName(module.FileName);

			Console.WriteLine($"Currently running {name} {module.FileVersionInfo.FileVersion}");
			Console.WriteLine("Checking for updates...");

			var version = (await Version.Get("nfive/nfpm/nfpm")).Name;

			if (version == module.FileVersionInfo.FileVersion)
			{
				Console.WriteLine($"{name} is up to date");

				return 0;
			}

			Console.WriteLine($"Updating {name} to {version}...");

			using (var client = new WebClient())
			{
				var data = await client.DownloadDataTaskAsync($"https://dl.bintray.com/nfive/nfpm/{version}/nfpm.exe");

				File.Delete($"{file}.old");
				File.Move(file, $"{file}.old");

				File.WriteAllBytes(file, data);
			}

			Console.WriteLine("Update successful");

			return 0;
		}

		internal static void Cleanup()
		{
			File.Delete($"{Path.GetFullPath(Process.GetCurrentProcess().MainModule.FileName)}.old");
		}
	}
}
