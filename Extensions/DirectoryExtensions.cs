using System.IO;

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
		/// <exception cref="DirectoryNotFoundException">The source directory could not be found.</exception>
		public static void Copy(this DirectoryInfo dir, string dest)
		{
			if (!dir.Exists) throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {dir.FullName}");

			var dirs = dir.GetDirectories();

			if (!Directory.Exists(dest)) Directory.CreateDirectory(dest);

			foreach (var file in dir.GetFiles())
			{
				file.CopyTo(Path.Combine(dest, file.Name), true);
			}

			foreach (var subDir in dirs)
			{
				Copy(subDir, Path.Combine(dest, subDir.Name));
			}
		}
	}
}
