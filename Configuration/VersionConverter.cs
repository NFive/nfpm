using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace NFive.PluginManager.Configuration
{
	/// <inheritdoc />
	/// <summary>
	/// Yaml converter for <see cref="T:NFive.PluginManager.Models.Plugin.Version" />.
	/// </summary>
	/// <seealso cref="T:YamlDotNet.Serialization.IYamlTypeConverter" />
	public class VersionConverter : IYamlTypeConverter
	{
		/// <inheritdoc />
		/// <summary>
		/// Gets a value indicating whether the current converter supports converting the specified type.
		/// </summary>
		public bool Accepts(Type type)
		{
			return type == typeof(Models.Plugin.Version);
		}

		/// <inheritdoc />
		/// <summary>
		/// Reads an object's state from a YAML parser.
		/// </summary>
		public object ReadYaml(IParser parser, Type type)
		{
			var value = ((Scalar)parser.Current).Value;
			parser.MoveNext();
			return new Models.Plugin.Version(value);
		}

		/// <inheritdoc />
		/// <summary>
		/// Writes the specified object's state to a YAML emitter.
		/// </summary>
		public void WriteYaml(IEmitter emitter, object value, Type type)
		{
			emitter.Emit(new Scalar(((Models.Plugin.Version)value).ToString()));
		}
	}
}
