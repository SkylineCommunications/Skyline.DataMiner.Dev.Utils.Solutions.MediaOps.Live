namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Subscriptions
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects;

	public sealed class ApiObjectsChangedEvent<T>
		where T : ApiObject<T>
	{
		public ApiObjectsChangedEvent(IEnumerable<T> created, IEnumerable<T> updated, IEnumerable<T> deleted)
		{
			Created = created != null ? new List<T>(created) : new List<T>(0);
			Updated = updated != null ? new List<T>(updated) : new List<T>(0);
			Deleted = deleted != null ? new List<T>(deleted) : new List<T>(0);
		}

		public IReadOnlyList<T> Created { get; }

		public IReadOnlyList<T> Updated { get; }

		public IReadOnlyList<T> Deleted { get; }

		public override string ToString()
		{
			return $"{nameof(ApiObjectsChangedEvent<T>)}: {Created.Count} created, {Updated.Count} updated, {Deleted.Count} deleted";
		}
	}
}
