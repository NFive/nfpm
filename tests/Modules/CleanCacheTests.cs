using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;

namespace NFive.PluginManager.Tests.Modules
{
	[TestClass]
	public class CleanCacheTests
	{
		[TestMethod]
		public async Task Main_WithCache_Deleted()
		{
			var fs = new MockFileSystem();

			var cacheDir = fs.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nfpm", "cache");
			var cacheFile = fs.Path.Combine(cacheDir, "test");

			fs.AddDirectory(cacheDir);
			fs.AddFile(cacheFile, new MockFileData(string.Empty));

			var module = new PluginManager.Modules.CleanCache(fs);

			Assert.AreEqual(await module.Main(), 0);
			Assert.IsFalse(fs.Directory.Exists(cacheDir));
			Assert.IsFalse(fs.File.Exists(cacheFile));
		}

		[TestMethod]
		public async Task Main_WithoutCache_Passes()
		{
			var module = new PluginManager.Modules.CleanCache(new MockFileSystem());

			Assert.AreEqual(await module.Main(), 0);
		}

		[TestMethod]
		public async Task Main_Quiet_Silent()
		{
			using (var console = new ConsoleOutput())
			{
				var module = new PluginManager.Modules.CleanCache(new MockFileSystem())
				{
					Quiet = true,
					Verbose = false
				};

				Assert.AreEqual(await module.Main(), 0);
				Assert.AreEqual(console.Output, string.Empty);
				Assert.AreEqual(console.Error, string.Empty);
			}
		}

		[TestMethod]
		public async Task Main_Verbose_Loud()
		{
			using (var console = new ConsoleOutput())
			{
				var module = new PluginManager.Modules.CleanCache(new MockFileSystem())
				{
					Quiet = true,
					Verbose = true
				};

				Assert.AreEqual(await module.Main(), 0);
				Assert.AreNotEqual(console.Output, string.Empty);
				Assert.AreEqual(console.Error, string.Empty);
			}
		}
	}
}
