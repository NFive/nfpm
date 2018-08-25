using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using NFive.PluginManager.Configuration;
using YamlDotNet.Serialization;

namespace NFive.PluginManager.Models.Plugin
{
	[PublicAPI]
	public class Definition
	{
		public Name Name { get; set; }

		public Version Version { get; set; }

		[YamlIgnore]
		public string FullName => $"{this.Name}@{this.Version}";

		public string Description { get; set; }

		public string Author { get; set; }

		public string License { get; set; }

		public string Website { get; set; }

		public Server Server { get; set; }

		public Client Client { get; set; }

		public Dictionary<Name, VersionRange> Dependencies { get; set; }

		public List<Definition> DependencyNodes { get; set; }

		public List<Repository> Repositories { get; set; }

		public static Definition Load(string path = Program.DefinitionFile)
		{
			if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
			if (!File.Exists(path)) throw new FileNotFoundException("Unable to find the plugin definition file", path);

			return Yaml.Deserialize<Definition>(File.ReadAllText(path));
		}
	}
}
