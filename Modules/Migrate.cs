using CommandLine;
using EnvDTE;
using EnvDTE100;
using EnvDTE80;
using JetBrains.Annotations;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using NFive.PluginManager.Utilities;
using NFive.SDK.Server;
using NFive.SDK.Server.Storage;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Design;
using System.Data.Entity.Migrations.Model;
using System.Data.Entity.Migrations.Utilities;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Console = Colorful.Console;
using ServiceProvider = Microsoft.VisualStudio.Shell.ServiceProvider;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Create a NFive database migration.
	/// </summary>
	[UsedImplicitly]
	[Verb("migrate", HelpText = "Create a NFive database migration.")]
	internal class Migrate
	{
		[Option("name", Required = true, HelpText = "Migration name.")]
		public string Name { get; set; } = null;

		[Option("db", Required = true, HelpText = "MySQL database connection string.")]
		public string Database { get; set; } = null;

		[Option("sln", Required = false, HelpText = "Visual Studio SLN solution file.")]
		public string Sln { get; set; } = null;

		[SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
		internal async Task<int> Main()
		{
			try
			{
				Environment.CurrentDirectory = PathManager.FindResource();
			}
			catch (FileNotFoundException ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine("Use `nfpm scaffold` to generate a NFive plugin in this directory");

				return 1;
			}

			if (!File.Exists(this.Sln)) this.Sln = Directory.EnumerateFiles(Environment.CurrentDirectory, "*.sln", SearchOption.TopDirectoryOnly).FirstOrDefault();
			if (this.Sln == null || !File.Exists(this.Sln)) this.Sln = Input.String("Visual Studio SLN solution file");

			Console.WriteLine("DEBUG: Connecting to Visual Studio...");

			var dte = VisualStudio.GetInstances().FirstOrDefault(env => env.Solution.FileName == this.Sln) ?? (DTE2)Activator.CreateInstance(Type.GetTypeFromProgID("VisualStudio.DTE", true), true); // TODO: VS version

			Console.WriteLine("DEBUG: Opening solution...");

			var solution = Retry.Do(() => (Solution4)dte.Solution);

			if (!Retry.Do(() => solution.IsOpen))
			{
				using (var serviceProvider = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)dte))
				using (var solutionEventsListener = new SolutionEventsListener(serviceProvider))
				{
					var formClosed = new AsyncEventHandler();
					solutionEventsListener.AfterSolutionLoaded += formClosed.Handler;

					Retry.Do(() => solution.Open(this.Sln));

					await Task.WhenAny(formClosed.Event, Task.Delay(TimeSpan.FromSeconds(30)));
				}
			}

			Console.WriteLine("DEBUG: Building solution...");

			solution.SolutionBuild.Build(true); // Required to load DLL

			Console.WriteLine("DEBUG: Searching projects...");

			var pp = Retry.Do(() => solution.Projects.Cast<Project>().ToList());

			var ppp = Retry.Do(() => pp.Where(p => !string.IsNullOrWhiteSpace(p.FullName)).ToList());

			foreach (var project in ppp)
			{
				Console.WriteLine($"DEBUG: Analyzing project {Retry.Do(() => project.Name)}...");

				var projectPath = Path.GetDirectoryName(Retry.Do(() => project.FullName));
				var outputPath = Path.GetFullPath(Path.Combine(projectPath, Retry.Do(() => project.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString()), Retry.Do(() => project.Properties.Item("OutputFileName").Value.ToString())));

				Assembly asm = Assembly.Load(File.ReadAllBytes(outputPath));
				if (asm.GetCustomAttribute<ServerPluginAttribute>() == null) continue;

				var contextType = asm.DefinedTypes.FirstOrDefault(t => t.BaseType != null && t.BaseType.IsGenericType && t.BaseType.GetGenericTypeDefinition() == typeof(EFContext<>));
				if (contextType == default) continue;

				Console.WriteLine($"\tDEBUG: Loaded {outputPath}");

				Console.WriteLine($"\tDEBUG: Found DB context: {contextType.Name}");

				var props = contextType
					.GetProperties()
					.Where(p =>
						p.CanRead &&
						p.CanWrite &&
						p.PropertyType.IsGenericType &&
						p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) &&
						p.PropertyType.GenericTypeArguments.Any(t => t.Namespace.StartsWith("NFive.SDK."))) // TODO
					.Select(t => $"dbo.{t.Name}"); // TODO

				Console.WriteLine($"\tDEBUG: Excluding tables: {string.Join(", ", props)}");

				var migrationsPath = "Migrations";

				if (!Directory.Exists(Path.Combine(projectPath, migrationsPath))) throw new Exception("Migrations dir"); // TODO: Input

				var @namespace = $"{project.Properties.Item("RootNamespace").Value}.{migrationsPath}";

				if (asm.DefinedTypes.Any(t => t.BaseType != null && t.BaseType == typeof(DbMigration) && t.Namespace == @namespace && t.Name == this.Name))
				{
					throw new Exception($"A migration named \"{this.Name}\" already exists at \"{@namespace}.{this.Name}\""); // TODO: Input
				}

				Console.WriteLine($"\tDEBUG: Generating migration...");

				var migrationsConfiguration = new DbMigrationsConfiguration
				{
					AutomaticMigrationDataLossAllowed = false,
					AutomaticMigrationsEnabled = false,
					CodeGenerator = new NFiveMigrationCodeGenerator(props),
					ContextType = contextType,
					ContextKey = @namespace,
					MigrationsAssembly = asm,
					MigrationsDirectory = migrationsPath,
					MigrationsNamespace = @namespace,
					TargetDatabase = new DbConnectionInfo(this.Database, "MySql.Data.MySqlClient"),
				};

				var ms = new MigrationScaffolder(migrationsConfiguration);
				var src = ms.Scaffold(this.Name, false);

				Console.WriteLine($"Writing migration: {Path.Combine(projectPath, migrationsPath, $"{src.MigrationId}.{src.Language}")}");

				File.WriteAllText(Path.Combine(projectPath, migrationsPath, $"{src.MigrationId}.{src.Language}"), src.UserCode);

				Console.WriteLine($"Updating project...");

				project.ProjectItems.AddFromFile(Path.Combine(projectPath, migrationsPath, $"{src.MigrationId}.{src.Language}"));
				project.Save();
			}

			Console.WriteLine("DEBUG: Building solution...");

			solution.SolutionBuild.Build(true);

			Console.WriteLine("Done");

			return await Task.FromResult(0);
		}


		public static class Retry
		{
			public static void Do(Action action, uint retryIntervalMs = 1000, int maxAttemptCount = 3)
			{
				Do<object>(() =>
				{
					action();

					return null;
				}, TimeSpan.FromMilliseconds(retryIntervalMs), maxAttemptCount);
			}

			public static void Do(Action action, TimeSpan retryInterval, int maxAttemptCount = 3)
			{
				Do<object>(() =>
				{
					action();

					return null;
				}, retryInterval, maxAttemptCount);
			}

			public static T Do<T>(Func<T> action, uint retryIntervalMs = 1000, int maxAttemptCount = 3)
			{
				return Do(action, TimeSpan.FromMilliseconds(retryIntervalMs), maxAttemptCount);
			}

			public static T Do<T>(Func<T> action, TimeSpan retryInterval, int maxAttemptCount = 3)
			{
				var exceptions = new List<Exception>();

				for (var attempted = 0; attempted < maxAttemptCount; attempted++)
				{
					try
					{
						if (attempted > 0) System.Threading.Thread.Sleep(retryInterval);

						return action();
					}
					catch (Exception ex)
					{
						exceptions.Add(ex);

						System.Threading.Thread.Sleep(100);
					}
				}

				throw new AggregateException(exceptions);
			}
		}


		public class AsyncEventHandler
		{
			private readonly TaskCompletionSource<EventArgs> tcs = new TaskCompletionSource<EventArgs>();

			public EventHandler Handler => (s, a) => this.tcs.SetResult(a);

			public Task<EventArgs> Event => this.tcs.Task;
		}


		/// <inheritdoc />
		public class NFiveMigrationCodeGenerator : CSharpMigrationCodeGenerator
		{
			protected IEnumerable<string> ExcludedModels;
			protected string MigrationId;
			protected string SourceModel;
			protected string TargetModel;

			/// <inheritdoc />
			public NFiveMigrationCodeGenerator(IEnumerable<string> excludedModels)
			{
				this.ExcludedModels = excludedModels;
			}

			/// <inheritdoc />
			public override ScaffoldedMigration Generate(string migrationId, IEnumerable<MigrationOperation> operations, string sourceModel, string targetModel, string @namespace, string className)
			{
				this.MigrationId = migrationId;
				this.SourceModel = sourceModel;
				this.TargetModel = targetModel;

				return base.Generate(migrationId, FilterMigrationOperations(operations.ToList()), sourceModel, targetModel, @namespace, className);
			}

			/// <inheritdoc />
			protected override void WriteClassStart(string @namespace, string className, IndentedTextWriter writer, string @base, bool designer = false, IEnumerable<string> namespaces = null)
			{
				if (writer == null) throw new ArgumentException(nameof(writer));
				if (string.IsNullOrWhiteSpace(className)) throw new ArgumentException(nameof(className));
				if (string.IsNullOrWhiteSpace(@base)) throw new ArgumentException(nameof(@base));

				writer.WriteLine("// <auto-generated />");
				writer.WriteLine("// ReSharper disable all");
				writer.WriteLine();

				foreach (var ns in (namespaces ?? GetDefaultNamespaces(designer)).Distinct().OrderBy(n => n)) writer.WriteLine("using " + ns + ";");
				writer.WriteLine("using System.CodeDom.Compiler;");
				writer.WriteLine("using System.Data.Entity.Migrations.Infrastructure;");
				writer.WriteLine();

				if (!string.IsNullOrWhiteSpace(@namespace))
				{
					writer.Write("namespace ");
					writer.WriteLine(@namespace);
					writer.WriteLine("{");
					++writer.Indent;
				}

				WriteClassAttributes(writer, designer);
				writer.Write("public ");
				if (designer) writer.Write("sealed partial ");
				writer.Write("class ");
				writer.Write(className);
				writer.Write(" : ");
				writer.Write(@base);
				writer.WriteLine(", IMigrationMetadata");
				writer.WriteLine("{");
				++writer.Indent;

				writer.Write($"string IMigrationMetadata.Id => \"{this.MigrationId}\";");
				writer.WriteLine();
				writer.WriteLine();
				writer.Write("string IMigrationMetadata.Source => null;"); // TODO
				writer.WriteLine();
				writer.WriteLine();
				writer.Write($"string IMigrationMetadata.Target => \"{this.TargetModel}\";");
				writer.WriteLine();
				writer.WriteLine();
			}

			/// <inheritdoc />
			protected override void WriteClassAttributes(IndentedTextWriter writer, bool designer)
			{
				writer.WriteLine($"[GeneratedCode(\"NFive.Migration\", \"{typeof(NFiveMigrationCodeGenerator).GetTypeInfo().Assembly.GetCustomAttributes<AssemblyInformationalVersionAttribute>().Single().InformationalVersion}\")]");
			}

			private IEnumerable<MigrationOperation> FilterMigrationOperations(List<MigrationOperation> operations)
			{
				var exceptions = new IEnumerable<MigrationOperation>[]
				{
				operations.OfType<CreateTableOperation>().Where(op => this.ExcludedModels.Contains(op.Name)),
				operations.OfType<DropTableOperation>().Where(op => this.ExcludedModels.Contains(op.Name)),

				operations.OfType<AddForeignKeyOperation>().Where(op => this.ExcludedModels.Contains(op.DependentTable)),
				operations.OfType<DropForeignKeyOperation>().Where(op => this.ExcludedModels.Contains(op.DependentTable)),

				operations.OfType<CreateIndexOperation>().Where(op => this.ExcludedModels.Contains(op.Table)),
				operations.OfType<DropIndexOperation>().Where(op => this.ExcludedModels.Contains(op.Table))
				};

				return operations.Except(exceptions.SelectMany(o => o));
			}
		}

		/// <inheritdoc cref="IVsSolutionEvents" />
		public class SolutionEventsListener : IVsSolutionEvents, IDisposable
		{
			private IVsSolution solution;
			private uint handle;

			public event EventHandler AfterSolutionLoaded;
			public event EventHandler BeforeSolutionClosed;

			public SolutionEventsListener(IServiceProvider serviceProvider)
			{
				this.solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
				this.solution?.AdviseSolutionEvents(this, out this.handle);
			}

			int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded) => VSConstants.S_OK;

			int IVsSolutionEvents.OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy) => VSConstants.S_OK;

			int IVsSolutionEvents.OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
			{
				this.AfterSolutionLoaded?.Invoke(this.solution, EventArgs.Empty);

				return VSConstants.S_OK;
			}

			int IVsSolutionEvents.OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel) => VSConstants.S_OK;

			int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved) => VSConstants.S_OK;

			int IVsSolutionEvents.OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel) => VSConstants.S_OK;

			int IVsSolutionEvents.OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy) => VSConstants.S_OK;

			int IVsSolutionEvents.OnQueryCloseSolution(object pUnkReserved, ref int pfCancel) => VSConstants.S_OK;

			int IVsSolutionEvents.OnBeforeCloseSolution(object pUnkReserved)
			{
				this.BeforeSolutionClosed?.Invoke(this.solution, EventArgs.Empty);

				return VSConstants.S_OK;
			}

			int IVsSolutionEvents.OnAfterCloseSolution(object pUnkReserved) => VSConstants.S_OK;

			public void Dispose()
			{
				if (this.solution == null || this.handle == 0) return;

				GC.SuppressFinalize(this);

				this.solution.UnadviseSolutionEvents(this.handle);
				this.AfterSolutionLoaded = null;
				this.BeforeSolutionClosed = null;
				this.handle = 0;
				this.solution = null;
			}
		}
	}
}
