namespace Skyline.DataMiner.MediaOps.Live.API.Objects.SlcOrchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

	public class Configuration : ApiObject<Configuration>
	{
		private readonly ConfigurationInstance _domInstance;

		private readonly WrappedList<NodeConfigurationSection, NodeConfiguration> _wrappedNodeConfigurations;
		private readonly WrappedList<ConnectionSection, Connection> _wrappedConnections;

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
