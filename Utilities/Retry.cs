using System;
using System.Collections.Generic;

namespace NFive.PluginManager.Utilities
{
	internal static class Retry
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
}
