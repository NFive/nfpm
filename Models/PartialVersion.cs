using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace NFive.PluginManager.Models
{
	public class PartialVersion
	{
		public int? Major { get; set; }

		public int? Minor { get; set; }

		public int? Patch { get; set; }

		public string PreRelease { get; set; }

		private static Regex regex = new Regex(@"^
                [v=\s]*
                (\d+|[Xx\*])                      # major version
                (
                    \.
                    (\d+|[Xx\*])                  # minor version
                    (
                        \.
                        (\d+|[Xx\*])              # patch version
                        (\-?([0-9A-Za-z\-\.]+))?  # pre-release version
                        (\+([0-9A-Za-z\-\.]+))?   # build version (ignored)
                    )?
                )?
                $",
			RegexOptions.IgnorePatternWhitespace);

		public PartialVersion(string input)
		{
			if (input.Trim() == "") return;

			var match = regex.Match(input);
			if (!match.Success)
			{
				throw new ArgumentException($"Invalid version string: \"{input}\"");
			}

			this.Major = int.Parse(match.Groups[1].Value);

			if (match.Groups[2].Success)
			{
				this.Minor = int.Parse(match.Groups[3].Value);
			}

			if (match.Groups[4].Success)
			{
				this.Patch = int.Parse(match.Groups[5].Value);
			}

			if (match.Groups[6].Success)
			{
				this.PreRelease = match.Groups[7].Value;
			}
		}

		public Version ToZeroVersion() => new Version(this.Major ?? 0, this.Minor ?? 0, this.Patch ?? 0, this.PreRelease);

		public bool IsFull() => this.Major.HasValue && this.Minor.HasValue && this.Patch.HasValue;
	}
}
