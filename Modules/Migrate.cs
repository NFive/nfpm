using CommandLine;
using EnvDTE;
using JetBrains.Annotations;
using NFive.SDK.Server;
using NFive.SDK.Server.Storage;
using System;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Design;
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

			var dte = (DTE)Activator.CreateInstance(Type.GetTypeFromProgID(DteProgId, true), true);
			dte.SuppressUI = true;
			dte.UserControl = false;
			dte.MainWindow.Visible = false;
			//dte.MainWindow.Activate();

			try
			{
				var solution = dte.Solution;
				solution.Open(this.Sln);

				System.Threading.Thread.Sleep(20000); // TODO

				solution.SolutionBuild.Build(true);

				foreach (Project project in dte.Solution.Projects)
				{
					if (string.IsNullOrWhiteSpace(project.FullName)) continue;

					var projectPath = Path.GetDirectoryName(project.FullName);
					var outputPath = Path.Combine(projectPath, project.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString(), project.Properties.Item("OutputFileName").Value.ToString());
					
					Assembly asm = Assembly.Load(File.ReadAllBytes(outputPath));

					if (asm.GetCustomAttribute<ServerPluginAttribute>() == null) continue;

					var type = asm.DefinedTypes.FirstOrDefault(t => t.BaseType != null && t.BaseType.IsGenericType && t.BaseType.GetGenericTypeDefinition() == typeof(EFContext<>));

					if (type == default) continue;
					
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
						CodeGenerator = new CSharpMigrationCodeGenerator(),
						ContextType = type,
						ContextKey = @namespace,
						MigrationsAssembly = asm,
						MigrationsDirectory = migrationsPath,
						MigrationsNamespace = @namespace,
						TargetDatabase = new DbConnectionInfo(this.Database, DatabaseProvider),
					};

					var ms = new MigrationScaffolder(migrationsConfiguration);
					var src = ms.Scaffold(this.Name, false);

					File.WriteAllText(Path.Combine(projectPath, migrationsPath, $"{src.MigrationId}.{src.Language}"), src.UserCode);

					project.ProjectItems.AddFromFile(Path.Combine(projectPath, migrationsPath, $"{src.MigrationId}.{src.Language}"));
					project.Save();
				}

				
				solution.SolutionBuild.Build(true);
			}
			finally
			{
				dte.Quit();
			}

			return await Task.FromResult(0);
		}
	}
}
