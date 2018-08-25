using System;
using NFive.PluginManager.Models.Plugin;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace NFive.PluginManager.Configuration
{
	/// <inheritdoc />
	/// <summary>
	/// Yaml converter for <see cref="T:NFive.PluginManager.Models.Plugin.VersionRange" />.
	/// </summary>
	/// <seealso cref="T:YamlDotNet.Serialization.IYamlTypeConverter" />
	public class VersionRangeConverter : IYamlTypeConverter
	{
		/// <inheritdoc />
		/// <summary>
		/// Gets a value indicating whether the current converter supports converting the specified type.
		/// </summary>
		public bool Accepts(Type type)
		{
			return type == typeof(VersionRange);
		}

		/// <inheritdoc />
		/// <summary>
		/// Reads an object's state from a YAML parser.
		/// </summary>
		public object ReadYaml(IParser parser, Type type)
		{
			var value = ((Scalar)parser.Current).Value;
			parser.MoveNext();
			return new VersionRange(value);
		}

		/// <inheritdoc />
		/// <summary>
		/// Writes the specified object's state to a YAML emitter.
		/// </summary>
		public void WriteYaml(IEmitter emitter, object value, Type type)
		{
			emitter.Emit(new Scalar(((VersionRange)value).ToString()));
		}
	}
}
