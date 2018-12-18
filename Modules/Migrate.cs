using CommandLine;
using EnvDTE;
using JetBrains.Annotations;
using NFive.SDK.Server;
using NFive.SDK.Server.Storage;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Design;
using System.Data.Entity.Migrations.Model;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using Console = Colorful.Console;
using IndentedTextWriter = System.Data.Entity.Migrations.Utilities.IndentedTextWriter;

namespace NFive.PluginManager.Modules
{

	internal static class EnumerableExtensions
	{
		public static bool ItemsEqual<T>(this IEnumerable<T> items, object other)
		{
			if (other == null) return false;
			var others = (IEnumerable<T>)other;
			return items != null && items.Count() == others.Count() && items.All(others.Contains);
		}

		public static IList<T> WhereIn<T>(this IEnumerable<object> items, IEnumerable<object> others, Func<T, dynamic, bool> predicate)
		{
			return items.OfType<T>().Where(op => others.Any(op2 => predicate(op, (dynamic)op2))).ToList();
		}
	}

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

			var i = GetInstances().ToList();
			DTE dte = null;
			foreach (var env in i)
			{
				if (env.Solution.FileName == this.Sln)
				{
					dte = env;
					break;
				}
			}

			if (dte == null) throw new Exception($"Could not find an open Visual Studio 2017 instance with the solution loaded: {this.Sln}");

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

				MessageFilter.Register();

				//Console.WriteLine("DEBUG: Opening solution...");

				var solution = Retry.Do(() => dte.Solution, TimeSpan.FromSeconds(1), 5);
				//Retry.Do(() => solution.Open(this.Sln), TimeSpan.FromSeconds(1), 5);

				//while (!solution.IsOpen) await Task.Delay(100);

				Console.WriteLine("DEBUG: Building solution...");

				Retry.Do(() => solution.SolutionBuild.Build(true), TimeSpan.FromSeconds(1), 5);

				while (solution.SolutionBuild.BuildState != vsBuildState.vsBuildStateDone) await Task.Delay(100);

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
			finally
			{
				//dte.Quit();

				MessageFilter.Revoke();
			}

			Console.WriteLine("Done");

			return await Task.FromResult(0);
		}

		IEnumerable<DTE> GetInstances()
		{
			IRunningObjectTable rot;
			IEnumMoniker enumMoniker;
			int retVal = GetRunningObjectTable(0, out rot);

			if (retVal == 0)
			{
				rot.EnumRunning(out enumMoniker);

				IntPtr fetched = IntPtr.Zero;
				IMoniker[] moniker = new IMoniker[1];
				while (enumMoniker.Next(1, moniker, fetched) == 0)
				{
					IBindCtx bindCtx;
					CreateBindCtx(0, out bindCtx);
					string displayName;
					moniker[0].GetDisplayName(bindCtx, null, out displayName);
					//Console.WriteLine("Display Name: {0}", displayName);
					bool isVisualStudio = displayName.StartsWith("!VisualStudio");
					if (isVisualStudio)
					{
						var dte = rot.GetObject(moniker[0], out var obj);
						yield return (DTE)obj;
					}
				}
			}
		}

		[DllImport("ole32.dll")]
		private static extern void CreateBindCtx(int reserved, out IBindCtx ppbc);

		[DllImport("ole32.dll")]
		private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);

		internal class Check
		{
			public static T NotNull<T>(T value, string parameterName) where T : class
			{
				if ((object)value == null)
					throw new ArgumentNullException(parameterName);
				return value;
			}

			public static T? NotNull<T>(T? value, string parameterName) where T : struct
			{
				if (!value.HasValue)
					throw new ArgumentNullException(parameterName);
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
			protected string @namespace;
			protected string className;
			protected List<string> excludedModels;

			public NFiveMigrationCodeGenerator(List<string>  excludedModels)
			{
				this.excludedModels = excludedModels;
			}

			public override ScaffoldedMigration Generate(string migrationId, IEnumerable<MigrationOperation> operations, string sourceModel, string targetModel, string @namespace, string className)
			{
				this.migrationId = migrationId;
				this.sourceModel = sourceModel;
				this.targetModel = targetModel;
				this.@namespace = @namespace;
				this.className = className;

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

				//base.WriteClassStart(@namespace, className, writer, @base, designer, namespaces);
			}

			protected override void WriteClassAttributes(IndentedTextWriter writer, bool designer)
			{
				//if (!designer) return;

				writer.WriteLine($"[GeneratedCode(\"NFive.Migration\", \"{typeof(NFiveMigrationCodeGenerator).GetTypeInfo().Assembly.GetCustomAttributes<AssemblyInformationalVersionAttribute>().Single().InformationalVersion}\")]");
			}

			protected override void WriteClassEnd(string @namespace, IndentedTextWriter writer)
			{
				base.WriteClassEnd(@namespace, writer);
			}

			private IEnumerable<MigrationOperation> FilterMigrationOperations(List<MigrationOperation> operations)
			{
				var dropTable = operations.OfType<DropTableOperation>().ToList();
				var dropColumns = operations.WhereIn<DropColumnOperation>(dropTable, (op, op2) => op2.Name == op.Table);
				var dropForeignKey = operations.WhereIn<DropForeignKeyOperation>(dropTable, (op, op2) => op2.Name == op.DependentTable || op2.Name == op.PrincipalTable);
				var dropIndex = operations.WhereIn<DropIndexOperation>(dropForeignKey, (op, op2) => op2.DependentTable == op.Table && op.Columns.ItemsEqual(op2.DependentColumns as object));
				var dropPrimaryKey = operations.WhereIn<DropPrimaryKeyOperation>(dropTable, (op, op2) => op2.Name == op.Table);

				var createTable = operations.WhereIn<CreateTableOperation>(dropTable, (op, op2) => op2.Name == op.Name);
				var addColumn = operations.WhereIn<AddColumnOperation>(dropColumns, (op, op2) => op2.Name == op.Column.Name && op2.Table == op.Table);
				var addForeignKey = operations.WhereIn<AddForeignKeyOperation>(dropForeignKey, (op, op2) => op2.Name == op.Name && op2.DependentTable == op.DependentTable && op2.PrincipalTable == op.PrincipalTable);
				var createIndex = operations.WhereIn<CreateIndexOperation>(dropIndex, (op, op2) => op.Table == op2.Table && op.Columns.ItemsEqual(op2.Columns as object));
				var addPrimaryKey = operations.WhereIn<AddPrimaryKeyOperation>(dropPrimaryKey, (op, op2) => op.Table == op2.Table && op.Columns.ItemsEqual(op2.Columns as object));

				var t = operations.OfType<CreateTableOperation>().Where(op => !this.excludedModels.Contains($"{op.Name}")).ToList();


				var exceptions = new IEnumerable<MigrationOperation>[]
				{
					dropTable,
					dropColumns,
					dropForeignKey,
					dropIndex,
					dropPrimaryKey,

					createTable,
					addColumn,
					addForeignKey,
					createIndex,
					addPrimaryKey,

					operations.OfType<CreateTableOperation>().Where(op => this.excludedModels.Contains($"{op.Name}")).ToList(),
					operations.OfType<AddForeignKeyOperation>().Where(op => this.excludedModels.Contains($"{op.DependentTable}")).ToList(),
					operations.OfType<CreateIndexOperation>().Where(op => this.excludedModels.Contains($"{op.Table}")).ToList(),

					operations.OfType<DropTableOperation>().Where(op => this.excludedModels.Contains($"{op.Name}")).ToList(),
					operations.OfType<DropForeignKeyOperation>().Where(op => this.excludedModels.Contains($"{op.DependentTable}")).ToList(),
					operations.OfType<DropIndexOperation>().Where(op => this.excludedModels.Contains($"{op.Table}")).ToList(),
				}.SelectMany(o => o).ToList();

				var filteredOperations = operations.Except(exceptions);

				return filteredOperations.ToList();
			}
		}
		public class MessageFilter : IOleMessageFilter
		{
			//
			// Class containing the IOleMessageFilter
			// thread error-handling functions.

			// Start the filter.
			public static void Register()
			{
				IOleMessageFilter newFilter = new MessageFilter();
				IOleMessageFilter oldFilter = null;
				CoRegisterMessageFilter(newFilter, out oldFilter);
			}

			// Done with the filter, close it.
			public static void Revoke()
			{
				IOleMessageFilter oldFilter = null;
				CoRegisterMessageFilter(null, out oldFilter);
			}

			//
			// IOleMessageFilter functions.
			// Handle incoming thread requests.
			int IOleMessageFilter.HandleInComingCall(int dwCallType,
			  System.IntPtr hTaskCaller, int dwTickCount, System.IntPtr
			  lpInterfaceInfo)
			{
				//Return the flag SERVERCALL_ISHANDLED.
				return 0;
			}

			// Thread call was rejected, so try again.
			int IOleMessageFilter.RetryRejectedCall(System.IntPtr
			  hTaskCallee, int dwTickCount, int dwRejectType)
			{
				if (dwRejectType == 2)
				// flag = SERVERCALL_RETRYLATER.
				{
					// Retry the thread call immediately if return >=0 & 
					// <100.
					return 99;
				}
				// Too busy; cancel call.
				return -1;
			}

			int IOleMessageFilter.MessagePending(System.IntPtr hTaskCallee,
			  int dwTickCount, int dwPendingType)
			{
				//Return the flag PENDINGMSG_WAITDEFPROCESS.
				return 2;
			}

			// Implement the IOleMessageFilter interface.
			[DllImport("Ole32.dll")]
			private static extern int
			  CoRegisterMessageFilter(IOleMessageFilter newFilter, out
			  IOleMessageFilter oldFilter);
		}

		[ComImport(), Guid("00000016-0000-0000-C000-000000000046"),
		InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
		interface IOleMessageFilter
		{
			[PreserveSig]
			int HandleInComingCall(
				int dwCallType,
				IntPtr hTaskCaller,
				int dwTickCount,
				IntPtr lpInterfaceInfo);

			[PreserveSig]
			int RetryRejectedCall(
				IntPtr hTaskCallee,
				int dwTickCount,
				int dwRejectType);

			[PreserveSig]
			int MessagePending(
				IntPtr hTaskCallee,
				int dwTickCount,
				int dwPendingType);
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
