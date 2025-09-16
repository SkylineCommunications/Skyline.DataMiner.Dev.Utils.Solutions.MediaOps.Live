namespace Skyline.DataMiner.MediaOps.Live.GQI
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Analytics.GenericInterface;

	public class GQIPageEnumerator : IDisposable
	{
		private readonly IEnumerator<GQIRow> _enumerator;

		private bool _hasNext;
		private GQIRow _nextRow;

		private bool _isDisposed;

		public GQIPageEnumerator(IEnumerable<GQIRow> rows)
		{
			if (rows == null)
			{
				throw new ArgumentNullException(nameof(rows));
			}

			_enumerator = rows.GetEnumerator();
			TryMoveNext();
		}

		public GQIPage GetNextPage(int pageSize)
		{
			try
			{
				var page = new List<GQIRow>(pageSize);

				while (page.Count < pageSize && _hasNext)
				{
					page.Add(_nextRow);
					TryMoveNext();
				}

				return new GQIPage(page.ToArray())
				{
					HasNextPage = _hasNext,
				};
			}
			finally
			{
				if (!_hasNext)
				{
					Dispose();
				}
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_isDisposed)
			{
				return;
			}

			_enumerator.Dispose();
			_isDisposed = true;
		}

		private void TryMoveNext()
		{
			_hasNext = _enumerator.MoveNext();
			_nextRow = _hasNext ? _enumerator.Current : default;
		}
	}
}
