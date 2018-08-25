using System.Collections.Generic;
using JetBrains.Annotations;

namespace NFive.PluginManager.Models.Plugin
{
	[PublicAPI]
	public class Server
	{
		public List<string> Main { get; set; }

		public List<string> Include { get; set; }
	}
}
