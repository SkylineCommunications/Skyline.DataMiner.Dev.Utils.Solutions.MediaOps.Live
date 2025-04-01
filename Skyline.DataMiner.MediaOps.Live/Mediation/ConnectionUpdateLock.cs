namespace Skyline.DataMiner.MediaOps.Live.Mediation
{
	using System;
	using System.Threading;

	public sealed class ConnectionUpdateLock : IDisposable
	{
		private readonly Mutex _mutex;
		private readonly bool _hasLock;

		private bool _isDisposed;

		public ConnectionUpdateLock(Guid id)
		{
			if (id == Guid.Empty)
			{
				throw new ArgumentException("ID cannot be an empty GUID.", nameof(id));
			}

			var mutexKey = $@"Global\Skyline_DataMiner_MediaOps_{nameof(ConnectionUpdateLock)}_{id}";

			_mutex = new Mutex(false, mutexKey);

			try
			{
				_hasLock = _mutex.WaitOne(TimeSpan.Zero, false);
			}
			catch
			{
				_mutex.Dispose();
				throw;
			}
		}

		~ConnectionUpdateLock()
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
				if (_hasLock)
				{
					try
					{
						_mutex.ReleaseMutex();
					}
					catch (Exception)
					{
						// Mutex was already released or not owned.
					}
				}

				_mutex.Dispose();
			}

			_isDisposed = true;
		}
	}
}
