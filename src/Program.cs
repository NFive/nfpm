using CommandLine;
using NFive.PluginManager.Exceptions;
using NFive.PluginManager.Extensions;
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
						(Module m) => m.Main(),
						e => Task.FromResult(1)
					);
			}
			catch (DefinitionLoadException)
			{
				Console.WriteLine("NFive installation or plugin not found.".DarkRed());
				Console.WriteLine("Use ", "nfpm setup".Yellow(), " to install NFive in this directory.");

				return 1;
			}
			catch (GraphLoadException ex)
			{
				Console.WriteLine("Unable to build definition graph (PANIC):".DarkRed());
				Console.WriteLine(ex.Message.Red());
				if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message.Red());

				return 1;
			}
			catch (Exception ex)
			{
				Console.WriteLine("An unhandled application error has occured:".DarkRed());
				Console.WriteLine(ex.Message.Red());
				if (ex.InnerException != null) Console.WriteLine(ex.InnerException.Message.Red());

				return 1;
			}
		}
	}
}
