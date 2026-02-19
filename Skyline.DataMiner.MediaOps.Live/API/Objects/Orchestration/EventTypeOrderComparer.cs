namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;

	public sealed class EventTypeOrderComparer : IComparer<EventType>
	{
		private readonly Dictionary<EventType, int> orderByType;

		public EventTypeOrderComparer(IEnumerable<EventType> order)
		{
			if (order == null)
			{
				throw new ArgumentNullException(nameof(order));
			}

			orderByType = order
				.Select((type, index) => new { type, index })
				.GroupBy(x => x.type)
				.ToDictionary(x => x.Key, x => x.First().index);
		}

		public int Compare(EventType x, EventType y)
		{
			bool hasX = orderByType.TryGetValue(x, out int orderX);
			bool hasY = orderByType.TryGetValue(y, out int orderY);

			if (hasX && hasY)
			{
				return orderX.CompareTo(orderY);
			}

			if (hasX)
			{
				return -1;
			}

			if (hasY)
			{
				return 1;
			}

			return x.CompareTo(y);
		}
	}
}
