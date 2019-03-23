using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NFive.PluginManager.Adapters.Hub;
using NFive.PluginManager.Extensions;
using NFive.PluginManager.Utilities;
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
using Version = NFive.PluginManager.Models.Version;

namespace NFive.PluginManager.Adapters
{
	/// <inheritdoc />
	/// <summary>
	/// Download adapter for fetching plugins from NFive Hub.
	/// </summary>
	/// <seealso cref="T:NFive.PluginManager.Adapters.IDownloadAdapter" />
	public class HubAdapter : IDownloadAdapter
	{
		private static readonly Dictionary<Name, List<HubShortVersion>> CachedReleases = new Dictionary<Name, List<HubShortVersion>>();

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
		/// <returns>List of release versions.</returns>
		public async Task<IEnumerable<Version>> GetVersions()
		{
			var releases = await GetReleases();

			return releases.Select(v => v.Version);
		}

		public async Task Download(Version version)
		{
			var cacheDir = new DirectoryInfo(Path.Combine(PathManager.CachePath, ConfigurationManager.PluginPath, this.name.Vendor, this.name.Project, version.ToString()));
			var targetDir = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, ConfigurationManager.PluginPath, this.name.Vendor, this.name.Project));

			if (!cacheDir.Exists)
			{
				await Cache(version);
			}

			cacheDir.Copy(targetDir.FullName);
		}

		/// <summary>
		/// Ensures the specified version exists in the local cache, downloading it if necessary.
		/// </summary>
		/// <param name="version">The version to cache.</param>
		/// <returns>The full path to the local cache directory.</returns>
		public async Task<string> Cache(Version version)
		{
			var cacheDir = new DirectoryInfo(Path.Combine(PathManager.CachePath, ConfigurationManager.PluginPath, this.name.Vendor, this.name.Project, version.ToString()));
			if (cacheDir.Exists) return cacheDir.FullName;

			cacheDir.Create();

			var releases = await GetReleases();
			var release = releases.First(r => r.Version.ToString() == version.ToString()); // TODO: IComparable

			var file = Path.Combine(cacheDir.FullName, Path.GetFileName(release.DownloadUrl));

			using (var client = new WebClient())
			{
				await client.DownloadFileTaskAsync(release.DownloadUrl, file);
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

		private async Task<List<HubShortVersion>> GetReleases()
		{
			if (CachedReleases.ContainsKey(this.name)) return CachedReleases[this.name];

			var result = await Get<HubProject>($"project/{this.name.Vendor}/{this.name.Project}.json");
			var releases = result.Versions.OrderBy(v => v.Version.ToString()).ToList();

			CachedReleases.Add(this.name, releases);

			return releases;
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
				Console.WriteLine($"https://hub.nfive.io/api/{endpoint}".DarkGray());

				var json = await client.DownloadStringTaskAsync($"https://hub.nfive.io/api/{endpoint}");

				return JsonConvert.DeserializeObject<T>(json, serializer);
			}
		}
	}
}
