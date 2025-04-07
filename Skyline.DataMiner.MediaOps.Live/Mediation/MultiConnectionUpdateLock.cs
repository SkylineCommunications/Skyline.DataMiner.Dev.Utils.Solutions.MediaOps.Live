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
			foreach (var id in ids.Distinct().OrderBy(id => id))
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

		public void Dispose()
		{
			if (_isDisposed)
			{
				return;
			}

			// Release locks in reverse order
			for (int i = _locks.Count - 1; i >= 0; i--)
			{
				_locks[i].Dispose();
			}

			_locks.Clear();

			_isDisposed = true;
		}
	}
}
