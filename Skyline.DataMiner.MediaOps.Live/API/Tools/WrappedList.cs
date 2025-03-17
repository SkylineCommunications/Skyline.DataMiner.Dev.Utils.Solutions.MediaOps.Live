namespace Skyline.DataMiner.MediaOps.Live.API.Tools
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;

	[DebuggerDisplay("Count = {Count}")]
	public class WrappedList<TInput, TOutput> : IList<TOutput>
	{
		private readonly IList<TInput> _wrappedList;
		private readonly Func<TInput, TOutput> _transform;
		private readonly Func<TOutput, TInput> _reverseTransform;

		public WrappedList(IList<TInput> wrappedCollection, Func<TInput, TOutput> transform, Func<TOutput, TInput> reverseTransform)
		{
			_wrappedList = wrappedCollection ?? throw new ArgumentNullException(nameof(wrappedCollection));
			_transform = transform ?? throw new ArgumentNullException(nameof(transform));
			_reverseTransform = reverseTransform ?? throw new ArgumentNullException(nameof(reverseTransform));
		}

		public TOutput this[int index]
		{
			get => _transform(_wrappedList[index]);
			set => _wrappedList[index] = _reverseTransform(value);
		}

		public int Count => _wrappedList.Count;

		public bool IsReadOnly => _wrappedList.IsReadOnly;

		public void Add(TOutput item)
		{
			if (item == null)
			{
				throw new ArgumentNullException(nameof(item));
			}

			_wrappedList.Add(_reverseTransform(item));
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

			return _wrappedList.Contains(_reverseTransform(item));
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
			return _wrappedList.Select(_transform).GetEnumerator();
		}

		public int IndexOf(TOutput item)
		{
			if (item == null)
			{
				throw new ArgumentNullException(nameof(item));
			}

			return _wrappedList.IndexOf(_reverseTransform(item));
		}

		public void Insert(int index, TOutput item)
		{
			if (item == null)
			{
				throw new ArgumentNullException(nameof(item));
			}

			_wrappedList.Insert(index, _reverseTransform(item));
		}

		public bool Remove(TOutput item)
		{
			if (item == null)
			{
				throw new ArgumentNullException(nameof(item));
			}

			return _wrappedList.Remove(_reverseTransform(item));
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
