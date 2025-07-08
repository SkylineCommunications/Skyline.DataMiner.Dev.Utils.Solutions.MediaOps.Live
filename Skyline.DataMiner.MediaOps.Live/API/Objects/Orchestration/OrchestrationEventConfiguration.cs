namespace Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

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

		internal OrchestrationEventConfiguration(OrchestrationEventInstance domInstance, ConfigurationInstance configurationInstance) : base(domInstance: domInstance)
		{
			_configuration = new Configuration(configurationInstance);
			ConfigurationReference = configurationInstance.ID.Id;
		}

		internal OrchestrationEventConfiguration(DomInstance eventInstance, DomInstance configurationInstance) : this(new OrchestrationEventInstance(eventInstance), new ConfigurationInstance(configurationInstance))
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
				var startingEvents = new List<SlcOrchestrationIds.Enums.EventType>
				{
					SlcOrchestrationIds.Enums.EventType.Start,
					SlcOrchestrationIds.Enums.EventType.Prerollstart,
					SlcOrchestrationIds.Enums.EventType.Prerollstop,
				};

				return startingEvents.Contains(EventType);
			}
		}

		internal bool IsStopEvent
		{
			get
			{
				var stoppingEvents = new List<SlcOrchestrationIds.Enums.EventType>
				{
					SlcOrchestrationIds.Enums.EventType.Stop,
					SlcOrchestrationIds.Enums.EventType.Postrollstart,
					SlcOrchestrationIds.Enums.EventType.Postrollstop,
				};

				return stoppingEvents.Contains(EventType);
			}
		}

		internal bool HasScripts()
		{
			bool global = !String.IsNullOrEmpty(GlobalOrchestrationScript);

			bool node = Configuration.NodeConfigurations.Any(nodeConfig => !String.IsNullOrEmpty(nodeConfig.OrchestrationScriptName));

			return global || node;
		}
	}
}
