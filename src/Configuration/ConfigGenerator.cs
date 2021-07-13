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

		public List<string> Tags { get; set; } = new List<string> { "NFive" };

		public string Locale { get; set; } = "en-US";

		public ushort MaxPlayers { get; set; } = 48;

		public bool OneSync { get; set; } = true;

		public string LicenseKey { get; set; } = null;

		public string SteamKey { get; set; } = "none";

		public string RconPassword { get; set; } = null;

		public bool ScriptHookAllowed { get; set; } = false;

		[SuppressMessage("ReSharper", "StringLiteralTypo")]
		public void Serialize(string path)
		{
			var output = new StringBuilder();

			output.AppendLine($"endpoint_add_tcp \"{this.Endpoint}\"");
			output.AppendLine($"endpoint_add_udp \"{this.Endpoint}\"");
			output.AppendLine();
			output.AppendLine($"sets sv_hostname \"{this.Hostname}\"");
			output.AppendLine($"sets sv_projectName \"{this.Hostname}\"");
			output.AppendLine($"sets sv_projectDesc \"{this.Hostname}\"");
			output.AppendLine($"sets tags \"{string.Join(", ", this.Tags)}\"");
			output.AppendLine($"sets locale \"{this.Locale}\"");
			output.AppendLine();
			output.AppendLine($"set onesync {this.OneSync.ToString().ToLowerInvariant()}");
			output.AppendLine($"set sv_maxclients {this.MaxPlayers}");
			output.AppendLine($"set sv_licensekey \"{this.LicenseKey}\"");
			output.AppendLine($"set steam_webApiKey \"{this.SteamKey}\"");
			output.AppendLine($"set rcon_password \"{this.RconPassword}\"");
			output.AppendLine($"set sv_scriptHookAllowed {this.ScriptHookAllowed.ToString().ToLowerInvariant()}");
			//output.AppendLine("set sv_endpointPrivacy true");
			//output.AppendLine("set sv_enhancedHostSupport true");
			output.AppendLine();
			output.AppendLine("start nfive");

			File.WriteAllText(path, output.ToString());
		}
	}
}
