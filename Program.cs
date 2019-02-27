using CommandLine;
using NFive.PluginManager.Modules;
using NFive.PluginManager.Utilities;
using System;
using System.Threading.Tasks;

namespace NFive.PluginManager
{
	/// <summary>
	/// Application entry-point.
	/// </summary>
	public static class Program
	{
		/// <summary>
		/// Initializes the <see cref="Program"/> class.
		/// </summary>
		static Program()
		{
			// Load embedded resources
			CosturaUtility.Initialize();
		}

		/// <summary>
		/// Application entry-point.
		/// </summary>
		/// <param name="args">The application arguments.</param>
		/// <returns>Exit status code.</returns>
		[STAThread]
		public static async Task<int> Main(string[] args)
		{
			try
			{
				// Remove old copy of self after an update
				SelfUpdate.Cleanup();
			}
			catch
			{
				// ignored
			}

			try
			{
				NetworkUtilities.ConfigureSupportedSecurityProtocols();
			}
			catch
			{
				// ignored
			}

			try
			{
				NetworkUtilities.SetConnectionLimit();
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
						CleanCache,
						Install,
						List,
						Migrate,
						Outdated,
						Pack,
						Rcon,
						Remove,
						Scaffold,
						Search,
						SelfUpdate,
						Setup,
						Start,
						Status,
						Update
					>(args)
					.MapResult(
						(CleanCache m) => m.Main(),
						(Install m) => m.Main(),
						(List m) => m.Main(),
						(Migrate m) => m.Main(),
						(Outdated m) => m.Main(),
						(Pack m) => m.Main(),
						(Rcon m) => m.Main(),
						(Remove m) => m.Main(),
						(Scaffold m) => m.Main(),
						(Search m) => m.Main(),
						(SelfUpdate m) => m.Main(),
						(Setup m) => m.Main(),
						(Start m) => m.Main(),
						(Status m) => m.Main(),
						(Update m) => m.Main(),
						e => Task.FromResult(1)
					);
			}
			catch (Exception ex)
			{
				Console.WriteLine("An unhandled application error has occured:");
				Console.WriteLine(ex.Message);
				if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message);

				return 1;
			}
		}
	}
}
