using System;
using NFive.SDK.Plugins.Models;

namespace NFive.PluginManager.Adapters
{
	/// <summary>
	/// Builds a download adapter.
	/// </summary>
	public class AdapterBuilder
	{
		private readonly IDownloadAdapter adapter;

		/// <summary>
		/// Initializes a new instance of the <see cref="AdapterBuilder"/> class.
		/// </summary>
		/// <param name="name">The plugin name.</param>
		/// <param name="repo">The plugin repository.</param>
		/// <exception cref="ArgumentOutOfRangeException">Unknown repository type.</exception>
		public AdapterBuilder(Name name, Repository repo)
		{
			if (repo == null)
			{
				this.adapter = new HubAdapter(name);

				return;
			}

			switch (repo.Type)
			{
				case "local":
					this.adapter = new LocalAdapter(name, repo);
					break;

				case "hub":
					this.adapter = new HubAdapter(name);
					break;

				case "github":
					this.adapter = new GitHubAdapter(name);
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(repo), repo.Type, "Unknown repository type");
			}
		}

		/// <summary>
		/// The download adapter instance.
		/// </summary>
		/// <returns></returns>
		public IDownloadAdapter Adapter()
		{
			return this.adapter;
		}
	}
}
