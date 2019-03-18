namespace NFive.PluginManager.Models
{
	public class Version : SDK.Core.Plugins.Version
	{
		public Version(string input)
		{
			var version = new SemVer.Version(input, true);

			this.Major = version.Major;
			this.Minor = version.Minor;
			this.Patch = version.Patch;
			this.PreRelease = version.PreRelease;
			this.Build = version.Build;
		}

		public Version(int major, int minor, int patch, string preRelease = null, string build = null)
		{
			var version = new SemVer.Version(major, minor, patch, preRelease, build);

			this.Major = version.Major;
			this.Minor = version.Minor;
			this.Patch = version.Patch;
			this.PreRelease = version.PreRelease;
			this.Build = version.Build;
		}

		public static implicit operator Version(string value) => new Version(value);

		public static implicit operator string(Version value) => value.ToString();

		public static implicit operator SemVer.Version(Version value) => new SemVer.Version(value.Major, value.Minor, value.Patch, value.PreRelease, value.Build);

		public static implicit operator Version(SemVer.Version value) => new Version(value.ToString());
	}
}
