namespace Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;

	/// <summary>
	/// Contains a node level configuration for event orchestration.
	/// </summary>
	public class NodeConfiguration
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NodeConfiguration"/> class.
		/// </summary>
		public NodeConfiguration()
		{
			DomSection = new NodeConfigurationSection();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NodeConfiguration"/> class.
		/// </summary>
		/// <param name="domSection">DOM section.</param>
		/// <exception cref="ArgumentNullException">DOM section can not be null.</exception>
		internal NodeConfiguration(NodeConfigurationSection domSection)
		{
			DomSection = domSection ?? throw new ArgumentNullException(nameof(domSection));
		}

		internal NodeConfigurationSection DomSection { get; }

		/// <summary>
		/// Gets or sets the node identifier.
		/// </summary>
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

		/// <summary>
		/// Gets or sets the node description.
		/// </summary>
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

		/// <summary>
		/// Gets or sets the script to call for node orchestration.
		/// </summary>
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

		/// <summary>
		/// Gets or sets the arguments to execute the node orchestration script.
		/// </summary>
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

		/// <summary>
		/// Gets or sets profile information to be used as node orchestration script input.
		/// </summary>
		public OrchestrationProfile Profile
		{
			get
			{
				return DomSection.Profile;
			}

			set
			{
				DomSection.Profile = value;
			}
		}
	}
}
