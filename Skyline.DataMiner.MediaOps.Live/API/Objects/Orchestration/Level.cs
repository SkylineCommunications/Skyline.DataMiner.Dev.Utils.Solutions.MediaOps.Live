namespace Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration
{
	/// <summary>
	/// Contains information about a specific endpoint level.
	/// </summary>
	public class Level
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Level"/> class.
		/// </summary>
		/// <param name="name">Level name.</param>
		/// <param name="number">Level number.</param>
		public Level(string name, int number)
		{
			Name = name;
			Number = number;
		}

		/// <summary>
		/// Gets or sets the level number.
		/// </summary>
		public int Number { get; set; }

		/// <summary>
		/// Gets or sets the level name.
		/// </summary>
		public string Name { get; set; }
	}
}
