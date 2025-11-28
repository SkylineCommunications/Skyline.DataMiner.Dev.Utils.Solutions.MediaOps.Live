namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public class VsgConnectionRequest
	{
		public VsgConnectionRequest(VirtualSignalGroup source, VirtualSignalGroup destination, ICollection<LevelMapping> levelMappings = null)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (destination == null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			if (!source.IsSource)
			{
				throw new ArgumentException("Source must have role 'Source'", nameof(source));
			}

			if (!destination.IsDestination)
			{
				throw new ArgumentException("Destination must have role 'Destination'", nameof(destination));
			}

			Source = source;
			Destination = destination;
			LevelMappings = levelMappings ?? [];
		}

		public VirtualSignalGroup Source { get; }

		public VirtualSignalGroup Destination { get; }

		public ICollection<LevelMapping> LevelMappings { get; }

		public bool IsConnectAllLevels => LevelMappings == null || LevelMappings.Count == 0;
	}
}
