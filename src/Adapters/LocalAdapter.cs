using NFive.SDK.Core.Plugins;
using NFive.SDK.Plugins.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NFive.PluginManager.Utilities;
using Plugin = NFive.SDK.Plugins.Plugin;
using Version = NFive.PluginManager.Models.Version;

namespace NFive.PluginManager.Adapters
{
	/// <inheritdoc />
	/// <summary>
	/// Download adapter for fetching local plugins.
	/// </summary>
	/// <seealso cref="T:NFive.PluginManager.Adapters.IDownloadAdapter" />
	public class LocalAdapter : IDownloadAdapter
	{
		private readonly Name name;
		private readonly Repository repo;

		/// <summary>
		/// Initializes a new instance of the <see cref="LocalAdapter"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="repo">The repo.</param>
		public LocalAdapter(Name name, Repository repo)
		{
			this.name = name;
			this.repo = repo;
		}

		/// <inheritdoc />
		/// <summary>
		/// Gets the valid release versions.
		/// </summary>
		/// <exception cref="T:System.IO.FileNotFoundException">Unable to find definition file</exception>
		public async Task<IEnumerable<Version>> GetVersions()
		{
			var path = Path.Combine(Path.GetFullPath(this.repo.Path), ConfigurationManager.DefinitionFile);

			if (!File.Exists(path)) throw new FileNotFoundException("Unable to find definition file", path);

			var definition = Plugin.Load(path);

			if (definition.Version == null) return new List<Version> { new Version("*") };

			return await Task.FromResult(new List<Version> { new Version(definition.Version.ToString()) });
		}

		public Task<string> Cache(Version version) => Task.FromResult(Path.Combine(Path.GetFullPath(this.repo.Path), this.repo.Path));

		/// <inheritdoc />
		/// <summary>
		/// Downloads and unpacks the specified plugin version.
		/// </summary>
		/// <param name="version">The version to download.</param>
		public async Task Download(Version version)
		{
			var src = Path.Combine(Path.GetFullPath(this.repo.Path), this.repo.Path);
			var dst = Path.Combine(Environment.CurrentDirectory, ConfigurationManager.PluginPath, this.name.Vendor, this.name.Project);

			var path = Path.Combine(Path.GetFullPath(this.repo.Path), ConfigurationManager.DefinitionFile);
			if (!File.Exists(path)) throw new FileNotFoundException("Unable to find definition file", path);

			var definition = Plugin.Load(path);

			var stockFiles = new List<string>
			{
				"nfive.yml",
				"nfive.lock",
				"README.md",
				"README",
				"LICENSE.md",
				"LICENSE"
			};

			var files = new List<string>();

			if (definition.Client != null)
			{
				if (definition.Client.Main != null && definition.Client.Main.Any())
				{
					files.AddRange(definition.Client.Main.Select(f => $"{f}.net.dll"));
				}

				if (definition.Client.Include != null && definition.Client.Include.Any())
				{
					files.AddRange(definition.Client.Include.Select(f => $"{f}.net.dll"));
				}

				if (definition.Client.Files != null && definition.Client.Files.Any())
				{
					files.AddRange(definition.Client.Files);
				}

				if (definition.Client.Overlays != null && definition.Client.Overlays.Any())
				{
					files.AddRange(definition.Client.Overlays);
				}
			}

			if (definition.Server != null)
			{
				if (definition.Server.Main != null && definition.Server.Main.Any())
				{
					files.AddRange(definition.Server.Main.Select(f => $"{f}.net.dll"));
				}

				if (definition.Server.Include != null && definition.Server.Include.Any())
				{
					files.AddRange(definition.Server.Include.Select(f => $"{f}.net.dll"));
				}
			}

			files = files.Distinct().ToList();

			foreach (var file in stockFiles)
			{
				if (!File.Exists(Path.Combine(src, file))) continue;

				File.Copy(Path.Combine(src, file), Path.Combine(dst, file), true);
			}

			foreach (var file in files)
			{
				Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(dst, file).Replace(Path.DirectorySeparatorChar, '/')) ?? throw new InvalidOperationException());

				File.Copy(Path.Combine(src, file).Replace(Path.DirectorySeparatorChar, '/'), Path.Combine(dst, file).Replace(Path.DirectorySeparatorChar, '/'), true);
			}

			if (!PathManager.IsServerInstall()) return;

			var pluginConfigDir = new DirectoryInfo(Path.Combine(src, ConfigurationManager.ConfigurationPath));
			if (!pluginConfigDir.Exists) return;
			var targetConfigDir = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, ConfigurationManager.ConfigurationPath, this.name.Vendor, this.name.Project));

			foreach (var file in pluginConfigDir.EnumerateFiles())
			{
				var targetFile = Path.Combine(targetConfigDir.FullName, file.Name);
				if (File.Exists(targetFile)) continue;
				if (!targetConfigDir.Exists) targetConfigDir.Create();
				file.CopyTo(targetFile);
			}

			await Task.FromResult(0);
		}
	}
}
