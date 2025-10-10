namespace Skyline.DataMiner.MediaOps.Live.API.Tools
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;

	[DebuggerDisplay("Count = {Count}")]
	internal class WrappedList<TInput, TOutput> : IList<TOutput>
	{
		private readonly IList<TInput> _wrappedList;
		private readonly Func<TInput, TOutput> _wrapFunc;
		private readonly Func<TOutput, TInput> _unwrapFunc;

		public WrappedList(IList<TInput> wrappedCollection, Func<TInput, TOutput> wrapFunc, Func<TOutput, TInput> unwrapFunc)
		{
			_wrappedList = wrappedCollection ?? throw new ArgumentNullException(nameof(wrappedCollection));
			_wrapFunc = wrapFunc ?? throw new ArgumentNullException(nameof(wrapFunc));
			_unwrapFunc = unwrapFunc ?? throw new ArgumentNullException(nameof(unwrapFunc));
		}

		public TOutput this[int index]
		{
			get => _wrapFunc(_wrappedList[index]);
			set => _wrappedList[index] = _unwrapFunc(value);
		}

		public int Count => _wrappedList.Count;

		public bool IsReadOnly => _wrappedList.IsReadOnly;

		public void Add(TOutput item)
		{
			if (item == null)
			{
				throw new ArgumentNullException(nameof(item));
			}

			_wrappedList.Add(_unwrapFunc(item));
		}

		public void AddRange(IEnumerable<TOutput> items)
		{
			if (items == null)
			{
				throw new ArgumentNullException(nameof(items));
			}

			foreach (var item in items)
			{
				Add(item);
			}
		}

		public void Clear()
		{
			_wrappedList.Clear();
		}

		public bool Contains(TOutput item)
		{
			if (item == null)
			{
				throw new ArgumentNullException(nameof(item));
			}

			return _wrappedList.Contains(_unwrapFunc(item));
		}

		public void CopyTo(TOutput[] array, int arrayIndex)
		{
			if (array == null)
			{
				throw new ArgumentNullException(nameof(array));
			}

			if (arrayIndex < 0 || arrayIndex + Count > array.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(arrayIndex));
			}

			foreach (var item in this)
			{
				array[arrayIndex++] = item;
			}
		}

		public IEnumerator<TOutput> GetEnumerator()
		{
			return _wrappedList.Select(_wrapFunc).GetEnumerator();
		}

		public int IndexOf(TOutput item)
		{
			if (item == null)
			{
				throw new ArgumentNullException(nameof(item));
			}

			return _wrappedList.IndexOf(_unwrapFunc(item));
		}

		public void Insert(int index, TOutput item)
		{
			if (item == null)
			{
				throw new ArgumentNullException(nameof(item));
			}

			_wrappedList.Insert(index, _unwrapFunc(item));
		}

		public bool Remove(TOutput item)
		{
			if (item == null)
			{
				throw new ArgumentNullException(nameof(item));
			}

			return _wrappedList.Remove(_unwrapFunc(item));
		}

		public void RemoveAt(int index)
		{
			_wrappedList.RemoveAt(index);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
