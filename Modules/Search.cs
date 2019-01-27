using CommandLine;
using JetBrains.Annotations;
using NFive.PluginManager.Adapters;
using NFive.PluginManager.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Searches available NFive plugins.
	/// </summary>
	[UsedImplicitly]
	[Verb("search", HelpText = "Searches available NFive plugins.")]
	internal class Search
	{
		[PublicAPI]
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

			var nameLength = Math.Max(Math.Min(50, results.Max(d => d.Name.Length)), 10);
			var versionLength = Math.Max(Math.Min(20, results.Max(d => d.Versions.First().Version.ToString().Length)), 10);
			var descriptionLength = Math.Max(Math.Min(50, results.Max(d => (d.Description ?? string.Empty).Length)), 15);

			Console.WriteLine($"{"NAME".PadRight(nameLength)} | {"VERSION".PadRight(versionLength)} | {"DESCRIPTION".PadRight(descriptionLength)}");

			//var styleSheet = new StyleSheet(Color.LightGray);

			//if (this.Query.Any())
			//{
			//	var colors = new[]
			//	{
			//		Color.FromArgb(197, 15, 31),
			//		Color.FromArgb(193, 156, 0),
			//		Color.FromArgb(19, 161, 14),
			//		Color.FromArgb(58, 150, 221),
			//		Color.FromArgb(0, 55, 218),
			//		Color.FromArgb(136, 23, 152)
			//	};

			//	var i = 0;
			//	foreach (var term in this.Query)
			//	{
			//		styleSheet.AddStyle($"(?i){Regex.Escape(term)}", colors[i++ % colors.Length]);
			//	}
			//}

			foreach (var repository in results)
			{
				//Console.WriteLineStyled($"{repository.Name.Truncate(nameLength).PadRight(nameLength)} | {repository.Versions.First().Version.ToString().Truncate(versionLength).PadRight(versionLength)} | {repository.Description?.Truncate(descriptionLength).PadRight(descriptionLength)}", styleSheet);
				Console.WriteLine($"{repository.Name.Truncate(nameLength).PadRight(nameLength)} | {repository.Versions.First().Version.ToString().Truncate(versionLength).PadRight(versionLength)} | {repository.Description?.Truncate(descriptionLength).PadRight(descriptionLength)}");
			}

			return 0;
		}
	}
}
