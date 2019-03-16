using NFive.SDK.Core.Plugins;
using System;
using System.Linq;

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
			switch (repo?.Type)
			{
				case "local":
					this.adapter = new LocalAdapter(name, repo);
					break;

				case null:
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

		public AdapterBuilder(Name name, Plugin plugin) : this(name, plugin.Repositories?.FirstOrDefault(r => r.Name == name)) { }

		/// <summary>
		/// The download adapter instance.
		/// </summary>
		/// <returns></returns>
		public IDownloadAdapter Adapter() => this.adapter;
	}
}
