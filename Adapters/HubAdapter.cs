using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NFive.PluginManager.Adapters.Hub;
using NFive.PluginManager.Extensions;
using NFive.SDK.Core.Plugins;
using NFive.SDK.Plugins.Configuration;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Version = NFive.SDK.Core.Plugins.Version;

namespace NFive.PluginManager.Adapters
{
	/// <inheritdoc />
	/// <summary>
	/// Download adapter for fetching plugins from NFive Hub.
	/// </summary>
	/// <seealso cref="T:NFive.PluginManager.Adapters.IDownloadAdapter" />
	public class HubAdapter : IDownloadAdapter
	{
		private readonly Name name;

		/// <summary>
		/// Initializes a new instance of the <see cref="HubAdapter"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		public HubAdapter(Name name)
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

			return releases.Select(v => v.Version);
		}

		/// <inheritdoc />
		/// <summary>
		/// Downloads and unpacks the specified release version.
		/// </summary>
		/// <param name="version">The version to download.</param>
		public async Task Download(Version version)
		{
			var cacheDir = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nfpm", "cache", this.name.Vendor, this.name.Project, version.ToString()));
			var targetDir = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, ConfigurationManager.PluginPath, ".staging", this.name.Vendor, this.name.Project));

			if (cacheDir.Exists)
			{
				cacheDir.Copy(targetDir.FullName);

				return;
			}

			var releases = await GetReleases();
			var release = releases.First(r => r.Version.ToString() == version.ToString());
			var file = Path.Combine(targetDir.FullName, Path.GetFileName(release.DownloadUrl));

			using (var client = new WebClient())
			{
				await client.DownloadFileTaskAsync(release.DownloadUrl, file);
			}

			using (var zip = ZipArchive.Open(file))
			{
				zip.WriteToDirectory(targetDir.FullName, new ExtractionOptions { Overwrite = true });
			}

			File.Delete(file);

			targetDir.Copy(cacheDir.FullName);
		}

		public async Task<List<HubShortVersion>> GetReleases()
		{
			var result = await Get<HubProject>($"project/{this.name.Vendor}/{this.name.Project}.json");

			return result.Versions.OrderBy(v => v.Version.ToString()).ToList();
		}

		public static async Task<List<HubSearchResult>> Search(string query)
		{
			var results = new List<HubSearchResult>();
			ulong page = 1;
			HubSearchResults result;

			do
			{
				result = await Get<HubSearchResults>($"search.json?q={query}&page={page}");

				results.AddRange(result.Results);
			} while (result.Count.TotalPages >= ++page);

			return results;
		}

		private static async Task<T> Get<T>(string endpoint)
		{
			var serializer = new JsonSerializerSettings
			{
				ContractResolver = new DefaultContractResolver
				{
					NamingStrategy = new SnakeCaseNamingStrategy()
				}
			};

			using (var client = new WebClient())
			{
				var json = await client.DownloadStringTaskAsync($"https://hub.nfive.io/api/{endpoint}");

				return JsonConvert.DeserializeObject<T>(json, serializer);
			}
		}
	}
}
