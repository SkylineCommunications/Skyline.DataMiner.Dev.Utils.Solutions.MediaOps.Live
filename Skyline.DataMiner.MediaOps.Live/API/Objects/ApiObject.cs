namespace Skyline.DataMiner.MediaOps.Live.API.Objects
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.DOM.Model;

	public abstract class ApiObject<T> : IEquatable<ApiObject<T>>
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
			if (other == null)
			{
				return false;
			}

			return DomInstance.Equals(other.DomInstance);
		}

		public override string ToString()
		{
			return $"{typeof(T).Name} [{ID}]";
		}
	}
}
