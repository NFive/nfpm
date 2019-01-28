using System;

namespace NFive.PluginManager
{
	public static class Input
	{
		public static string String(string prompt, string @default = null)
		{
			Console.Write($"{prompt}: ");
			if (@default != null) Console.Write($"({@default}) ");

			var input = Console.ReadLine()?.Trim();

			if (@default != null && string.IsNullOrEmpty(input)) return @default;

			return input;
		}

		public static string String(string prompt, string @default, Func<string, bool> validator)
		{
			var input = String(prompt, @default);

			while (!validator(input))
			{
				input = Console.ReadLine()?.Trim();
			}

			return input;
		}

		public static string String(string prompt, Func<string, bool> validator)
		{
			return String(prompt, null, validator);
		}

		public static bool Bool(string prompt, bool? @default = null)
		{
			var input = String(prompt, @default?.ToString()).ToLowerInvariant();

			return input == "1" || input == "true" || input == "yes" || input == "y";
		}

		public static int Int(string prompt, int min = int.MinValue, int max = int.MaxValue, int? @default = null)
		{
			var input = String(prompt, @default?.ToString());

			if (@default != null && string.IsNullOrEmpty(input)) input = @default.ToString();

			int value;
			while (!int.TryParse(input, out value) || value < min || value > max)
			{
				input = String($"Please enter an integer between {min} and {max} (inclusive)", @default?.ToString());
			}

			return value;
		}
	}
}
