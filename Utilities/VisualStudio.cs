using EnvDTE;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace NFive.PluginManager.Utilities
{
	internal static class VisualStudio
	{
		[DllImport("ole32.dll")]
		// ReSharper disable once IdentifierTypo
		private static extern void CreateBindCtx(int reserved, out IBindCtx ppbc);

		[DllImport("ole32.dll")]
		// ReSharper disable once IdentifierTypo
		private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);

		public static IEnumerable<DTE> GetInstances()
		{
			if (GetRunningObjectTable(0, out var rot) != 0) yield break;

			rot.EnumRunning(out var enumMoniker);

			var fetched = IntPtr.Zero;
			var moniker = new IMoniker[1];

			while (enumMoniker.Next(1, moniker, fetched) == 0)
			{
				CreateBindCtx(0, out var bindCtx);
				moniker[0].GetDisplayName(bindCtx, null, out var displayName);

				if (!displayName.StartsWith("!VisualStudio")) continue;

				rot.GetObject(moniker[0], out var obj);

				yield return (DTE)obj;
			}
		}
	}
}
