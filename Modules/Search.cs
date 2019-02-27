using CommandLine;
using NFive.PluginManager.Adapters;
using NFive.PluginManager.Extensions;
using NFive.PluginManager.Utilities.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Searches available NFive plugins.
	/// </summary>
	[Verb("search", HelpText = "Searches available NFive plugins.")]
	internal class Search
	{
		[Value(0, Required = false, HelpText = "search query")]
		public IEnumerable<string> Query { get; set; }

		internal async Task<int> Main()
		{
			var results = await HubAdapter.Search(string.Join(" ", this.Query));

			if (results.Count < 1)
			{
				Console.WriteLine($"No matches found for \"{string.Join("\" \"", this.Query)}\"");

				return 1;
			}

			// TODO: Color keywords

			var nameLength = Math.Max(Math.Min(50, results.Max(d => d.Name.Length)), 10);
			var versionLength = Math.Max(Math.Min(20, results.Max(d => d.Versions.First().Version.ToString().Length)), 8);
			var descriptionLength = Math.Max(Math.Min(50, results.Max(d => (d.Description ?? string.Empty).Length)), 15);

			ColorConsole.WriteLine("NAME".PadRight(nameLength).White(), " | ", "VERSION".PadLeft(versionLength).White(), " | ", "DESCRIPTION".PadRight(descriptionLength).White());

			foreach (var repository in results)
			{
				ColorConsole.WriteLine(
					repository.Name.Truncate(nameLength).PadRight(nameLength),
					" | ",
					repository.Versions.First().Version.ToString().Truncate(versionLength).PadLeft(versionLength),
					" | ",
					(repository.Description ?? string.Empty).Truncate(descriptionLength).PadRight(descriptionLength)
				);
			}

			return 0;
		}
	}
}
