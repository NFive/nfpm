using CommandLine;
using System.Threading.Tasks;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Update installed NFive plugins.
	/// </summary>
	[Verb("update", HelpText = "Update installed NFive plugins.")]
	internal class Update
	{
		internal async Task<int> Main() => await Task.FromResult(0);
	}
}
