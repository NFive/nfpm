using NFive.SDK.Core.Plugins;
using SemVer;

namespace NFive.PluginManager.Extensions
{
	public static class VersionRangeExtensions
	{
		public static bool IsSatisfied(this VersionRange target, string version)
		{
			return new Range(version).IsSatisfied(version);
		}
	}
}
