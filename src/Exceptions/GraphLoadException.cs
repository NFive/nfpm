using System;

namespace NFive.PluginManager.Exceptions
{
	public class GraphLoadException : Exception
	{
		public GraphLoadException(Exception inner) : base(inner.Message, inner.InnerException) { }
	}
}
