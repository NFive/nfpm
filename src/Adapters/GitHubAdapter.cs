using NFive.PluginManager.Extensions;
using NFive.PluginManager.Utilities;
using NFive.SDK.Core.Plugins;
using NFive.SDK.Plugins.Configuration;
using Octokit;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Version = NFive.PluginManager.Models.Version;

namespace NFive.PluginManager.Adapters
{
	/// <inheritdoc />
	/// <summary>
	/// Download adapter for fetching plugins from GitHub releases.
	/// </summary>
	/// <seealso cref="T:NFive.PluginManager.Adapters.IDownloadAdapter" />
	public class GitHubAdapter : IDownloadAdapter
	{
		private readonly Name name;

		/// <summary>
		/// Initializes a new instance of the <see cref="GitHubAdapter"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		public GitHubAdapter(Name name)
		{
			this.name = name;
		}

		/// <inheritdoc />
		/// <summary>
		/// Gets the valid release versions.
		/// </summary>
		public async Task<IEnumerable<Version>> GetVersions()
		{
			var releases = await GetReleases();

			return releases
				.Where(r => !r.Prerelease && !r.Draft && r.Assets.Any(a => a.Name.EndsWith(".zip")))
				.Select(r => new Version(r.TagName))
				.OrderBy(v => v.ToString());
		}

		public async Task<string> Cache(Version version)
		{
			var cacheDir = new DirectoryInfo(Path.Combine(PathManager.CachePath, this.name.Vendor, this.name.Project, version.ToString()));
			if (cacheDir.Exists) return cacheDir.FullName;

			cacheDir.Create();

			var releases = await GetReleases();
			var release = releases.First(r => !r.Prerelease && !r.Draft && r.Assets.Any(a => a.Name.EndsWith(".zip")));
			var asset = release.Assets.First(a => a.Name.EndsWith(".zip"));

			var file = Path.Combine(cacheDir.FullName, asset.Name);

			using (var client = new WebClient())
			{
				await client.DownloadFileTaskAsync(asset.BrowserDownloadUrl, file);
			}

			using (var zip = ZipArchive.Open(file))
			{
				zip.WriteToDirectory(cacheDir.FullName, new ExtractionOptions
				{
					Overwrite = true,
					ExtractFullPath = true
				});
			}

			File.Delete(file);

			return cacheDir.FullName;
		}

		public async Task Download(Version version)
		{
			var cacheDir = new DirectoryInfo(Path.Combine(PathManager.CachePath, this.name.Vendor, this.name.Project, version.ToString()));
			var targetDir = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, ConfigurationManager.PluginPath, this.name.Vendor, this.name.Project));

			if (!cacheDir.Exists)
			{
				await Cache(version);
			}

			targetDir.Copy(cacheDir.FullName);
		}

		/// <summary>
		/// Gets the GitHub API client.
		/// </summary>
		private static GitHubClient GetClient() => new GitHubClient(new ProductHeaderValue("nfpm"));

		/// <summary>
		/// Gets the available GitHub releases.
		/// </summary>
		private async Task<IReadOnlyList<Release>> GetReleases() => await GetClient().Repository.Release.GetAll(this.name.Vendor, this.name.Project);
	}
}
