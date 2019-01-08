using CommandLine;
using JetBrains.Annotations;
using System.Threading.Tasks;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Update installed NFive plugins.
	/// </summary>
	[UsedImplicitly]
	[Verb("update", HelpText = "Update installed NFive plugins.")]
	internal class Update
	{
		internal async Task<int> Main()
		{
			return await Task.FromResult(0);
		}
	}
}
