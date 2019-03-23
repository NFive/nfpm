using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using NFive.PluginManager.Utilities;

namespace NFive.PluginManager.Tests.Modules
{
	[TestClass]
	public class CleanCacheTests
	{
		[TestMethod]
		public async Task Main_WithCache_Deleted()
		{
			var fs = new MockFileSystem();
			fs.AddDirectory(PathManager.CachePath);
			fs.AddFile(fs.Path.Combine(PathManager.CachePath, "test"), new MockFileData(string.Empty));

			var module = new PluginManager.Modules.CleanCache(fs);

			Assert.AreEqual(await module.Main(), 0);
			Assert.IsFalse(fs.Directory.Exists(PathManager.CachePath));
			Assert.IsFalse(fs.File.Exists(fs.Path.Combine(PathManager.CachePath, "test")));
		}

		[TestMethod]
		public async Task Main_WithoutCache_Passes()
		{
			var module = new PluginManager.Modules.CleanCache(new MockFileSystem());

			Assert.AreEqual(await module.Main(), 0);
		}

		[TestMethod]
		public async Task Main_QuietFlag_IsSilent()
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
		public async Task Main_VerboseFlag_HasOutput()
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
