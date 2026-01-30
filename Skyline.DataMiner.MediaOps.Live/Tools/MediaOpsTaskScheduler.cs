namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tools
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Threading;
	using System.Threading.Tasks;

	internal sealed class MediaOpsTaskScheduler : TaskScheduler, IDisposable
	{
		private readonly object _lock = new();
		private readonly int _maxConcurrencyLevel;
		private readonly List<Thread> _threads = [];
		private readonly BlockingCollection<Task> _tasks = [];

		/// <summary>Whether we're processing tasks on the current thread.</summary>
		private static readonly ThreadLocal<bool> _taskProcessingThread = new();

		private int _workingThreads;
		private bool _disposed;

		public MediaOpsTaskScheduler(int maxConcurrencyLevel = 100)
		{
			if (maxConcurrencyLevel < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(maxConcurrencyLevel));
			}

			_maxConcurrencyLevel = maxConcurrencyLevel;
		}

		public override int MaximumConcurrencyLevel => _maxConcurrencyLevel;

		protected override IEnumerable<Task> GetScheduledTasks()
		{
			return _tasks;
		}

		protected override void QueueTask(Task task)
		{
			if (task is null)
			{
				throw new ArgumentNullException(nameof(task));
			}

			if (_disposed)
			{
				throw new ObjectDisposedException(nameof(MediaOpsTaskScheduler));
			}

			_tasks.Add(task);

			MaybeStartNewThread();
		}

		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
		{
			if (task is null)
			{
				throw new ArgumentNullException(nameof(task));
			}

			if (!taskWasPreviouslyQueued && _taskProcessingThread.Value)
			{
				return TryExecuteTask(task);
			}

			return false;
		}

		private void MaybeStartNewThread()
		{
			lock (_lock)
			{
				var moreWorkThanThreads = _workingThreads + _tasks.Count > _threads.Count;
				var roomForMoreThreads = _threads.Count < _maxConcurrencyLevel;

				if (moreWorkThanThreads && roomForMoreThreads)
				{
					StartNewThread();
				}
			}
		}

		private void StartNewThread()
		{
			var thread = new Thread(ProcessTasks)
			{
				Name = $"MediaOps Live - Worker Thread",
				IsBackground = true,
			};

			_threads.Add(thread);
			thread.Start();
		}

		private void ProcessTasks()
		{
			try
			{
				_taskProcessingThread.Value = true;

				foreach (var task in _tasks.GetConsumingEnumerable())
				{
					try
					{
						Interlocked.Increment(ref _workingThreads);
						TryExecuteTask(task);
					}
					finally
					{
						Interlocked.Decrement(ref _workingThreads);
					}
				}
			}
			catch (Exception)
			{
				// ignore
			}
			finally
			{
				_taskProcessingThread.Value = false;

				lock (_lock)
				{
					_threads.Remove(Thread.CurrentThread);
				}
			}
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_tasks.CompleteAdding();

			ICollection<Thread> threadsToJoin;

			lock (_lock)
			{
				threadsToJoin = _threads.ToArray();
			}

			foreach (var thread in threadsToJoin)
			{
				thread.Join();
			}

			_tasks.Dispose();
		}
	}
}
