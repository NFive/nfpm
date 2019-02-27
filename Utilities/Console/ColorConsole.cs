namespace NFive.PluginManager.Utilities.Console
{
	public static class ColorConsole
	{
		private static readonly object Lock = new object();

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
