namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	/// <summary>
	/// Represents a request to create a connection between two virtual signal groups.
	/// </summary>
	public class VsgConnectionRequest
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VsgConnectionRequest"/> class.
		/// </summary>
		/// <param name="source">The source virtual signal group.</param>
		/// <param name="destination">The destination virtual signal group.</param>
		/// <param name="levelMappings">Optional collection of level mappings. If null or empty, all matching levels will be connected.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> or <paramref name="destination"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown when the source does not have role 'Source' or the destination does not have role 'Destination'.</exception>
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
		/// Gets the collection of level mappings for this connection request.
		/// </summary>
		public ICollection<LevelMapping> LevelMappings { get; }

		/// <summary>
		/// Gets a value indicating whether all matching levels should be connected.
		/// </summary>
		public bool IsConnectAllLevels => LevelMappings == null || LevelMappings.Count == 0;

		/// <summary>
		/// Gets or sets optional metadata associated with this connection request.
		/// </summary>
		public object MetaData { get; set; }
	}
}
