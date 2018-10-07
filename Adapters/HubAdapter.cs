using Ionic.Zip;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NFive.PluginManager.Adapters.Hub;
using NFive.SDK.Plugins.Configuration;
using NFive.SDK.Plugins.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Version = NFive.SDK.Plugins.Models.Version;

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
			var releases = await GetReleases();
			var release = releases.First(r => r.Version == version);
			var file = Path.Combine(Environment.CurrentDirectory, ConfigurationManager.PluginPath, ".staging", this.name.Vendor, this.name.Project, Path.GetFileName(release.DownloadUrl));

			using (var client = new WebClient())
			{
				await client.DownloadFileTaskAsync(release.DownloadUrl, file);
			}

			using (var zip = ZipFile.Read(file))
			{
				zip.ExtractAll(Path.Combine(Environment.CurrentDirectory, ConfigurationManager.PluginPath, ".staging", this.name.Vendor, this.name.Project), ExtractExistingFileAction.OverwriteSilently);
			}

			File.Delete(file);
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
