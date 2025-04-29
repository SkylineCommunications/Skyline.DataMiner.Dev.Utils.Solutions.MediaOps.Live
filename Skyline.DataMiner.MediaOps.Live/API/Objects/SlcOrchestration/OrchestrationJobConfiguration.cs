using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Skyline.DataMiner.MediaOps.Live.API.Objects.SlcOrchestration
{
	public class OrchestrationJobConfiguration : OrchestrationJob
	{
		private IList<OrchestrationEventConfiguration> _orchestrationEventConfigurations;
		
		public OrchestrationJobConfiguration(Guid jobId) : this (jobId, new List<OrchestrationEventConfiguration>())
		{
		}

		public OrchestrationJobConfiguration(Guid jobId, IList<OrchestrationEventConfiguration> orchestrationEventConfigurations) : base (jobId, orchestrationEventConfigurations)
		{
			_orchestrationEventConfigurations = orchestrationEventConfigurations;
		}

		private void ValidateConfigurationsBeforeSaving(IEnumerable<OrchestrationEvent> orchestrationEventConfigurations)
		{
			// IEnumerable<OrchestrationEvent> configurations = orchestrationEventConfigurations.ToList();
			// To be implemented
		}

		internal void ValidateEventsBeforeSaving(IEnumerable<OrchestrationEventConfiguration> orchestrationEventConfigurations)
		{
			IEnumerable<OrchestrationEventConfiguration> eventConfigurations = orchestrationEventConfigurations.ToList();
			ValidateEventsBeforeSaving();
			ValidateConfigurationsBeforeSaving(eventConfigurations);
		}

		public new IList<OrchestrationEventConfiguration> OrchestrationEvents
		{
			get
			{
				return _orchestrationEventConfigurations;
			}

			internal set
			{
				_orchestrationEventConfigurations = value;
			}
		}

	}
}
