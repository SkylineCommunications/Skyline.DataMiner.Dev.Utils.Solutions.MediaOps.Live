namespace Skyline.DataMiner.MediaOps.Live.Take
{
	using API.Objects;
	using API.Objects.SlcConnectivityManagement;

	/// <summary>
	/// Represents a mapping between source and destination levels.
	/// </summary>
	public class LevelMapping
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LevelMapping"/> class.
		/// </summary>
		/// <param name="sourceLevel">The source level reference.</param>
		/// <param name="destinationLevel">The destination level reference.</param>
		public LevelMapping(ApiObjectReference<Level> sourceLevel, ApiObjectReference<Level> destinationLevel)
		{
			SourceLevel = sourceLevel;
			DestinationLevel = destinationLevel;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LevelMapping"/> class with the same source and destination level.
		/// </summary>
		/// <param name="level">The level reference for both source and destination.</param>
		public LevelMapping(ApiObjectReference<Level> level)
		{
			SourceLevel = level;
			DestinationLevel = level;
		}

		/// <summary>
		/// Gets the source level reference.
		/// </summary>
		public ApiObjectReference<Level> SourceLevel { get; }

		/// <summary>
		/// Gets the destination level reference.
		/// </summary>
		public ApiObjectReference<Level> DestinationLevel { get; }

		public void Deconstruct(out ApiObjectReference<Level> sourceLevel, out ApiObjectReference<Level> destinationLevel)
		{
			sourceLevel = SourceLevel;
			destinationLevel = DestinationLevel;
		}
	}
}