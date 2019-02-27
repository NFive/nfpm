using System;

namespace NFive.PluginManager.Utilities.Console
{
	public struct ColorToken : IEquatable<ColorToken>
	{
		public string Text { get; set; }

		public ConsoleColor? Color { get; }

		public ConsoleColor? BackgroundColor { get; }

		public ColorToken(string text) : this(text, null, null) { }

		public ColorToken(string text, ConsoleColor? color) : this(text, color, null) { }

		public ColorToken(string text, ConsoleColor? color, ConsoleColor? backgroundColor)
		{
			this.Text = text;
			this.Color = color;
			this.BackgroundColor = backgroundColor;
		}

		public ColorToken Mask(ConsoleColor defaultColor) => Mask(defaultColor, null);

		public ColorToken Mask(ConsoleColor? defaultColor, ConsoleColor? defaultBackgroundColor) => new ColorToken(this.Text, this.Color ?? defaultColor, this.BackgroundColor ?? defaultBackgroundColor);

		public override string ToString() => this.Text;

		public bool Equals(ColorToken other) => this.Text == other.Text && this.Color == other.Color && this.BackgroundColor == other.BackgroundColor;

		public override bool Equals(object obj) => obj is ColorToken token && Equals(token);

		public override int GetHashCode() => this.Text == null ? 0 : this.Text.GetHashCode();

		public static implicit operator ColorToken(string text) => new ColorToken(text);

		public static bool operator ==(ColorToken left, ColorToken right) => left.Equals(right);

		public static bool operator !=(ColorToken left, ColorToken right) => !left.Equals(right);
	}
}
