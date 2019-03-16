using System.Collections.Generic;
using System.Threading.Tasks;
using NFive.PluginManager.Models;

namespace NFive.PluginManager.Adapters
{
	/// <summary>
	/// Download adapter for fetching plugins.
	/// </summary>
	public interface IDownloadAdapter
	{
		/// <summary>
		/// Gets the valid release versions.
		/// </summary>
		Task<IEnumerable<Version>> GetVersions();

		Task<string> Cache(Version version);

		/// <summary>
		/// Downloads and unpacks the specified plugin version.
		/// </summary>
		/// <param name="version">The version to download.</param>
		Task Download(Version version);
	}
}
