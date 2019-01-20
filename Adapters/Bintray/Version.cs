using System;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace NFive.PluginManager.Adapters.Bintray
{
	public class Version
	{
		public string Name { get; set; }
		public string Desc { get; set; }
		public string Package { get; set; }
		public string Repo { get; set; }
		public string Owner { get; set; }
		public string[] Labels { get; set; }
		public bool Published { get; set; }
		public string[] AttributeNames { get; set; }
		public DateTime Created { get; set; }
		public DateTime Updated { get; set; }
		public string Released { get; set; }
		public double Ordinal { get; set; }
		public uint RatingCount { get; set; }

		public static async Task<Version> Get(string path)
		{
			using (var client = new WebClient())
			{
				var data = await client.DownloadStringTaskAsync($"https://bintray.com/api/v1/packages/{path}/versions/_latest");

				return JsonConvert.DeserializeObject<Version>(data, new JsonSerializerSettings
				{
					ContractResolver = new DefaultContractResolver
					{
						NamingStrategy = new SnakeCaseNamingStrategy()
					}
				});
			}
		}
	}
}
