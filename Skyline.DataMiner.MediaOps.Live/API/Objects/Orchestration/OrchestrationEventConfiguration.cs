namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Model.SlcOrchestration;

	/// <summary>
	/// This type inherits the information from <see cref="OrchestrationEvent"/> and exposes the full event configuration.
	/// </summary>
	public class OrchestrationEventConfiguration : OrchestrationEvent
	{
		private readonly Configuration _configuration;

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationEventConfiguration"/> class.
		/// </summary>
		public OrchestrationEventConfiguration() : this(new OrchestrationEventInstance(), new ConfigurationInstance())
		{
		}

		internal OrchestrationEventConfiguration(OrchestrationEventInstance domInstance, ConfigurationInstance configurationInstance)
			: base(domInstance)
		{
			_configuration = new Configuration(configurationInstance);
			ConfigurationReference = configurationInstance.ID.Id;
		}

		internal OrchestrationEventConfiguration(DomInstance eventInstance, DomInstance configurationInstance)
			: this(new OrchestrationEventInstance(eventInstance), new ConfigurationInstance(configurationInstance))
		{
		}

		/// <summary>
		/// Gets the configuration for this event.
		/// </summary>
		public Configuration Configuration => _configuration;

		/// <summary>
		/// Gets or sets the script that will execute during the global orchestration step of the event.
		/// </summary>
		public new string GlobalOrchestrationScript
		{
			get
			{
				return base.GlobalOrchestrationScript;
			}

			set
			{
				base.GlobalOrchestrationScript = value;
			}
		}

		/// <summary>
		/// Gets or sets a list of input arguments for the script in case a global orchestration script is set to execute.
		/// </summary>
		public new IList<OrchestrationScriptArgument> GlobalOrchestrationScriptArguments
		{
			get
			{
				return base.GlobalOrchestrationScriptArguments;
			}

			set
			{
				base.GlobalOrchestrationScriptArguments = value;
			}
		}

		/// <summary>
		/// Gets or sets profile information to be used as global orchestration script input.
		/// </summary>
		public new OrchestrationProfile Profile
		{
			get
			{
				return base.Profile;
			}

			set
			{
				base.Profile = value;
			}
		}

		internal bool IsStartEvent
		{
			get
			{
				var startingEvents = new List<EventType>
				{
					EventType.Start,
					EventType.PrerollStart,
				};

				return startingEvents.Contains(EventType);
			}
		}

		internal bool IsStopEvent
		{
			get
			{
				var stoppingEvents = new List<EventType>
				{
					EventType.Stop,
					EventType.PostrollStop,
				};

				return stoppingEvents.Contains(EventType);
			}
		}

		internal bool IsConnectEvent
		{
			get
			{
				var connectEvents = new List<EventType>
				{
					EventType.Start,
					EventType.PrerollStart,
					EventType.PrerollStop,
				};

				return connectEvents.Contains(EventType);
			}
		}

		internal bool IsDisconnectEvent
		{
			get
			{
				var disconnectEvents = new List<EventType>
				{
					EventType.Stop,
					EventType.PostrollStart,
					EventType.PostrollStop,
				};

				return disconnectEvents.Contains(EventType);
			}
		}

		internal bool HasGlobalOrchestrationScript => !String.IsNullOrEmpty(GlobalOrchestrationScript);

		internal bool HasScripts
		{
			get
			{
				bool global = HasGlobalOrchestrationScript;

				bool node = Configuration.NodeConfigurations.Any(nodeConfig => !String.IsNullOrEmpty(nodeConfig.OrchestrationScriptName));

				return global || node;
			}
		}

		internal bool HasConnections => Configuration?.Connections != null && Configuration.Connections.Any();
	}
}
