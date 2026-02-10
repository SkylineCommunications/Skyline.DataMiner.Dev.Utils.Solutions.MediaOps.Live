namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Repositories
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Timers;

	using Skyline.DataMiner.Net.Helper;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects;

	internal class WriteBuffer<T> : IDisposable where T : ApiObject<T>
	{
		private readonly Repository<T> _repository;
		private readonly ConcurrentQueue<T> _queue;
		private readonly ConcurrentQueue<Exception> _exceptions;
		private readonly Timer _timer;

		public WriteBuffer(Repository<T> repository) : this(repository, TimeSpan.FromSeconds(1))
		{
		}

		public WriteBuffer(Repository<T> repository, TimeSpan writeInterval)
		{
			_repository = repository ?? throw new ArgumentNullException(nameof(repository));

			_queue = new ConcurrentQueue<T>();
			_exceptions = new ConcurrentQueue<Exception>();

			_timer = new Timer(writeInterval.TotalMilliseconds);
			_timer.Elapsed += IntervalPassed;
			_timer.AutoReset = true;
			_timer.Start();
		}

		public void Enqueue(IEnumerable<T> items)
		{
			foreach (T item in items)
			{
				_queue.Enqueue(item);
			}
		}

		public void Enqueue(T item)
		{
			_queue.Enqueue(item);
		}

		public void Flush()
		{
			WriteItemsInQueue();

			if (TryDequeueExceptions(out List<Exception> exceptions))
			{
				throw new AggregateException("One or more exceptions occurred while writing items to the repository.", exceptions);
			}
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
			try
			{
				var localItems = new Dictionary<Guid, T>(_queue.Count);

				while (_queue.TryDequeue(out T item))
				{
					// In case multiple items with the same ID are enqueued, only the last one will be written to the repository.
					localItems[item.ID] = item;
				}

				if (localItems.Count > 0)
				{
					_repository.CreateOrUpdate(localItems.Values);
				}
			}
			catch (Exception ex)
			{
				_exceptions.Enqueue(ex);
			}
		}

		private bool TryDequeueExceptions(out List<Exception> exceptions)
		{
			exceptions = null;

			while (_exceptions.TryDequeue(out Exception ex))
			{
				exceptions ??= new List<Exception>();
				exceptions.Add(ex);
			}

			return exceptions != null;
		}

		public void Dispose()
		{
			_timer.Elapsed -= IntervalPassed;
			_timer?.Dispose();

			Flush();
		}
	}
}
