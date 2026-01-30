namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tools
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	internal class OneToManyMapping<TParent, TChild>
	{
		#region Private Fields

		private readonly IEqualityComparer<TParent> _parentComparer;
		private readonly IEqualityComparer<TChild> _childComparer;

		private readonly Dictionary<TParent, ICollection<TChild>> _childrenByParent;
		private readonly Dictionary<TChild, TParent> _parentByChild;

		#endregion

		public OneToManyMapping(
			IEqualityComparer<TParent> parentComparer = null,
			IEqualityComparer<TChild> childComparer = null)
		{
			_parentComparer = parentComparer ?? EqualityComparer<TParent>.Default;
			_childComparer = childComparer ?? EqualityComparer<TChild>.Default;

			_childrenByParent = new Dictionary<TParent, ICollection<TChild>>(_parentComparer);
			_parentByChild = new Dictionary<TChild, TParent>(_childComparer);
		}

		#region Public Properties

		/// <summary>
		/// Gets the mapping of each parent to its children.
		/// </summary>
		public IReadOnlyDictionary<TParent, ICollection<TChild>> ChildrenByParent => _childrenByParent;

		/// <summary>
		/// Gets the mapping of each child to its parent.
		/// </summary>
		public IReadOnlyDictionary<TChild, TParent> ParentByChild => _parentByChild;

		public int ParentCount => _childrenByParent.Count;

		public int ChildCount => _parentByChild.Count;

		#endregion

		#region Public Methods

		public void Add(TParent parent, TChild child)
		{
			if (parent == null)
				throw new ArgumentNullException(nameof(parent));
			if (child == null)
				throw new ArgumentNullException(nameof(child));

			if (_parentByChild.ContainsKey(child))
				throw new InvalidOperationException("Child already has a parent.");

			if (!_childrenByParent.TryGetValue(parent, out var children))
			{
				children = new HashSet<TChild>(_childComparer);
				_childrenByParent[parent] = children;
			}

			children.Add(child);
			_parentByChild[child] = parent;
		}

		public void AddOrUpdate(TParent parent, TChild child)
		{
			if (parent == null)
				throw new ArgumentNullException(nameof(parent));
			if (child == null)
				throw new ArgumentNullException(nameof(child));

			if (ContainsChild(child))
			{
				RemoveChild(child);
			}

			Add(parent, child);
		}

		public IEnumerable<TChild> GetChildren(TParent parent)
		{
			if (parent == null)
				throw new ArgumentNullException(nameof(parent));

			return _childrenByParent.TryGetValue(parent, out var children)
				? children
				: Enumerable.Empty<TChild>();
		}

		public bool TryGetChildren(TParent parent, out IReadOnlyCollection<TChild> children)
		{
			if (parent == null)
				throw new ArgumentNullException(nameof(parent));

			if (_childrenByParent.TryGetValue(parent, out var set))
			{
				children = set as IReadOnlyCollection<TChild>;
				return true;
			}

			children = [];
			return false;
		}

		public TParent GetParent(TChild child)
		{
			if (child == null)
				throw new ArgumentNullException(nameof(child));

			_parentByChild.TryGetValue(child, out var parent);
			return parent;
		}

		public bool TryGetParent(TChild child, out TParent parent)
		{
			if (child == null)
				throw new ArgumentNullException(nameof(child));

			return _parentByChild.TryGetValue(child, out parent);
		}

		public void RemoveParent(TParent parent)
		{
			if (parent == null)
				throw new ArgumentNullException(nameof(parent));

			if (!_childrenByParent.TryGetValue(parent, out var children))
			{
				return;
			}

			foreach (var child in children)
			{
				_parentByChild.Remove(child);
			}

			_childrenByParent.Remove(parent);
		}

		public void RemoveChild(TChild child)
		{
			if (child == null)
				throw new ArgumentNullException(nameof(child));

			if (!_parentByChild.TryGetValue(child, out var parent))
			{
				return;
			}

			_parentByChild.Remove(child);

			if (_childrenByParent.TryGetValue(parent, out var children))
			{
				children.Remove(child);
				if (children.Count == 0)
					_childrenByParent.Remove(parent);
			}
		}

		public void Clear()
		{
			_childrenByParent.Clear();
			_parentByChild.Clear();
		}

		public bool Contains(TParent parent, TChild child)
		{
			if (parent == null)
				throw new ArgumentNullException(nameof(parent));
			if (child == null)
				throw new ArgumentNullException(nameof(child));

			return _childrenByParent.TryGetValue(parent, out var children) &&
				children.Contains(child);
		}

		public bool ContainsParent(TParent parent)
		{
			if (parent == null)
				throw new ArgumentNullException(nameof(parent));

			return _childrenByParent.ContainsKey(parent);
		}

		public bool ContainsChild(TChild child)
		{
			if (child == null)
				throw new ArgumentNullException(nameof(child));

			return _parentByChild.ContainsKey(child);
		}

		public override string ToString()
		{
			return $"OneToManyMapping<{typeof(TParent).Name}, {typeof(TChild).Name}> [Parents: {ParentCount}, Children: {ChildCount}]";
		}

		#endregion
	}
}
