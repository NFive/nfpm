using System.Globalization;
using System.Linq;

namespace NFive.PluginManager.Extensions
{
	/// <summary>
	/// <see cref="string"/> extension methods.
	/// </summary>
	public static class StringExtensions
	{
		public static string TrimStart(this string target, string trimString)
		{
			if (string.IsNullOrEmpty(trimString)) return target;

			var result = target;
			while (result.StartsWith(trimString))
			{
				result = result.Substring(trimString.Length);
			}

			return result;
		}

		public static string Truncate(this string value, int length, string truncationString = "...")
		{
			if (value == null)
			{
				return null;
			}

			if (value.Length == 0)
			{
				return value;
			}

			if (truncationString == null || truncationString.Length > length)
			{
				return value.Substring(0, length);
			}

			return value.Length > length
				? value.Substring(0, length - truncationString.Length) + truncationString
				: value;
		}

		public static string Dehumanize(this string input)
		{
			var ti = CultureInfo.CurrentCulture.TextInfo;
			var words = input.Split(' ', '_', '-').Select(w => ti.ToTitleCase(w));
			return string.Join(string.Empty, words);
		}
	}
}
