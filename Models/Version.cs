namespace NFive.PluginManager.Models
{
	public class Version : SDK.Core.Plugins.Version
	{
		public Version(string input)
		{
			var version = new SemVer.Version(input);

			this.Major = version.Major;
			this.Minor = version.Minor;
			this.Patch = version.Patch;
			this.PreRelease = version.PreRelease;
			this.Build = version.Build;
		}

		public static implicit operator Version(string value) => new Version(value);

		public static implicit operator string(Version value) => value.ToString();
	}
}
