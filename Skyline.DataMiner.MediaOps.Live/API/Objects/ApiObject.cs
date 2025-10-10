namespace Skyline.DataMiner.MediaOps.Live.API.Objects
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.DOM.Model;

	public abstract class ApiObject<T> : IApiObjectReference, IEquatable<ApiObject<T>>
		where T : ApiObject<T>
	{
		protected ApiObject(DomInstanceBase domInstance)
		{
			DomInstance = domInstance ?? throw new ArgumentNullException(nameof(domInstance));
		}

		internal DomInstanceBase DomInstance { get; }

		public Guid ID => DomInstance.ID.Id;

		public ApiObjectReference<T> Reference => new ApiObjectReference<T>(DomInstance.ID.Id);

		public override int GetHashCode()
		{
			return DomInstance.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as ApiObject<T>);
		}

		public virtual bool Equals(ApiObject<T> other)
		{
			if (ReferenceEquals(this, other))
			{
				return true;
			}

			if (other is null)
			{
				return false;
			}

			return DomInstance.Equals(other.DomInstance);
		}

		public static bool operator ==(ApiObject<T> left, ApiObject<T> right)
		{
			if (ReferenceEquals(left, right))
			{
				return true;
			}

			if (left is null || right is null)
			{
				return false;
			}

			return left.Equals(right);
		}

		public static bool operator !=(ApiObject<T> left, ApiObject<T> right)
		{
			return !(left == right);
		}

		public override string ToString()
		{
			if (!String.IsNullOrWhiteSpace(DomInstance.Name))
			{
				return $"{typeof(T).Name} '{DomInstance.Name}' [{ID}]";
			}

			return $"{typeof(T).Name} [{ID}]";
		}
	}
}
