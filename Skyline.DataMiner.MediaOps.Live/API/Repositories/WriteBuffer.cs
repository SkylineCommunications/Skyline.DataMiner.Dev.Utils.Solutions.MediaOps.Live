namespace Skyline.DataMiner.MediaOps.Live.API.Repositories
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;
	using System.Timers;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;

	internal class WriteBuffer<T> : IDisposable where T : ApiObject<T>
	{
		private readonly Repository<T> _repository;
		private readonly ConcurrentQueue<T> _queue;
		private readonly Timer _timer;

		public WriteBuffer(Repository<T> repository) : this(repository, TimeSpan.FromSeconds(1))
		{
		}

		public WriteBuffer(Repository<T> repository, TimeSpan writeInterval)
		{
			_repository = repository ?? throw new ArgumentNullException(nameof(repository));
			_queue = new ConcurrentQueue<T>();

			_timer = new Timer(writeInterval.TotalMilliseconds);
			_timer.Elapsed += IntervalPassed;
			_timer.AutoReset = true;
			_timer.Start();
		}

		public void QueueItems(IEnumerable<T> items)
		{
			foreach (T item in items)
			{
				_queue.Enqueue(item);
			}
		}

		public void Queue(T item)
		{
			_queue.Enqueue(item);
		}

		public void Flush()
		{
			WriteItemsInQueue();
		}

		private void IntervalPassed(object sender, ElapsedEventArgs e)
		{
			try
			{
				WriteItemsInQueue();
			}
			catch (Exception)
			{
				// Nothing
			}
		}

		private void WriteItemsInQueue()
		{
			List<T> items = new List<T>();
			while (_queue.TryDequeue(out T item))
			{
				items.Add(item);
			}

			if (items.Any())
			{
				_repository.CreateOrUpdate(items);
			}
		}

		public void Dispose()
		{
			Flush();

			_timer.Elapsed -= IntervalPassed;
			_timer?.Dispose();
		}
	}
}
