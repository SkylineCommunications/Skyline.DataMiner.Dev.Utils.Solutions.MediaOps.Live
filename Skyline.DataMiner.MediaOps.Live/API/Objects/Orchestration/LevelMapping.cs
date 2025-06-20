namespace Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration
{
	/// <summary>
	/// Contains information on a specific mapping between a source and destination level.
	/// </summary>
	public class LevelMapping
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LevelMapping"/> class.
		/// </summary>
		/// <param name="source">Source level.</param>
		/// <param name="destination">Destination level.</param>
		public LevelMapping(Level source, Level destination)
		{
			Source = source;
			Destination = destination;
		}

		/// <summary>
		/// Gets or sets the source level.
		/// </summary>
		public Level Source { get; set; }

		/// <summary>
		/// Gets or sets the destination level.
		/// </summary>
		public Level Destination { get; set; }
	}
}
