using CommandLine;
using EnvDTE;
using EnvDTE100;
using EnvDTE80;
using JetBrains.Annotations;
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Console = Colorful.Console;

namespace NFive.PluginManager.Modules
{
	/// <summary>
	/// Run or create a NFive database migration.
	/// </summary>
	[UsedImplicitly]
	[Verb("migrate", HelpText = "Run or create a NFive database migration.")]
	internal partial class Migrate
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

			Console.WriteLine("DEBUG: Connecting to Visual Studio...");

			var dte = VisualStudio.GetInstances().FirstOrDefault(env => env.Solution.FileName == this.Sln) ?? (DTE2)Activator.CreateInstance(Type.GetTypeFromProgID(DteProgId, true), true); // TODO: VS version

			Console.WriteLine("DEBUG: Opening solution...");

			// ReSharper disable once SuspiciousTypeConversion.Global
			var solution = (Solution4)dte.Solution;

			if (!solution.IsOpen) solution.Open(this.Sln);

			// TODO: Check saved, dirty

			Console.WriteLine("DEBUG: Building solution...");

			solution.SolutionBuild.Build(true); // Required to load DLL

			Console.WriteLine("DEBUG: Searching projects...");

			foreach (var project in solution.Projects.Cast<Project>().Where(p => !string.IsNullOrWhiteSpace(p.FullName)))
			{
				var projectPath = Path.GetDirectoryName(project.FullName);
				var outputPath = Path.Combine(projectPath, project.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString(), project.Properties.Item("OutputFileName").Value.ToString());

				Assembly asm = Assembly.Load(File.ReadAllBytes(outputPath));
				if (asm.GetCustomAttribute<ServerPluginAttribute>() == null) continue;

				var contextType = asm.DefinedTypes.FirstOrDefault(t => t.BaseType != null && t.BaseType.IsGenericType && t.BaseType.GetGenericTypeDefinition() == typeof(EFContext<>));
				if (contextType == default) continue;


				var props = contextType
					.GetProperties()
					.Where(p =>
						p.CanRead &&
						p.CanWrite &&
						p.PropertyType.IsGenericType &&
						p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) &&
						p.PropertyType.GenericTypeArguments.Any(t => t.Namespace.StartsWith("NFive.SDK."))) // TODO
					.Select(t => $"dbo.{t.Name}"); // TODO


				var migrationsPath = "Migrations";

				if (!Directory.Exists(Path.Combine(projectPath, migrationsPath))) throw new Exception("Migrations dir"); // TODO: Input

				var @namespace = $"{project.Properties.Item("RootNamespace").Value}.{migrationsPath}";

				if (asm.DefinedTypes.Any(t => t.BaseType != null && t.BaseType == typeof(DbMigration) && t.Namespace == @namespace && t.Name == this.Name))
				{
					throw new Exception($"A migration named \"{this.Name}\" already exists at \"{@namespace}.{this.Name}\""); // TODO: Input
				}

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
					TargetDatabase = new DbConnectionInfo(this.Database, DatabaseProvider),
				};

				var ms = new MigrationScaffolder(migrationsConfiguration);
				var src = ms.Scaffold(this.Name, false);

				Console.WriteLine($"Writing migration: {Path.Combine(projectPath, migrationsPath, $"{src.MigrationId}.{src.Language}")}");

				File.WriteAllText(Path.Combine(projectPath, migrationsPath, $"{src.MigrationId}.{src.Language}"), src.UserCode);

				project.ProjectItems.AddFromFile(Path.Combine(projectPath, migrationsPath, $"{src.MigrationId}.{src.Language}"));
				project.Save();
			}

			solution.SolutionBuild.Build(true);

			Console.WriteLine("Done");

			return await Task.FromResult(0);
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
	}
}
