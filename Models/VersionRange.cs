namespace NFive.PluginManager.Models
{
	public class VersionRange : SDK.Core.Plugins.VersionRange
	{
		public VersionRange(string input)
		{
			this.Value = new SemVer.Range(input).ToString();
		}
	}
}
