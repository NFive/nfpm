using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;
using NFive.PluginManager.Adapters;
using NFive.PluginManager.Models.Plugin;
using Octokit;
using Console = Colorful.Console;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Searches available plugins.
	/// </summary>
	[UsedImplicitly]
	[Verb("search", HelpText = "Searches available NFive plugins.")]
	internal class Search
	{
		[PublicAPI]
		[Value(0, Required = false, HelpText = "search query")]
		public string Query { get; set; }

		internal async Task<int> Main()
		{
			var gh = new GitHubClient(new ProductHeaderValue("nfpm"));
			var results = await gh.Search.SearchRepo(new SearchRepositoriesRequest($"{this.Query} topic:nfive-plugin&sort=stars&order=desc"));

			if (results.TotalCount < 1)
			{
				Console.WriteLine($"No matches found for \"{this.Query}\"");
			}
			else
			{
				Console.WriteLine($"{"NAME",-25} | {"VERSION",-8} | {"DESCRIPTION",-20} | {"AUTHOR",-15} | {"URL",-45}");

				foreach (var repository in results.Items)
				{
					var adapter = new GitHubAdapter(new Name(repository.FullName));
					var versions = await adapter.GetVersions();

					Console.WriteLine($"{repository.Name,-25} | {versions.First(),-8} | {repository.Description,-20} | {repository.Owner.Login,-15} | {repository.HtmlUrl,-45}");
				}
			}

			return await Task.FromResult(0);
		}
	}
}
