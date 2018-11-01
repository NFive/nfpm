namespace NFive.PluginManager.Models
{
	public class VersionRange : SDK.Core.Plugins.VersionRange
	{
		public VersionRange(string input)
		{
			this.Value = new SemVer.Range(input).ToString();
		}

		public static implicit operator VersionRange(string value) => new VersionRange(value);

		public static implicit operator string(VersionRange value) => value.Value;
	}
}
