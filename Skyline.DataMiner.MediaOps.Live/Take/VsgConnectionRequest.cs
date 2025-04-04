namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;
	using System.Collections.Generic;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.SlcConnectivityManagement;

	public class VsgConnectionRequest
	{
		public VsgConnectionRequest(VirtualSignalGroup source, VirtualSignalGroup destination, ICollection<Level> levels = null)
		{
			if (source == null)
			{
				// ignore
			}

			if (destination == null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			if (source != null && !source.IsSource)
			{
				throw new ArgumentException("Source must have role 'Source'", nameof(source));
			}

			if (!destination.IsDestination)
			{
				throw new ArgumentException("Destination must have role 'Destination'", nameof(destination));
			}

			Source = source;
			Destination = destination;
			Levels = levels;
		}

		public VirtualSignalGroup Source { get; }

		public VirtualSignalGroup Destination { get; }

		public ICollection<Level> Levels { get; }
	}
}
