namespace Skyline.DataMiner.MediaOps.Live.API.Objects
{
	using System;

	public readonly struct ApiObjectReference<T> : IApiObjectReference, IEquatable<ApiObjectReference<T>>
		where T : ApiObject<T>
	{
		public ApiObjectReference(Guid id)
		{
			ID = id;
		}

		public static ApiObjectReference<T> Empty { get; } = new ApiObjectReference<T>(Guid.Empty);

		public Guid ID { get; }

		public static implicit operator ApiObjectReference<T>(Guid id)
		{
			return new ApiObjectReference<T>(id);
		}

		public static implicit operator ApiObjectReference<T>(ApiObject<T> apiObject)
		{
			if (apiObject == null)
			{
				return Empty;
			}

			return apiObject.Reference;
		}

		public static implicit operator Guid(ApiObjectReference<T> reference)
		{
			return reference.ID;
		}

		public override bool Equals(object obj)
		{
			return obj is ApiObjectReference<T> reference && Equals(reference);
		}

		public bool Equals(ApiObjectReference<T> other)
		{
			return ID.Equals(other.ID);
		}

		public override int GetHashCode()
		{
			return ID.GetHashCode();
		}

		public static bool operator ==(ApiObjectReference<T> left, ApiObjectReference<T> right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(ApiObjectReference<T> left, ApiObjectReference<T> right)
		{
			return !(left == right);
		}

		public override string ToString()
		{
			return $"{typeof(T).Name} [{ID}]";
		}
	}
}
