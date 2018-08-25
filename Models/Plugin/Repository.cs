using JetBrains.Annotations;

namespace NFive.PluginManager.Models.Plugin
{
	[PublicAPI]
	public class Repository
	{
		public Name Name { get; set; }

		public string Type { get; set; }

		public string Path { get; set; }

		public string Url { get; set; }
	}
}
