namespace Skyline.DataMiner.MediaOps.Live.Mediation
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public sealed class MultiConnectionUpdateLock : IDisposable
	{
		private readonly List<ConnectionUpdateLock> _locks = new List<ConnectionUpdateLock>();
		private bool _isDisposed;

		public MultiConnectionUpdateLock(IEnumerable<Guid> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			// Sort IDs to ensure a consistent locking order, preventing deadlocks
			foreach (var id in ids.OrderBy(id => id))
			{
				try
				{
					_locks.Add(new ConnectionUpdateLock(id));
				}
				catch
				{
					// If acquiring a lock fails, release already acquired locks
					Dispose();
					throw;
				}
			}
		}

		~MultiConnectionUpdateLock()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (_isDisposed)
			{
				return;
			}

			if (disposing)
			{
				// Release locks in reverse order
				_locks.Reverse();

				foreach (var lockInstance in _locks)
				{
					lockInstance.Dispose();
				}

				_locks.Clear();
			}

			_isDisposed = true;
		}
	}
}
