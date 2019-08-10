using CommandLine;
using EnvDTE;
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
using ConfigurationManager = NFive.SDK.Plugins.Configuration.ConfigurationManager;
using NFive.PluginManager.Models;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Create a NFive database migration.
	/// </summary>
	[Verb("migrate", HelpText = "Create a NFive database migration.")]
	internal class Migrate : Module
	{
		private bool existingInstance = true;

		[Option("name", Required = true, HelpText = "Migration name.")]
		public string Name { get; set; } = null;

		[Option("path", Required = true, HelpText = "NFive server path.")]
		public string ServerPath { get; set; } = null;

		[Option("db", Required = false, HelpText = "MySQL database connection string.")]
		public string Database { get; set; } = null;

		[Option("sln", Required = false, HelpText = "Visual Studio SLN solution file.")]
		public string Sln { get; set; }

		[Option("migrate", Required = false, HelpText = "Run existing migrations if necessary.")]
		public bool RunMigrations { get; set; } = false;

		[Option("sdk", Required = false, HelpText = "Internal use only, do not exclude SDK types.")]
		public bool Sdk { get; set; } = false;

		[SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
		[SuppressMessage("ReSharper", "ImplicitlyCapturedClosure")]
		public override async Task<int> Main()
		{
			string originalDirectoty = Environment.CurrentDirectory;

			if (this.Database == null)
			{
				try
				{
					Console.WriteLine($"Path: {this.ServerPath}");
					Environment.CurrentDirectory = this.ServerPath;
					PathManager.FindServer();
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
					Console.WriteLine("Make sure the path provided is a valid NFive server path.");
				}
				
				string dataBaseConfigYaml = Path.Combine(Environment.CurrentDirectory, "resources", "nfive", ConfigurationManager.ConfigurationPath, "database.yml");
				DatabaseConfiguration databaseConfiguration = null;

				try
				{
					databaseConfiguration = ConfigurationManager.Load<DatabaseConfiguration>(dataBaseConfigYaml);
				}
				catch(Exception ex)
				{
					Console.WriteLine(ex.Message);
					Console.WriteLine("Make sure NFive is properly installed.");
				}

				this.Database = $"Host={databaseConfiguration.Connection.Host};Port={databaseConfiguration.Connection.Port};" +
					$"Database={databaseConfiguration.Connection.Database};User Id={databaseConfiguration.Connection.User};" +
					$"Password={databaseConfiguration.Connection.Password};CharSet={databaseConfiguration.Connection.Charset};SSL Mode=None";
				Console.WriteLine(this.Database);
			}

			try
			{
				Environment.CurrentDirectory = originalDirectoty;
				Environment.CurrentDirectory = PathManager.FindResource();
			}
			catch (FileNotFoundException ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine("Use `nfpm scaffold` to generate a NFive plugin in this directory");

				return 1;
			}		

			AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
			{
				if (args.Name.Contains(".resources")) return null;

				var fileName = args.Name.Substring(0, args.Name.IndexOf(",", StringComparison.InvariantCultureIgnoreCase)) + ".dll";

				if (File.Exists(fileName)) return Assembly.Load(File.ReadAllBytes(fileName));

				var path = Directory.EnumerateFiles("plugins", "*.dll", SearchOption.AllDirectories).FirstOrDefault(f => Path.GetFileName(f) == fileName);

				if (string.IsNullOrEmpty(path)) throw new FileLoadException(args.Name);

				return Assembly.Load(File.ReadAllBytes(path));
			};

			DTE dte = null;

			try
			{
				if (!File.Exists(this.Sln)) this.Sln = Directory.EnumerateFiles(Environment.CurrentDirectory, "*.sln", SearchOption.TopDirectoryOnly).FirstOrDefault();
				if (this.Sln == null || !File.Exists(this.Sln)) this.Sln = Input.String("Visual Studio SLN solution file");

				Console.Write("Searching for existing Visual Studio instance...");

				dte = VisualStudio.GetInstances().FirstOrDefault(env => env.Solution.FileName == this.Sln);

				if (dte != default)
				{
					Console.WriteLine(" found");
				}
				else
				{
					Console.WriteLine(" not found");
					Console.WriteLine("Starting new Visual Studio instance...");

					dte = (DTE)Activator.CreateInstance(Type.GetTypeFromProgID("VisualStudio.DTE", true), true);

					this.existingInstance = false;
				}

				Console.WriteLine("Opening solution");

				var solution = Retry.Do(() => dte.Solution);

				if (!Retry.Do(() => solution.IsOpen)) Retry.Do(() => solution.Open(this.Sln));

				Console.WriteLine("Building solution");

				solution.SolutionBuild.Build(true);

				Console.WriteLine("Searching for projects");

				var pp = Retry.Do(() => solution.Projects.Cast<Project>().ToList());

				var ppp = Retry.Do(() => pp.Where(p => !string.IsNullOrWhiteSpace(p.FullName)).ToList());

				foreach (var project in ppp)
				{
					Console.WriteLine($"  Analyzing project {Retry.Do(() => project.Name)}...");

					var projectPath = Path.GetDirectoryName(Retry.Do(() => project.FullName)) ?? string.Empty;
					var outputPath = Path.GetFullPath(Path.Combine(projectPath, Retry.Do(() => project.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString()), Retry.Do(() => project.Properties.Item("OutputFileName").Value.ToString())));

					var asm = Assembly.Load(File.ReadAllBytes(outputPath));
					if (!this.Sdk && asm.GetCustomAttribute<ServerPluginAttribute>() == null) continue;

					var contextType = asm.DefinedTypes.FirstOrDefault(t => t.BaseType != null && t.BaseType.IsGenericType && t.BaseType.GetGenericTypeDefinition() == typeof(EFContext<>));
					if (contextType == default) continue;

					Console.WriteLine($"    Loaded {outputPath}");

					Console.WriteLine($"    Found DB context: {contextType.Name}");

					var props = contextType
						.GetProperties()
						.Where(p =>
							p.CanRead &&
							p.CanWrite &&
							p.PropertyType.IsGenericType &&
							p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) &&
							p.PropertyType.GenericTypeArguments.Any(t => !string.IsNullOrEmpty(t.Namespace) && t.Namespace.StartsWith("NFive.SDK."))) // TODO
						.Select(t => $"dbo.{t.Name}") // TODO
						.ToArray();

					if (!this.Sdk) Console.WriteLine($"    Excluding tables: {string.Join(", ", props)}");

					var migrationsPath = "Migrations";

					if (!Directory.Exists(Path.Combine(projectPath, migrationsPath))) migrationsPath = Input.String("Migration source code folder", "Migrations"); // TODO: Validate

					var @namespace = $"{project.Properties.Item("RootNamespace").Value}.{migrationsPath}";

					if (asm.DefinedTypes.Any(t => t.BaseType != null && t.BaseType == typeof(DbMigration) && t.Namespace == @namespace && t.Name == this.Name))
					{
						throw new Exception($"A migration named \"{this.Name}\" already exists at \"{@namespace}.{this.Name}\", please use another migration name.");
					}

					Console.WriteLine("    Generating migration...");

					var migrationsConfiguration = new DbMigrationsConfiguration
					{
						AutomaticMigrationDataLossAllowed = false,
						AutomaticMigrationsEnabled = false,
						CodeGenerator = new NFiveMigrationCodeGenerator(this.Sdk ? new string[] { } : props),
						ContextType = contextType,
						ContextKey = $"{@namespace}.Configuration",
						MigrationsAssembly = asm,
						MigrationsDirectory = migrationsPath,
						MigrationsNamespace = @namespace,
						TargetDatabase = new DbConnectionInfo(this.Database, "MySql.Data.MySqlClient")
					};

					var ms = new MigrationScaffolder(migrationsConfiguration);
					
					if (this.RunMigrations)
					{
						var migrator = new DbMigrator(migrationsConfiguration);

						if (migrator.GetPendingMigrations().Any())
						{
							Console.WriteLine("    Running existing migrations...");

							foreach (var migration in migrator.GetPendingMigrations())
							{
								Console.WriteLine($"        Running migration: {migration}");

								migrator.Update(migration);
							}
						}
					}

					Console.WriteLine("    Scaffolding migration...");

					var src = ms.Scaffold(this.Name, false);

					var file = Path.Combine(projectPath, migrationsPath, $"{src.MigrationId}.{src.Language}");

					Console.WriteLine($"    Writing migration: {file}");

					File.WriteAllText(file, src.UserCode);

					Console.WriteLine("    Updating project...");

					project.ProjectItems.AddFromFile(file);
					project.Save();
				}

				Console.WriteLine("Building solution...");

				solution.SolutionBuild.Build(true);

				if (!this.existingInstance)
				{
					Console.WriteLine("Quitting Visual Studio instance");

					dte.Quit();
				}

				Console.WriteLine("Done");

				return await Task.FromResult(0);
			}
			catch (ReflectionTypeLoadException ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine(string.Join(Environment.NewLine, ex.LoaderExceptions.Select(e => e.Message)));

				return 1;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);

				return 1;
			}
			finally
			{
				if (!this.existingInstance) dte.Quit();
			}
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
				writer.Write($"string IMigrationMetadata.Source => {(this.SourceModel == null ? "null" : $"\"{this.TargetModel}\"")};");
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
	}
}
