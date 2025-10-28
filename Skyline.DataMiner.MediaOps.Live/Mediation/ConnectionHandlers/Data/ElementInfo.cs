namespace Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers.Data
{
	/// <summary>
	/// Contains information about a DataMiner element.
	/// </summary>
	public class ElementInfo
	{
		/// <summary>
		/// Gets or sets the agent ID.
		/// </summary>
		public int AgentId { get; set; }

		/// <summary>
		/// Gets or sets the hosting agent ID.
		/// </summary>
		public int HostingAgentId { get; set; }

		/// <summary>
		/// Gets or sets the element ID.
		/// </summary>
		public int ElementId { get; set; }

		/// <summary>
		/// Gets or sets the element name.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the protocol name.
		/// </summary>
		public string Protocol { get; set; }

		/// <summary>
		/// Gets or sets the protocol version.
		/// </summary>
		public string Version { get; set; }
	}
}
