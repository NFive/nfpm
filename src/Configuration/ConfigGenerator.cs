using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace NFive.PluginManager.Configuration
{
	/// <summary>
	/// Represent and generate a FiveM server configuration file.
	/// </summary>
	public class ConfigGenerator
	{
		public string Endpoint { get; set; } = "[::]:30120";

		public string Hostname { get; set; } = "NFive";

		public string LicenseKey { get; set; } = null;

		public string SteamKey { get; set; } = "none";

		public string RconPassword { get; set; } = null;

		public ushort MaxPlayers { get; set; } = 32;

		public string Locale { get; set; } = "en-US";

		public bool OneSync { get; set; } = true;

		public bool ScriptHookAllowed { get; set; } = false;

		public List<string> Tags { get; set; } = new List<string> { "NFive" };

		[SuppressMessage("ReSharper", "StringLiteralTypo")]
		public void Serialize(string path)
		{
			var output = new StringBuilder();

			output.AppendLine($"endpoint_add_tcp \"{this.Endpoint}\"");
			output.AppendLine($"endpoint_add_udp \"{this.Endpoint}\"");
			output.AppendLine();
			output.AppendLine($"sv_hostname \"{this.Hostname}\"");
			output.AppendLine($"sets tags \"{string.Join(", ", this.Tags)}\"");
			output.AppendLine($"sv_maxclients {this.MaxPlayers}");
			output.AppendLine($"set locale \"{this.Locale}\"");
			output.AppendLine($"set onesync_enabled {this.OneSync.ToString().ToLowerInvariant()}");
			output.AppendLine();
			output.AppendLine($"sv_licensekey \"{this.LicenseKey}\"");
			output.AppendLine($"set steam_webApiKey \"{this.SteamKey}\"");
			output.AppendLine($"rcon_password \"{this.RconPassword}\"");
			output.AppendLine();
			output.AppendLine("sv_endpointPrivacy true");
			output.AppendLine("sv_enhancedHostSupport true");
			output.AppendLine($"sv_scriptHookAllowed {this.ScriptHookAllowed.ToString().ToLowerInvariant()}");
			output.AppendLine();
			output.AppendLine("start nfive");

			File.WriteAllText(path, output.ToString());
		}
	}
}
