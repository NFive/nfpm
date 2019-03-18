using System;
using System.IO;

namespace NFive.PluginManager.Tests
{
	public class ConsoleOutput : IDisposable
	{
		private readonly TextWriter originalOutput;
		private readonly TextWriter originalError;
		private readonly StringWriter output;
		private readonly StringWriter error;

		public string Output => this.output.ToString();

		public string Error => this.error.ToString();

		public ConsoleOutput()
		{
			this.originalOutput = System.Console.Out;
			this.originalError = System.Console.Error;

			this.output = new StringWriter();
			this.error = new StringWriter();

			System.Console.SetOut(this.output);
			System.Console.SetError(this.error);
		}

		public void Dispose()
		{
			System.Console.SetOut(this.originalOutput);
			System.Console.SetError(this.originalError);

			this.output.Dispose();
			this.error.Dispose();
		}
	}
}
