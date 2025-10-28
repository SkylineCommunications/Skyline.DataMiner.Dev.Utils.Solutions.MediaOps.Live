namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	/// <summary>
	/// Represents a request to disconnect a destination virtual signal group.
	/// </summary>
	public class VsgDisconnectRequest
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VsgDisconnectRequest"/> class.
		/// </summary>
		/// <param name="destination">The destination virtual signal group to disconnect.</param>
		/// <param name="levels">Optional collection of level references to disconnect. If not specified, all levels will be disconnected.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="destination"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown when the destination doesn't have the 'Destination' role.</exception>
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

		/// <summary>
		/// Gets the destination virtual signal group to disconnect.
		/// </summary>
		public VirtualSignalGroup Destination { get; }

		/// <summary>
		/// Gets the collection of level references to disconnect.
		/// </summary>
		public ICollection<ApiObjectReference<Level>> Levels { get; }

		/// <summary>
		/// Gets a value indicating whether all levels should be disconnected.
		/// </summary>
		public bool IsDisconnectAllLevels => Levels == null || Levels.Count == 0;

		/// <summary>
		/// Gets or sets metadata associated with this disconnect request.
		/// </summary>
		public object MetaData { get; set; }
	}
}
