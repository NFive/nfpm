using System.IO;
using System.Linq;

namespace NFive.PluginManager.Extensions
{
	/// <summary>
	/// <see cref="DirectoryInfo"/> extension methods.
	/// </summary>
	public static class DirectoryExtensions
	{
		/// <summary>
		/// Copies the specified directory to the destination recursively.
		/// </summary>
		/// <param name="dir">The directory to copy.</param>
		/// <param name="dest">The destination directory.</param>
		/// <exception cref="DirectoryNotFoundException">Source directory does not exist or could not be found.</exception>
		/// <exception cref="IOException">Unable to create directory.</exception>
		public static void Copy(this DirectoryInfo dir, string dest)
		{
			if (!dir.Exists) throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {dir.FullName}");

			var files = Directory.EnumerateFiles(dir.FullName, "*", SearchOption.AllDirectories).Select(f => f.TrimStart(dir.FullName + Path.DirectorySeparatorChar)).ToArray();

			foreach (var file in files)
			{
				var destFile = Path.Combine(dest, file);

				Directory.CreateDirectory(Path.GetDirectoryName(destFile) ?? throw new IOException($"Unable to create directory: {Path.GetDirectoryName(destFile)}"));

				File.Copy(Path.Combine(dir.FullName, file), destFile, true);
			}
		}
	}
}
