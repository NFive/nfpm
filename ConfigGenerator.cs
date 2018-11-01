using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NFive.PluginManager
{
	/// <summary>
	/// Represent and generate a FiveM server configuration file.
	/// </summary>
	public class ConfigGenerator
	{
		public string Endpoint { get; set; } = "0.0.0.0:30120";

		public string Hostname { get; set; } = "NFive";

		public string LicenseKey { get; set; } = null;

		public string RconPassword { get; set; } = null;

		public ushort MaxPlayers { get; set; } = 32;

		public bool ScriptHookAllowed { get; set; } = false;

		public ushort AuthMaxVariance { get; set; } = 1;

		public ushort AuthMinTrust { get; set; } = 5;

		public List<string> Tags { get; set; } = new List<string> { "nfive" };

		public void Serialize(string path)
		{
			var output = new StringBuilder();

			WriteLine(ref output, $"endpoint_add_tcp \"{this.Endpoint}\"");
			WriteLine(ref output, $"endpoint_add_udp \"{this.Endpoint}\"");
			WriteLine(ref output);
			WriteLine(ref output, $"sv_hostname \"{this.Hostname}\"");
			WriteLine(ref output, $"rcon_password \"{this.RconPassword}\"");
			WriteLine(ref output);
			WriteLine(ref output, $"sets tags \"{string.Join(", ", this.Tags)}\"");
			WriteLine(ref output, $"sv_maxclients {this.MaxPlayers}");
			WriteLine(ref output);
			WriteLine(ref output, $"sv_licensekey {this.LicenseKey}");
			WriteLine(ref output);
			WriteLine(ref output, "sv_endpointPrivacy true");
			WriteLine(ref output, "sv_enhancedHostSupport true");
			WriteLine(ref output, $"sv_scriptHookAllowed {this.ScriptHookAllowed.ToString().ToLowerInvariant()}");
			WriteLine(ref output);
			WriteLine(ref output, $"sv_authMaxVariance {this.AuthMaxVariance}");
			WriteLine(ref output, $"sv_authMinTrust {this.AuthMinTrust}");
			WriteLine(ref output);
			WriteLine(ref output, "start nfive");

			File.WriteAllText(path, output.ToString());
		}

		private static void WriteLine(ref StringBuilder builder, string line = "")
		{
			builder.AppendLine(line);
		}
	}
}
