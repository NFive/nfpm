using NFive.PluginManager.Utilities.Console;

namespace NFive.PluginManager.Utilities
{
	public static class Output
	{
		public static bool Quiet { get; set; }

		public static bool Verbose { get; set; }

		public static void Debug(params ColorToken[] tokens)
		{
			if (Verbose) PluginManager.Console.WriteLine(tokens);
		}

		public static void Debug(string message)
		{
			if (Verbose) PluginManager.Console.WriteLine(message);
		}

		public static void Info(params ColorToken[] tokens)
		{
			if (!Quiet) PluginManager.Console.WriteLine(tokens);
		}

		public static void Info(string message)
		{
			if (!Quiet) PluginManager.Console.WriteLine(message);
		}

		public static void Error(params ColorToken[] tokens)
		{
			PluginManager.Console.WriteLine(tokens);
		}

		public static void Error(string message)
		{
			PluginManager.Console.WriteLine(message);
		}
	}
}
