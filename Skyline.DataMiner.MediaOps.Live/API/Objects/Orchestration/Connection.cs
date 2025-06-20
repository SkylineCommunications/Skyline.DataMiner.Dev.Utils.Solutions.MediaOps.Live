namespace Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration
{
	using System;
	using System.Collections.Generic;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;

	/// <summary>
	/// Configuration regarding a connection between nodes.
	/// </summary>
	public class Connection
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Connection"/> class.
		/// </summary>
		public Connection()
		{
			DomSection = new ConnectionSection();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Connection"/> class.
		/// </summary>
		/// <param name="domSection">DOM section.</param>
		/// <exception cref="ArgumentNullException">DOM section cannot be null.</exception>
		internal Connection(ConnectionSection domSection)
		{
			DomSection = domSection ?? throw new ArgumentNullException(nameof(domSection));
		}

		internal ConnectionSection DomSection { get; }

		/// <summary>
		/// Gets or sets the source node via node id.
		/// </summary>
		public string SourceNodeId
		{
			get
			{
				return DomSection.SourceNodeID;
			}

			set
			{
				DomSection.SourceNodeID = value;
			}
		}

		/// <summary>
		/// Gets or sets the destination node via node id.
		/// </summary>
		public string DestinationNodeId
		{
			get
			{
				return DomSection.DestinationNodeID;
			}

			set
			{
				DomSection.DestinationNodeID = value;
			}
		}

		/// <summary>
		/// Gets or sets the virtual signal group to apply on the source node.
		/// </summary>
		public ApiObjectReference<VirtualSignalGroup>? SourceVsg
		{
			get
			{
				return DomSection.SourceVSG;
			}

			set
			{
				DomSection.SourceVSG = value;
			}
		}

		/// <summary>
		/// Gets or sets the virtual signal group to apply on the destination node.
		/// </summary>
		public ApiObjectReference<VirtualSignalGroup>? DestinationVsg
		{
			get
			{
				return DomSection.DestinationVSG;
			}

			set
			{
				DomSection.DestinationVSG = value;
			}
		}

		/// <summary>
		/// Gets or sets a customized level mapping collection to connect the source and destination virtual signal groups.
		/// </summary>
		public IList<LevelMapping> LevelMappings
		{
			get
			{
				return DomSection.LevelMappingList;
			}

			set
			{
				DomSection.LevelMappingList.Clear();
				DomSection.LevelMappingList.AddRange(value);
			}
		}
	}
}
