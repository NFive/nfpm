using NFive.SDK.Plugins.Configuration;
using System;
using System.IO;

namespace NFive.PluginManager
{
	public class ResourceString
	{
		protected string Value;

		public ResourceString(string value)
		{
			this.Value = value;
		}

		public void Save(string path = null)
		{
			if (path == null) path = Path.Combine(Environment.CurrentDirectory, ConfigurationManager.ResourceFile);

			File.WriteAllText(path, this.Value);
		}

		public override string ToString() => this.Value;
	}
}
