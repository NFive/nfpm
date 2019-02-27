using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NFive.PluginManager.Utilities.Console;

namespace NFive.PluginManager.Extensions
{
	[PublicAPI]
	public static class ColorTokenExtensions
	{
		public static ColorToken[] Mask(this IEnumerable<ColorToken> tokens, ConsoleColor color) => tokens.Mask(color, null);

		public static ColorToken[] Mask(this IEnumerable<ColorToken> tokens, ConsoleColor? color, ConsoleColor? backgroundColor) => tokens?.Select(token => token.Mask(color, backgroundColor)).ToArray();

		public static ColorToken On(this ColorToken token, ConsoleColor? backgroundColor) => new ColorToken(token.Text, token.Color, backgroundColor);

		public static ColorToken OnBlack(this ColorToken token) => token.On(ConsoleColor.Black);

		public static ColorToken OnBlue(this ColorToken token) => token.On(ConsoleColor.Blue);

		public static ColorToken OnCyan(this ColorToken token) => token.On(ConsoleColor.Cyan);

		public static ColorToken OnDarkBlue(this ColorToken token) => token.On(ConsoleColor.DarkBlue);

		public static ColorToken OnDarkCyan(this ColorToken token) => token.On(ConsoleColor.DarkCyan);

		public static ColorToken OnDarkGray(this ColorToken token) => token.On(ConsoleColor.DarkGray);

		public static ColorToken OnDarkGreen(this ColorToken token) => token.On(ConsoleColor.DarkGreen);

		public static ColorToken OnDarkMagenta(this ColorToken token) => token.On(ConsoleColor.DarkMagenta);

		public static ColorToken OnDarkRed(this ColorToken token) => token.On(ConsoleColor.DarkRed);

		public static ColorToken OnDarkYellow(this ColorToken token) => token.On(ConsoleColor.DarkYellow);

		public static ColorToken OnGray(this ColorToken token) => token.On(ConsoleColor.Gray);

		public static ColorToken OnGreen(this ColorToken token) => token.On(ConsoleColor.Green);

		public static ColorToken OnMagenta(this ColorToken token) => token.On(ConsoleColor.Magenta);

		public static ColorToken OnRed(this ColorToken token) => token.On(ConsoleColor.Red);

		public static ColorToken OnWhite(this ColorToken token) => token.On(ConsoleColor.White);

		public static ColorToken OnYellow(this ColorToken token) => token.On(ConsoleColor.Yellow);

		public static ColorToken Color(this ColorToken token, ConsoleColor? color) => new ColorToken(token.Text, color, token.BackgroundColor);

		public static ColorToken Black(this ColorToken token) => token.Color(ConsoleColor.Black);

		public static ColorToken Blue(this ColorToken token) => token.Color(ConsoleColor.Blue);

		public static ColorToken Cyan(this ColorToken token) => token.Color(ConsoleColor.Cyan);

		public static ColorToken DarkBlue(this ColorToken token) => token.Color(ConsoleColor.DarkBlue);

		public static ColorToken DarkCyan(this ColorToken token) => token.Color(ConsoleColor.DarkCyan);

		public static ColorToken DarkGray(this ColorToken token) => token.Color(ConsoleColor.DarkGray);

		public static ColorToken DarkGreen(this ColorToken token) => token.Color(ConsoleColor.DarkGreen);

		public static ColorToken DarkMagenta(this ColorToken token) => token.Color(ConsoleColor.DarkMagenta);

		public static ColorToken DarkRed(this ColorToken token) => token.Color(ConsoleColor.DarkRed);

		public static ColorToken DarkYellow(this ColorToken token) => token.Color(ConsoleColor.DarkYellow);

		public static ColorToken Gray(this ColorToken token) => token.Color(ConsoleColor.Gray);

		public static ColorToken Green(this ColorToken token) => token.Color(ConsoleColor.Green);

		public static ColorToken Magenta(this ColorToken token) => token.Color(ConsoleColor.Magenta);

		public static ColorToken Red(this ColorToken token) => token.Color(ConsoleColor.Red);

		public static ColorToken White(this ColorToken token) => token.Color(ConsoleColor.White);

		public static ColorToken Yellow(this ColorToken token) => token.Color(ConsoleColor.Yellow);
	}
}
