using System;
using System.IO;
using NFive.SDK.Plugins.Configuration;

namespace NFive.PluginManager.Configuration
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
