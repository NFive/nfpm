using System.Linq;
using NFive.PluginManager.Utilities.Console;

namespace NFive.PluginManager
{
	public static class Console
	{
		private static readonly object Lock = new object();

		public static void WriteLine()
		{
			System.Console.WriteLine();
		}

		public static void Write(string message)
		{
			System.Console.Write(message);
		}

		public static void WriteLine(string message)
		{
			System.Console.WriteLine(message);
		}

		public static void Write(params ColorToken[] tokens)
		{
			if (tokens == null || tokens.Length == 0) return;

			lock (Lock)
			{
				foreach (var token in tokens)
				{
					if (token.Color.HasValue || token.BackgroundColor.HasValue)
					{
						var originalColor = System.Console.ForegroundColor;
						var originalBackgroundColor = System.Console.BackgroundColor;

						try
						{
							System.Console.ForegroundColor = token.Color ?? originalColor;
							System.Console.BackgroundColor = token.BackgroundColor ?? originalBackgroundColor;

							System.Console.Write(token.Text);
						}
						finally
						{
							System.Console.ForegroundColor = originalColor;
							System.Console.BackgroundColor = originalBackgroundColor;
						}
					}
					else
					{
						System.Console.Write(token.Text);
					}
				}
			}
		}

		public static void WriteLine(params ColorToken[] tokens)
		{
			lock (Lock)
			{
				Write(tokens);
				System.Console.WriteLine();
			}
		}
	}
}
