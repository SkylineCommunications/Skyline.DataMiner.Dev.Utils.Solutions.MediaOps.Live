namespace Skyline.DataMiner.MediaOps.Live.Tools
{
	using System;
	using System.Threading.Tasks;

	internal static class TaskExtensions
	{
		public static void FireAndForget(this Task task, Action<Exception> errorHandler = null)
		{
			task.ContinueWith(
				t =>
				{
					if (t.IsFaulted && errorHandler != null)
					{
						errorHandler(t.Exception);
					}
				},
				TaskContinuationOptions.OnlyOnFaulted);
		}
	}
}
