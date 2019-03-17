using System;

namespace NFive.PluginManager.Utilities
{
	public static class Input
	{
		public static string String(string prompt, string @default = null)
		{
			System.Console.Write($"{prompt}: ");
			if (@default != null) System.Console.Write($"({@default}) ");

			var input = System.Console.ReadLine()?.Trim();

			if (@default != null && string.IsNullOrEmpty(input)) return @default;

			return input;
		}

		public static string String(string prompt, string @default, Func<string, bool> validator)
		{
			var input = String(prompt, @default);

			while (!validator(input))
			{
				input = System.Console.ReadLine()?.Trim();
			}

			return input;
		}

		public static string String(string prompt, Func<string, bool> validator) => String(prompt, null, validator);

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

		public static string Password(string prompt, string @default = null)
		{
			System.Console.Write($"{prompt}: ");
			if (@default != null) System.Console.Write($"({@default}) ");

			var input = string.Empty;

			do
			{
				var key = System.Console.ReadKey(true);

				if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
				{
					input += key.KeyChar;
					System.Console.Write("*");
				}
				else
				{
					if (key.Key == ConsoleKey.Backspace && input.Length > 0)
					{
						input = input.Substring(0, input.Length - 1);
						System.Console.Write("\b \b");
					}
					else if (key.Key == ConsoleKey.Enter)
					{
						break;
					}
				}
			} while (true);

			System.Console.WriteLine();

			return input;
		}
	}
}
