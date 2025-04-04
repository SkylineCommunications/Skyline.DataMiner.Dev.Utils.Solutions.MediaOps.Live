using System;
using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;

namespace Skyline.DataMiner.MediaOps.Live.API.Objects.SlcOrchestration
{
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
	}
}
