namespace Skyline.DataMiner.MediaOps.Live.Mediation.Element
{
	using Skyline.DataMiner.Core.DataMinerSystem.Common;

	/// <summary>
	/// Contains information about a mediated element.
	/// </summary>
	public class MediatedElementInfo
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MediatedElementInfo"/> class.
		/// </summary>
		/// <param name="id">The DataMiner element ID.</param>
		/// <param name="name">The element name.</param>
		public MediatedElementInfo(DmsElementId id, string name)
		{
			Id = id;
			Name = name;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MediatedElementInfo"/> class.
		/// </summary>
		/// <param name="dmaId">The DataMiner Agent ID.</param>
		/// <param name="elementId">The element ID.</param>
		/// <param name="name">The element name.</param>
		public MediatedElementInfo(int dmaId, int elementId, string name)
			: this(new DmsElementId(dmaId, elementId), name)
		{
		}

		/// <summary>
		/// Gets the DataMiner element ID.
		/// </summary>
		public DmsElementId Id { get; }

		/// <summary>
		/// Gets the element name.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets or sets the connection handler script name.
		/// </summary>
		public string ConnectionHandlerScript { get; internal set; }

		/// <summary>
		/// Gets or sets a value indicating whether the element is enabled for mediation.
		/// </summary>
		public bool IsEnabled { get; internal set; }
	}
}
