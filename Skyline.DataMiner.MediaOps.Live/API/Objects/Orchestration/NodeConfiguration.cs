namespace Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;

	public class NodeConfiguration
	{
		public NodeConfiguration()
		{
			DomSection = new NodeConfigurationSection();
		}

		internal NodeConfiguration(NodeConfigurationSection domSection)
		{
			DomSection = domSection ?? throw new ArgumentNullException(nameof(domSection));
		}

		internal NodeConfigurationSection DomSection { get; }

		public string NodeId
		{
			get
			{
				return DomSection.NodeID;
			}

			set
			{
				DomSection.NodeID = value;
			}
		}

		public string NodeLabel
		{
			get
			{
				return DomSection.NodeLabel;
			}

			set
			{
				DomSection.NodeLabel = value;
			}
		}

		public string OrchestrationScriptName
		{
			get
			{
				return DomSection.OrchestrationScriptName;
			}

			set
			{
				DomSection.OrchestrationScriptName = value;
			}
		}

		public IList<OrchestrationScriptArgument> OrchestrationScriptArguments
		{
			get
			{
				return DomSection.OrchestrationScriptArgumentsList;
			}

			set
			{
				DomSection.OrchestrationScriptArgumentsList.Clear();
				DomSection.OrchestrationScriptArgumentsList.AddRange(value);
			}
		}
	}
}
