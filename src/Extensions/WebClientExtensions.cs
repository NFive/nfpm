using System;
using System.ComponentModel;
using System.Net;
using System.Threading.Tasks;

namespace NFive.PluginManager.Extensions
{
	public static class WebClientExtensions
	{
		public static async Task DownloadFileTaskAsync(this WebClient webClient, string address, string fileName, IProgress<Tuple<long, int, long>> progress) => await webClient.DownloadFileTaskAsync(new Uri(address), fileName, progress);

		public static async Task DownloadFileTaskAsync(this WebClient webClient, Uri address, string fileName, IProgress<Tuple<long, int, long>> progress)
		{
			var tcs = new TaskCompletionSource<object>(address);

			void CompletedHandler(object s, AsyncCompletedEventArgs e)
			{
				if (e.UserState != tcs) return;

				if (e.Error != null) tcs.TrySetException(e.Error);
				else if (e.Cancelled) tcs.TrySetCanceled();
				else tcs.TrySetResult(null);
			}

			void ProgressChangedHandler(object s, DownloadProgressChangedEventArgs e)
			{
				if (e.UserState != tcs) return;

				progress.Report(Tuple.Create(e.BytesReceived, e.ProgressPercentage, e.TotalBytesToReceive));
			}

			try
			{
				webClient.DownloadFileCompleted += CompletedHandler;
				webClient.DownloadProgressChanged += ProgressChangedHandler;

				webClient.DownloadFileAsync(address, fileName, tcs);

				await tcs.Task;
			}
			finally
			{
				webClient.DownloadFileCompleted -= CompletedHandler;
				webClient.DownloadProgressChanged -= ProgressChangedHandler;
			}
		}

		public static async Task<byte[]> DownloadDataTaskAsync(this WebClient webClient, string address, IProgress<Tuple<long, int, long>> progress) => await webClient.DownloadDataTaskAsync(new Uri(address), progress);

		public static async Task<byte[]> DownloadDataTaskAsync(this WebClient webClient, Uri address, IProgress<Tuple<long, int, long>> progress)
		{
			var tcs = new TaskCompletionSource<byte[]>(address);

			void CompletedHandler(object s, DownloadDataCompletedEventArgs e)
			{
				if (e.UserState != tcs) return;

				if (e.Error != null) tcs.TrySetException(e.Error);
				else if (e.Cancelled) tcs.TrySetCanceled();
				else tcs.TrySetResult(e.Result);
			}

			void ProgressChangedHandler(object s, DownloadProgressChangedEventArgs e)
			{
				if (e.UserState != tcs) return;

				progress.Report(Tuple.Create(e.BytesReceived, e.ProgressPercentage, e.TotalBytesToReceive));
			}

			try
			{
				webClient.DownloadDataCompleted += CompletedHandler;
				webClient.DownloadProgressChanged += ProgressChangedHandler;

				webClient.DownloadDataAsync(address, tcs);

				return await tcs.Task;
			}
			finally
			{
				webClient.DownloadDataCompleted -= CompletedHandler;
				webClient.DownloadProgressChanged -= ProgressChangedHandler;
			}
		}
	}
}
