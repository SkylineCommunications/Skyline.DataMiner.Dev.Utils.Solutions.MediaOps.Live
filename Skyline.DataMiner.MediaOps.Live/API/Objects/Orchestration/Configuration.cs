namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Model.SlcOrchestration;

	/// <summary>
	/// Configuration information for an orchestration event.
	/// </summary>
	public class Configuration : ApiObject<Configuration>
	{
		private readonly ConfigurationInstance _domInstance;

		private readonly WrappedList<NodeConfigurationSection, NodeConfiguration> _wrappedNodeConfigurations;
		private readonly WrappedList<ConnectionSection, Connection> _wrappedConnections;

		/// <summary>
		/// Initializes a new instance of the <see cref="Configuration"/> class.
		/// </summary>
		public Configuration() : this(new ConfigurationInstance())
		{
		}

		internal Configuration(ConfigurationInstance domInstance) : base(domInstance)
		{
			_domInstance = domInstance ?? throw new ArgumentNullException(nameof(domInstance));

			_wrappedNodeConfigurations = new WrappedList<NodeConfigurationSection, NodeConfiguration>(
				_domInstance.NodeConfiguration,
				x => new NodeConfiguration(x),
				x => x.DomSection);

			_wrappedConnections = new WrappedList<ConnectionSection, Connection>(
				_domInstance.Connection,
				x => new Connection(x),
				x => x.DomSection);
		}

		internal Configuration(DomInstance domInstance) : this(new ConfigurationInstance(domInstance))
		{
		}

		internal static DomDefinitionId DomDefinition => SlcOrchestrationIds.Definitions.Configuration;

		/// <summary>
		/// Gets or sets information for all nodes that need to be configured.
		/// </summary>
		public IList<NodeConfiguration> NodeConfigurations
		{
			get
			{
				return _wrappedNodeConfigurations;
			}

			set
			{
				_wrappedNodeConfigurations.Clear();
				_wrappedNodeConfigurations.AddRange(value);
			}
		}

		/// <summary>
		/// Gets or sets the information about the way nodes need to be connected.
		/// </summary>
		public IList<Connection> Connections
		{
			get
			{
				return _wrappedConnections;
			}

			set
			{
				_wrappedConnections.Clear();
				_wrappedConnections.AddRange(value);
			}
		}

		internal bool IsEmpty()
		{
			bool emptyNodes = NodeConfigurations == null || !NodeConfigurations.Any();
			bool emptyConnections = Connections == null || !Connections.Any();

			return emptyConnections && emptyNodes;
		}

		internal void Save(DomHelper helper)
		{
			_domInstance.Save(helper);
		}
	}
}
