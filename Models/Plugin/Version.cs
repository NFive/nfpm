using JetBrains.Annotations;

namespace NFive.PluginManager.Models.Plugin
{
	[PublicAPI]
	public class Version : SemVer.Version
	{
		public Version(string input, bool loose = false) : base(input, loose) { }

		public Version(int major, int minor, int patch, string preRelease = null, string build = null) : base(major, minor, patch, preRelease, build) { }

		public static implicit operator Version(string value) => new Version(value);
	}
}
