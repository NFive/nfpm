using CommandLine;
using NFive.PluginManager.Modules;
using System;
using System.Threading.Tasks;
using NFive.PluginManager.Utilities;

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
						Setup,
						Search,
						List,
						Install,
						Remove,
						Update,
						Outdated,
						SelfUpdate,
						Start,
						Scaffold,
						Rcon,
						Status,
						Migrate,
						CleanCache,
						Pack
					>(args)
					.MapResult(
						(Setup s) => s.Main(),
						(Search s) => s.Main(),
						(List l) => l.Main(),
						(Install i) => i.Main(),
						(Remove r) => r.Main(),
						(Update u) => u.Main(),
						(Outdated o) => o.Main(),
						(SelfUpdate s) => s.Main(),
						(Start s) => s.Main(),
						(Scaffold s) => s.Main(),
						(Rcon r) => r.Main(),
						(Status s) => s.Main(),
						(Migrate s) => s.Main(),
						(CleanCache c) => c.Main(),
						(Pack p) => p.Main(),
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
