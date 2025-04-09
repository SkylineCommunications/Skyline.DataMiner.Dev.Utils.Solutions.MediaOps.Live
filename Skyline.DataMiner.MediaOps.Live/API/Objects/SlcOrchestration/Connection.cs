namespace Skyline.DataMiner.MediaOps.Live.API.Objects.SlcOrchestration
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using SlcConnectivityManagement;

	public class Connection
	{
		public Connection()
		{
			DomSection = new ConnectionSection();
		}

		internal Connection(ConnectionSection domSection)
		{
			DomSection = domSection ?? throw new ArgumentNullException(nameof(domSection));
		}

		internal ConnectionSection DomSection { get; }

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
