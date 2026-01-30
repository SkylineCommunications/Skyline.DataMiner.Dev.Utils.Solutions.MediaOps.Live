namespace Skyline.DataMiner.Solutions.MediaOps.Live.Take
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;

	public class VsgDisconnectRequest : DisconnectRequest
	{
		public VsgDisconnectRequest(VirtualSignalGroup destination, ICollection<ApiObjectReference<Level>> levels = null)
		{
			if (destination == null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			if (!destination.IsDestination)
			{
				throw new ArgumentException("Destination must have role 'Destination'", nameof(destination));
			}

			Destination = destination;
			Levels = levels ?? [];
		}

		public VirtualSignalGroup Destination { get; }

		public ICollection<ApiObjectReference<Level>> Levels { get; }

		public bool IsDisconnectAllLevels => Levels == null || Levels.Count == 0;
	}
}
