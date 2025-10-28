namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	/// <summary>
	/// Represents a connection request between source and destination virtual signal groups.
	/// </summary>
	public class VsgConnectionRequest
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VsgConnectionRequest"/> class.
		/// </summary>
		/// <param name="source">The source virtual signal group.</param>
		/// <param name="destination">The destination virtual signal group.</param>
		/// <param name="levelMappings">Optional collection of level mappings. If not specified, all matching levels will be connected.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="destination"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown when the source doesn't have the 'Source' role or the destination doesn't have the 'Destination' role.</exception>
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

		/// <summary>
		/// Gets the source virtual signal group.
		/// </summary>
		public VirtualSignalGroup Source { get; }

		/// <summary>
		/// Gets the destination virtual signal group.
		/// </summary>
		public VirtualSignalGroup Destination { get; }

		/// <summary>
		/// Gets the collection of level mappings defining which levels to connect.
		/// </summary>
		public ICollection<LevelMapping> LevelMappings { get; }

		/// <summary>
		/// Gets a value indicating whether all levels should be connected.
		/// </summary>
		public bool IsConnectAllLevels => LevelMappings == null || LevelMappings.Count == 0;

		/// <summary>
		/// Gets or sets metadata associated with this connection request.
		/// </summary>
		public object MetaData { get; set; }
	}
}
