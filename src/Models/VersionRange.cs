using System.Collections.Generic;
using System.Linq;
using SemVer;

namespace NFive.PluginManager.Models
{
	public class VersionRange : SDK.Core.Plugins.VersionRange
	{
		private readonly Range value;

		public VersionRange(string input)
		{
			this.value = new Range(input);
			this.Value = this.value.ToString();
		}

		public bool IsSatisfied(Version version) => this.value.IsSatisfied(version);

		public bool IsSatisfied(string versionString, bool loose = false) => this.value.IsSatisfied(versionString, loose);

		public IEnumerable<Version> Satisfying(IEnumerable<Version> versions) => this.value.Satisfying(versions.Select(v => (SemVer.Version)v)).Select(v => (Version)v);

		public IEnumerable<string> Satisfying(IEnumerable<string> versionStrings, bool loose = false) => this.value.Satisfying(versionStrings, loose);

		public Version MaxSatisfying(IEnumerable<Version> versions) => this.value.MaxSatisfying(versions.Select(v => (SemVer.Version)v));

		public string MaxSatisfying(IEnumerable<string> versionStrings, bool loose = false) => this.value.MaxSatisfying(versionStrings, loose);

		public static implicit operator VersionRange(string value) => new VersionRange(value);

		public static implicit operator string(VersionRange value) => value.Value;
	}
}
