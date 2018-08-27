using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Ionic.Zip;
using NFive.PluginManager.Models.Plugin;
using Octokit;
using Version = NFive.PluginManager.Models.Plugin.Version;

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

		/// <inheritdoc />
		/// <summary>
		/// Downloads and unpacks the specified release version.
		/// </summary>
		/// <param name="version">The version to download.</param>
		public async Task Download(Version version)
		{
			var releases = await GetReleases();

			var release = releases.First(r => !r.Prerelease && !r.Draft && r.Assets.Any(a => a.Name.EndsWith(".zip")));

			var asset = release.Assets.First(a => a.Name.EndsWith(".zip"));
			var file = Path.Combine(Environment.CurrentDirectory, Program.PluginPath, ".staging", this.name.Vendor, this.name.Project, asset.Name);

			using (var client = new WebClient())
			{
				await client.DownloadFileTaskAsync(asset.BrowserDownloadUrl, file);
			}

			using (var zip = ZipFile.Read(file))
			{
				zip.ExtractAll(Path.Combine(Environment.CurrentDirectory, Program.PluginPath, ".staging", this.name.Vendor, this.name.Project), ExtractExistingFileAction.OverwriteSilently);
			}

			File.Delete(file);
		}

		/// <summary>
		/// Gets the GitHub API client.
		/// </summary>
		private GitHubClient GetClient()
		{
			return new GitHubClient(new ProductHeaderValue("nfpm"));
		}

		/// <summary>
		/// Gets the available GitHub releases.
		/// </summary>
		private async Task<IReadOnlyList<Release>> GetReleases()
		{
			return await GetClient().Repository.Release.GetAll(this.name.Vendor, this.name.Project);
		}
	}
}
