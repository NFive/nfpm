using System;

namespace NFive.PluginManager.Utilities.Console
{
	public struct ColorToken : IEquatable<ColorToken>
	{
		private readonly string text;
		private readonly ConsoleColor? color;
		private readonly ConsoleColor? backgroundColor;

		public ColorToken(string text) : this(text, null, null) { }

		public ColorToken(string text, ConsoleColor? color) : this(text, color, null) { }

		public ColorToken(string text, ConsoleColor? color, ConsoleColor? backgroundColor)
		{
			this.text = text;
			this.color = color;
			this.backgroundColor = backgroundColor;
		}

		public string Text => this.text;

		public ConsoleColor? Color => this.color;

		public ConsoleColor? BackgroundColor => this.backgroundColor;

		public static implicit operator ColorToken(string text) => new ColorToken(text);

		public static bool operator ==(ColorToken left, ColorToken right) => left.Equals(right);

		public static bool operator !=(ColorToken left, ColorToken right) => !left.Equals(right);

		public ColorToken Mask(ConsoleColor defaultColor) => this.Mask(defaultColor, null);

		public ColorToken Mask(ConsoleColor? defaultColor, ConsoleColor? defaultBackgroundColor) => new ColorToken(this.text, this.color ?? defaultColor, this.backgroundColor ?? defaultBackgroundColor);

		public override string ToString() => this.text;

		public override int GetHashCode() => this.text == null ? 0 : this.text.GetHashCode();

		public override bool Equals(object obj) => obj is ColorToken token && this.Equals(token);

		public bool Equals(ColorToken other) => this.text == other.text && this.color == other.color && this.backgroundColor == other.backgroundColor;
	}
}
