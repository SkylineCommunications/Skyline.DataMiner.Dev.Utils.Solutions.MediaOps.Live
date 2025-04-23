namespace Skyline.DataMiner.MediaOps.Live.API.Objects.SlcOrchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Net.Messages.SLDataGateway;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

	public class OrchestrationEventConfiguration : OrchestrationEvent
	{
		private Configuration configuration;

		public OrchestrationEventConfiguration() : this(new OrchestrationEventInstance(), new ConfigurationInstance())
		{
		}

		internal OrchestrationEventConfiguration(OrchestrationEventInstance domInstance, ConfigurationInstance configurationInstance) : base(domInstance)
		{
			configuration = new Configuration(configurationInstance);
		}

		internal OrchestrationEventConfiguration(DomInstance domInstance, DomInstance configurationInstance) : this(new OrchestrationEventInstance(domInstance), new ConfigurationInstance(configurationInstance))
		{
		}

		internal static DomDefinitionId DomDefinition => SlcOrchestrationIds.Definitions.OrchestrationEvent;

		public Configuration Configuration => configuration;

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

		internal void Save(DomHelper helper)
		{
			configuration.Save(helper);
			base.Save(helper);
		}
	}
}
