using CommandLine;
using EnvDTE;
using JetBrains.Annotations;
using NFive.SDK.Server;
using NFive.SDK.Server.Storage;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Design;
using System.Data.Entity.Migrations.Model;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NFive.PluginManager.Utilities;
using Console = Colorful.Console;
using IndentedTextWriter = System.Data.Entity.Migrations.Utilities.IndentedTextWriter;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Run or create a NFive database migration.
	/// </summary>
	[UsedImplicitly]
	[Verb("migrate", HelpText = "Run or create a NFive database migration.")]
	internal class Migrate
	{
		private const string DteProgId = "VisualStudio.DTE.15.0";
		private const string DatabaseProvider = "MySql.Data.MySqlClient";

		[Option("name", Required = true, HelpText = "Migration name.")]
		public string Name { get; set; } = null;

		[Option("db", Required = true, HelpText = "MySQL database connection string.")]
		public string Database { get; set; } = null;

		[Option("sln", Required = false, HelpText = "Visual Studio SLN solution file.")]
		public string Sln { get; set; } = null;

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

			var dte = VisualStudio.GetInstances().FirstOrDefault(env => env.Solution.FileName == this.Sln);

			if (dte == null)
			{
				//throw new Exception($"Could not find an open Visual Studio 2017 instance with the solution loaded: {this.Sln}");

				dte = (DTE)Activator.CreateInstance(Type.GetTypeFromProgID(DteProgId, true), true);
			}

			//Console.WriteLine("DEBUG: Creating DTE instance...");

			//var dte = Retry.Do(() => (DTE)Activator.CreateInstance(Type.GetTypeFromProgID(DteProgId, true), true), TimeSpan.FromSeconds(1), 5);

			//dte.ExecuteCommand("ReSharper_Suspend");

			//dte.SuppressUI = true;
			//dte.UserControl = false;
			//dte.MainWindow.Visible = false;
			//dte.MainWindow.Activate();

			try
			{
				Console.WriteLine("DEBUG: Registering message filter...");


				//Console.WriteLine("DEBUG: Opening solution...");

				var solution = Retry.Do(() => dte.Solution, TimeSpan.FromSeconds(1), 5);


				if (!solution.IsOpen)
				{
					Retry.Do(() => solution.Open(this.Sln), TimeSpan.FromSeconds(1), 5);
				}

				//while (!solution.IsOpen) await Task.Delay(100);

				Console.WriteLine("DEBUG: Building solution...");

				Retry.Do(() => solution.SolutionBuild.Build(true), TimeSpan.FromSeconds(1), 5);

				//while (solution.SolutionBuild.BuildState != vsBuildState.vsBuildStateDone) await Task.Delay(100);

				Console.WriteLine("DEBUG: Iterating projects...");

				var projects = Retry.Do(() => solution.Projects.Cast<Project>().ToList(), TimeSpan.FromSeconds(1), 5);

				foreach (var project in projects)
				{
					try
					{
						Console.WriteLine("DEBUG: Checking project name...");

						var name = Retry.Do(() => project.FullName, TimeSpan.FromSeconds(3), 5);

						if (string.IsNullOrWhiteSpace(name)) continue;

						var projectPath = Path.GetDirectoryName(name);
						var outputPath = Path.Combine(projectPath, project.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString(), project.Properties.Item("OutputFileName").Value.ToString());

						Assembly asm = Assembly.Load(File.ReadAllBytes(outputPath));

						if (asm.GetCustomAttribute<ServerPluginAttribute>() == null) continue;

						var type = asm.DefinedTypes.FirstOrDefault(t => t.BaseType != null && t.BaseType.IsGenericType && t.BaseType.GetGenericTypeDefinition() == typeof(EFContext<>));

						if (type == default) continue;




						var props = type
							.GetProperties()
							.Where(p => p.CanRead && p.CanWrite && p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))


							.Where(p => p.PropertyType.GenericTypeArguments.Any(t => t.Namespace.StartsWith("NFive.SDK.")))

							//.SelectMany(p => p.PropertyType.GenericTypeArguments)
							//.Where(t => t.Namespace.StartsWith("NFive.SDK."))
							.Select(t => $"dbo.{t.Name}")
							.ToList();




						var migrationsPath = "Migrations";

						if (!Directory.Exists(Path.Combine(projectPath, migrationsPath))) throw new Exception("Migrations dir"); // TODO: Input

						var @namespace = project.Properties.Item("RootNamespace").Value.ToString() + $".{migrationsPath}";

						if (asm.DefinedTypes.Any(t => t.BaseType != null && t.BaseType == typeof(DbMigration) && t.Namespace == @namespace && t.Name == this.Name))
						{
							throw new Exception($"A migration named \"{this.Name}\" already exists at \"{@namespace}.{this.Name}\""); // TODO: Input
						}

						var migrationsConfiguration = new DbMigrationsConfiguration
						{
							AutomaticMigrationDataLossAllowed = false,
							AutomaticMigrationsEnabled = false,
							CodeGenerator = new NFiveMigrationCodeGenerator(props),
							ContextType = type,
							ContextKey = @namespace,
							MigrationsAssembly = asm,
							MigrationsDirectory = migrationsPath,
							MigrationsNamespace = @namespace,
							TargetDatabase = new DbConnectionInfo(this.Database, DatabaseProvider),
						};

						var ms = new MigrationScaffolder(migrationsConfiguration);
						var src = ms.Scaffold(this.Name, false);

						Console.WriteLine($"Writing migration: {Path.Combine(projectPath, migrationsPath, $"{src.MigrationId}.{src.Language}")}");

						File.WriteAllText(Path.Combine(projectPath, migrationsPath, $"{src.MigrationId}.{src.Language}"), src.UserCode);

						project.ProjectItems.AddFromFile(Path.Combine(projectPath, migrationsPath, $"{src.MigrationId}.{src.Language}"));
						project.Save();

						break;
					}
					catch (Exception ex)
					{
						//repeat
					}
				}

				solution.SolutionBuild.Build(true);
			}
			catch (Exception ex)
			{

			}

			Console.WriteLine("Done");

			return await Task.FromResult(0);
		}

		internal class Check
		{
			public static T NotNull<T>(T value, string parameterName) where T : class
			{
				if (value == null) throw new ArgumentNullException(parameterName);

				return value;
			}

			public static T? NotNull<T>(T? value, string parameterName) where T : struct
			{
				if (!value.HasValue) throw new ArgumentNullException(parameterName);

				return value;
			}

			public static string NotEmpty(string value, string parameterName)
			{
				if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(parameterName);
				return value;
			}
		}

		public class NFiveMigrationCodeGenerator : CSharpMigrationCodeGenerator
		{
			protected string migrationId;
			protected string sourceModel;
			protected string targetModel;
			protected List<string> excludedModels;

			public NFiveMigrationCodeGenerator(List<string> excludedModels)
			{
				this.excludedModels = excludedModels;
			}

			public override ScaffoldedMigration Generate(string migrationId, IEnumerable<MigrationOperation> operations, string sourceModel, string targetModel, string @namespace, string className)
			{
				this.migrationId = migrationId;
				this.sourceModel = sourceModel;
				this.targetModel = targetModel;

				var op = FilterMigrationOperations(operations.ToList());

				//List<MigrationOperation> ops = operations.OfType<CreateTableOperation>().Where(o => o.Name != "dbo.Users" && o.Name != "dbo.Sessions").Cast<MigrationOperation>().ToList();
				//ops.Add(operations.OfType<AddForeignKeyOperation>().Where(o => o.Name != "dbo.Users" && o.Name != "dbo.Sessions").Select(o => (AddForeignKeyOperation)o));
				//ops.Add(operations.OfType<CreateIndexOperation>().Where(o => o.Name != "dbo.Users" && o.Name != "dbo.Sessions"));

				return base.Generate(migrationId, op, sourceModel, targetModel, @namespace, className);
			}

			protected override void WriteClassStart(string @namespace, string className, IndentedTextWriter writer, string @base, bool designer = false, IEnumerable<string> namespaces = null)
			{
				Check.NotNull<IndentedTextWriter>(writer, nameof(writer));
				Check.NotEmpty(className, nameof(className));
				Check.NotEmpty(@base, nameof(@base));

				writer.WriteLine("// <auto-generated />");
				writer.WriteLine("// ReSharper disable all");
				writer.WriteLine();

				foreach (var ns in namespaces?.Distinct().OrderBy(n => n) ?? this.GetDefaultNamespaces(designer)) writer.WriteLine("using " + ns + ";");
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

				this.WriteClassAttributes(writer, designer);
				writer.Write("public ");
				if (designer) writer.Write("sealed partial ");
				writer.Write("class ");
				writer.Write(className);
				writer.Write(" : ");
				writer.Write(@base);
				writer.WriteLine(", IMigrationMetadata");
				writer.WriteLine("{");
				++writer.Indent;

				writer.Write($"string IMigrationMetadata.Id => \"{this.migrationId}\";");
				writer.WriteLine();
				writer.WriteLine();
				writer.Write("string IMigrationMetadata.Source => null;"); // TODO
				writer.WriteLine();
				writer.WriteLine();
				writer.Write($"string IMigrationMetadata.Target => \"{this.targetModel}\";");
				writer.WriteLine();
				writer.WriteLine();
			}

			protected override void WriteClassAttributes(IndentedTextWriter writer, bool designer)
			{
				writer.WriteLine($"[GeneratedCode(\"NFive.Migration\", \"{typeof(NFiveMigrationCodeGenerator).GetTypeInfo().Assembly.GetCustomAttributes<AssemblyInformationalVersionAttribute>().Single().InformationalVersion}\")]");
			}

			private IEnumerable<MigrationOperation> FilterMigrationOperations(List<MigrationOperation> operations)
			{
				var exceptions = new IEnumerable<MigrationOperation>[]
				{
					operations.OfType<CreateTableOperation>().Where(op => this.excludedModels.Contains($"{op.Name}")).ToList(),
					operations.OfType<AddForeignKeyOperation>().Where(op => this.excludedModels.Contains($"{op.DependentTable}")).ToList(),
					operations.OfType<CreateIndexOperation>().Where(op => this.excludedModels.Contains($"{op.Table}")).ToList(),

					operations.OfType<DropTableOperation>().Where(op => this.excludedModels.Contains($"{op.Name}")).ToList(),
					operations.OfType<DropForeignKeyOperation>().Where(op => this.excludedModels.Contains($"{op.DependentTable}")).ToList(),
					operations.OfType<DropIndexOperation>().Where(op => this.excludedModels.Contains($"{op.Table}")).ToList(),
				};

				return operations.Except(exceptions.SelectMany(o => o));
			}
		}

		public static class Retry
		{
			public static void Do(
				Action action,
				TimeSpan retryInterval,
				int maxAttemptCount = 3)
			{
				Do<object>(() =>
				{
					action();
					return null;
				}, retryInterval, maxAttemptCount);
			}

			public static T Do<T>(
				Func<T> action,
				TimeSpan retryInterval,
				int maxAttemptCount = 3)
			{
				var exceptions = new List<Exception>();

				for (int attempted = 0; attempted < maxAttemptCount; attempted++)
				{
					try
					{
						if (attempted > 0)
						{
							System.Threading.Thread.Sleep(retryInterval);
						}
						return action();
					}
					catch (Exception ex)
					{
						exceptions.Add(ex);
					}
				}
				throw new AggregateException(exceptions);
			}
		}
	}
}
