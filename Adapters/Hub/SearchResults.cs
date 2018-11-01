using System;
using System.Collections.Generic;
using Version = NFive.PluginManager.Models.Version;

namespace NFive.PluginManager.Adapters.Hub
{
	public class HubSearchResults
	{
		public List<HubSearchResult> Results { get; set; }
		public HubPagination Count { get; set; }
	}

	public class HubSearchResult
	{
		public string Name { get; set; }
		public string Owner { get; set; }
		public string GhUrl { get; set; }
		public string License { get; set; }
		public ulong Downloads { get; set; }
		public string Description { get; set; }
		public string Readme { get; set; }
		public List<HubShortVersion> Versions { get; set; }
		public DateTime Scraped { get; set; }
	}

	public class HubProject
	{
		public string Name { get; set; }
		public string Owner { get; set; }
		public string GhUrl { get; set; }
		public string License { get; set; }
		public ulong Downloads { get; set; }
		public string Description { get; set; }
		public string Readme { get; set; }
		public List<HubShortVersion> Versions { get; set; }
		public DateTime Scraped { get; set; }
	}

	public class HubShortVersion
	{
		public Version Version { get; set; }
		public string DownloadUrl { get; set; }
	}

	public class HubPagination
	{
		public ulong Total { get; set; }
		public ulong Page { get; set; }
		public ulong TotalPages { get; set; }
	}
}
