namespace Skyline.DataMiner.MediaOps.Live.Tools
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	internal class MediaOpsTaskScheduler : TaskScheduler, IDisposable
	{
		private readonly object _lock = new();
		private readonly int _concurrencyLevel;
		private readonly List<Thread> _threads = [];
		private readonly BlockingCollection<Task> _tasks = [];

		private int _workingThreads;
		private bool _disposed;

		public MediaOpsTaskScheduler(int concurrencyLevel = 100)
		{
			_concurrencyLevel = concurrencyLevel;
		}

		public override int MaximumConcurrencyLevel => _concurrencyLevel;

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

			lock (_lock)
			{
				var allBusy = _workingThreads >= _threads.Count;
				var roomForMoreThreads = _threads.Count < _concurrencyLevel;

				if (roomForMoreThreads && allBusy)
				{
					StartNewThread();
				}

				if (_threads.Count < _concurrencyLevel && _workingThreads >= _threads.Count)
				{
					StartNewThread();
				}
			}
		}

		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
		{
			if (task is null)
			{
				throw new ArgumentNullException(nameof(task));
			}

			if (!taskWasPreviouslyQueued && _threads.Contains(Thread.CurrentThread))
			{
				return TryExecuteTask(task);
			}

			return false;
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

			lock (_lock)
			{
				foreach (var thread in _threads.ToList())
				{
					thread.Join();
				}

				_threads.Clear();
			}

			_tasks.Dispose();
		}
	}
}
