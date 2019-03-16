using JetBrains.Annotations;
using NFive.PluginManager.Utilities.Console;
using System;

namespace NFive.PluginManager.Extensions
{
	[PublicAPI]
	public static class StringColorExtensions
	{
		public static ColorToken Color(this string text, ConsoleColor? color) => new ColorToken(text, color);

		public static ColorToken Black(this string text) => text.Color(ConsoleColor.Black);

		public static ColorToken Blue(this string text) => text.Color(ConsoleColor.Blue);

		public static ColorToken Cyan(this string text) => text.Color(ConsoleColor.Cyan);

		public static ColorToken DarkBlue(this string text) => text.Color(ConsoleColor.DarkBlue);

		public static ColorToken DarkCyan(this string text) => text.Color(ConsoleColor.DarkCyan);

		public static ColorToken DarkGray(this string text) => text.Color(ConsoleColor.DarkGray);

		public static ColorToken DarkGreen(this string text) => text.Color(ConsoleColor.DarkGreen);

		public static ColorToken DarkMagenta(this string text) => text.Color(ConsoleColor.DarkMagenta);

		public static ColorToken DarkRed(this string text) => text.Color(ConsoleColor.DarkRed);

		public static ColorToken DarkYellow(this string text) => text.Color(ConsoleColor.DarkYellow);

		public static ColorToken Gray(this string text) => text.Color(ConsoleColor.Gray);

		public static ColorToken Green(this string text) => text.Color(ConsoleColor.Green);

		public static ColorToken Magenta(this string text) => text.Color(ConsoleColor.Magenta);

		public static ColorToken Red(this string text) => text.Color(ConsoleColor.Red);

		public static ColorToken White(this string text) => text.Color(ConsoleColor.White);

		public static ColorToken Yellow(this string text) => text.Color(ConsoleColor.Yellow);

		public static ColorToken On(this string text, ConsoleColor? backgroundColor) => new ColorToken(text, null, backgroundColor);

		public static ColorToken OnBlack(this string text) => text.On(ConsoleColor.Black);

		public static ColorToken OnBlue(this string text) => text.On(ConsoleColor.Blue);

		public static ColorToken OnCyan(this string text) => text.On(ConsoleColor.Cyan);

		public static ColorToken OnDarkBlue(this string text) => text.On(ConsoleColor.DarkBlue);

		public static ColorToken OnDarkCyan(this string text) => text.On(ConsoleColor.DarkCyan);

		public static ColorToken OnDarkGray(this string text) => text.On(ConsoleColor.DarkGray);

		public static ColorToken OnDarkGreen(this string text) => text.On(ConsoleColor.DarkGreen);

		public static ColorToken OnDarkMagenta(this string text) => text.On(ConsoleColor.DarkMagenta);

		public static ColorToken OnDarkRed(this string text) => text.On(ConsoleColor.DarkRed);

		public static ColorToken OnDarkYellow(this string text) => text.On(ConsoleColor.DarkYellow);

		public static ColorToken OnGray(this string text) => text.On(ConsoleColor.Gray);

		public static ColorToken OnGreen(this string text) => text.On(ConsoleColor.Green);

		public static ColorToken OnMagenta(this string text) => text.On(ConsoleColor.Magenta);

		public static ColorToken OnRed(this string text) => text.On(ConsoleColor.Red);

		public static ColorToken OnWhite(this string text) => text.On(ConsoleColor.White);

		public static ColorToken OnYellow(this string text) => text.On(ConsoleColor.Yellow);
	}
}
