using System;
using System.Text;
using System.Threading;

namespace NFive.PluginManager.Utilities
{
	public class ProgressBar : IDisposable, IProgress<double>
	{
		private const string Animation = @"|/-\";
		private readonly TimeSpan animationInterval = TimeSpan.FromSeconds(1.0 / 8);
		private readonly Timer timer;

		private string currentText = string.Empty;
		private double currentProgress;
		private bool disposed;
		private int animationIndex;

		public ProgressBar()
		{
			this.timer = new Timer(s =>
			{
				lock (this.timer)
				{
					if (this.disposed) return;

					var blockCount = Math.Min(70, System.Console.WindowWidth - 10);
					var progressBlockCount = (int)(this.currentProgress * blockCount);
					var percent = (int)(this.currentProgress * 100);

					UpdateText($"[{new string('#', progressBlockCount)}{new string('-', blockCount - progressBlockCount)}] {percent,3}% {Animation[this.animationIndex++ % Animation.Length]}");

					ResetTimer();
				}
			});

			// Don't output to file
			if (!System.Console.IsOutputRedirected) ResetTimer();
		}

		public void Report(double value)
		{
			Interlocked.Exchange(ref this.currentProgress, Math.Max(0, Math.Min(1, value)));
		}

		private void ResetTimer()
		{
			this.timer.Change(this.animationInterval, TimeSpan.FromMilliseconds(-1));
		}

		private void UpdateText(string text)
		{
			var commonPrefixLength = 0;
			var commonLength = Math.Min(this.currentText.Length, text.Length);
			while (commonPrefixLength < commonLength && text[commonPrefixLength] == this.currentText[commonPrefixLength])
			{
				commonPrefixLength++;
			}

			var outputBuilder = new StringBuilder();
			outputBuilder.Append('\b', this.currentText.Length - commonPrefixLength);

			outputBuilder.Append(text.Substring(commonPrefixLength));

			var overlapCount = this.currentText.Length - text.Length;
			if (overlapCount > 0)
			{
				outputBuilder.Append(' ', overlapCount);
				outputBuilder.Append('\b', overlapCount);
			}

			System.Console.Write(outputBuilder);

			this.currentText = text;
		}

		public void Dispose()
		{
			lock (this.timer)
			{
				this.disposed = true;

				UpdateText(string.Empty);
			}
		}
	}
}
