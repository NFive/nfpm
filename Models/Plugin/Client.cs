using System.Collections.Generic;
using JetBrains.Annotations;

namespace NFive.PluginManager.Models.Plugin
{
	[PublicAPI]
	public class Client
	{
		public List<string> Main { get; set; }

		public List<string> Include { get; set; }

		public List<string> Files { get; set; }

		public string Loadscreen { get; set; }

		public string Ui { get; set; }
	}
}
