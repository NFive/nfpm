using NFive.SDK.Core.Plugins;
using SemVer;
using System.Collections.Generic;
using System.Linq;
using Version = NFive.SDK.Core.Plugins.Version;

namespace NFive.PluginManager.Extensions
{
	public static class VersionRangeExtensions
	{
		public static bool IsSatisfied(this VersionRange target, Version version) => new Range(target.Value).IsSatisfied(new SemVer.Version(version.Major, version.Minor, version.Patch, version.PreRelease, version.Build));

		public static Version Latest(this VersionRange target, IEnumerable<Version> versions) => versions?.OrderBy(v => v.ToString()).LastOrDefault(target.IsSatisfied);
	}
}
