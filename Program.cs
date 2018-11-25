using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using CommandLine;
using NFive.PluginManager.Modules;
using Console = Colorful.Console;

namespace NFive.PluginManager
{
	/// <summary>
	/// Application entry-point.
	/// </summary>
	public static class Program
	{
		/// <summary>
		/// Application entry-point.
		/// </summary>
		/// <param name="args">The application arguments.</param>
		/// <returns>Exit status code.</returns>
		public static async Task<int> Main(string[] args)
		{
			try
			{
				File.Delete("nfpm.exe.old");
			}
			catch
			{
				// ignored
			}

			try
			{
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
			}
			catch
			{
				// ignored
			}

			try
			{
				return await Parser
					.Default
					.ParseArguments<
						Setup,
						Init,
						Search,
						List,
						Install,
						Remove,
						//Update,
						SelfUpdate,
						Start,
						Scaffold,
						Modules.Rcon,
						Status
					>(args)
					.MapResult(
						(Setup s) => s.Main(),
						(Init i) => i.Main(),
						(Search s) => s.Main(),
						(List l) => l.Main(),
						(Install i) => i.Main(),
						(Remove r) => r.Main(),
						//(Update u) => u.Main(),
						(SelfUpdate s) => s.Main(),
						(Start s) => s.Main(),
						(Scaffold s) => s.Main(),
						(Modules.Rcon r) => r.Main(),
						(Status s) => s.Main(),
						e => Task.FromResult(1)
					);
			}
			catch (Exception ex)
			{
				Console.WriteLine("An unhandled application error has occured:", Color.Red);
				Console.WriteLine(ex.Message);
				if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message);

				return 1;
			}
		}
	}
}
