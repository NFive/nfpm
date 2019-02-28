using System;
using NFive.SDK.Core.Plugins;
using SemVer;
using Version = NFive.SDK.Core.Plugins.Version;

namespace NFive.PluginManager.Extensions
{
	public static class VersionRangeExtensions
	{
		[Obsolete]
		public static bool IsSatisfied(this VersionRange target, string version) => new Range(target.Value).IsSatisfied(version);
		public static bool IsSatisfied(this VersionRange target, Version version) => new Range(target.Value).IsSatisfied(new SemVer.Version(version.Major, version.Minor, version.Patch, version.PreRelease, version.Build));
	}
}
