using System;
using System.Threading.Tasks;

namespace NFive.PluginManager.Utilities
{
	public class AsyncEventHandler
	{
		private readonly TaskCompletionSource<EventArgs> tcs = new TaskCompletionSource<EventArgs>();

		public EventHandler Handler => (s, a) => this.tcs.SetResult(a);

		public Task<EventArgs> Event => this.tcs.Task;
	}
}
