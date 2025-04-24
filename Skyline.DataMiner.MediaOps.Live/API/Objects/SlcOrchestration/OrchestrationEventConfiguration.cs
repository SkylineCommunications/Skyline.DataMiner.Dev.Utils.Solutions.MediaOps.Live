namespace Skyline.DataMiner.MediaOps.Live.API.Objects.SlcOrchestration
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

	public class OrchestrationEventConfiguration : OrchestrationEvent
	{
		private Configuration configuration;

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationEventConfiguration"/> class.
		/// </summary>
		public OrchestrationEventConfiguration() : this(new OrchestrationEventInstance(), new ConfigurationInstance())
		{
		}
		
		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationEventConfiguration"/> class, inheriting the data from the given <see cref="OrchestrationEventInstance"/> and <see cref="ConfigurationInstance"/> object.
		/// </summary>
		internal OrchestrationEventConfiguration(OrchestrationEventInstance domInstance, ConfigurationInstance configurationInstance) : base(domInstance)
		{
			configuration = new Configuration(configurationInstance);
			ConfigurationReference = configurationInstance.ID.Id;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationEventConfiguration"/> class, inheriting the data from the given <see cref="DomInstance"/> objects.
		/// </summary>
		internal OrchestrationEventConfiguration(DomInstance eventInstance, DomInstance configurationInstance) : this(new OrchestrationEventInstance(eventInstance), new ConfigurationInstance(configurationInstance))
		{
		}

		internal static DomDefinitionId DomDefinition => SlcOrchestrationIds.Definitions.OrchestrationEvent;

		public Configuration Configuration => configuration;

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

		internal void ApplyConfiguration(Configuration configurationToApply)
		{
			configuration = configurationToApply;
		}

		internal void ApplyConfiguration(DomInstance configurationDomInstance)
		{
			if (configurationDomInstance == null)
			{
				configuration = null;
			}

			configuration = new Configuration(configurationDomInstance);
		}
	}
}
