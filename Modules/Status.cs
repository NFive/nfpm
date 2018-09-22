using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using JetBrains.Annotations;
using NFive.PluginManager.Extensions;
using Console = Colorful.Console;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Show the status of the current directory.
	/// </summary>
	[UsedImplicitly]
	[Verb("status", HelpText = "Show the status of the current directory.")]
	internal class Status
	{
		internal async Task<int> Main()
		{
			var cd = Path.GetFullPath(Environment.CurrentDirectory);
			Console.WriteLine($"Current directory: {cd}", Color.Green);

			var bin = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
			Console.WriteLine($"nfpm.exe: {Relative(bin, cd)}", Color.Green);
			
			string server = null;
			try
			{
				server = Relative(PathManager.FindServer(), cd);
			}
			catch (Exception) { }

			Console.WriteLine($"server: {server ?? "NOT FOUND"}");
			
			string resource = null;
			try
			{
				resource = Relative(PathManager.FindResource(), cd);
			}
			catch (Exception) { }

			Console.WriteLine($"resource: {resource ?? "NOT FOUND"}");
			
			bool current = false;
			try
			{
				current = PathManager.IsResource();
			}
			catch (Exception) { }

			Console.WriteLine($"current: {current}");

			return await Task.FromResult(0);
		}

		private string Relative(string path, string cd)
		{
			var res = path.TrimStart(cd);

			return string.IsNullOrEmpty(res) ? @".\" : res;
		}
	}
}
