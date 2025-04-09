namespace Skyline.DataMiner.MediaOps.Live.DOM.Helpers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Model.SlcOrchestration;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.DOM.Tools;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	public class SlcOrchestrationHelper : DomModuleHelperBase
	{
		public SlcOrchestrationHelper(Func<DMSMessage[], DMSMessage[]> messageHandler) : base(SlcOrchestrationIds.ModuleId, messageHandler)
		{
		}

		public SlcOrchestrationHelper(IEngine engine) : base(SlcOrchestrationIds.ModuleId, engine)
		{
		}

		#region Orchestration Events

		public IEnumerable<OrchestrationEventInstance> GetAllOrchestrationEvents()
		{
			var filter = DomInstanceExposers.DomDefinitionId.Equal(SlcOrchestrationIds.Definitions.OrchestrationEvent.Id);

			return GetOrchestrationEventIterator(filter);
		}

		public IEnumerable<OrchestrationEventInstance> GetAllOrchestrationEvents(string jobReference)
		{
			var filter = DomInstanceExposers.DomDefinitionId.Equal(SlcOrchestrationIds.Definitions.OrchestrationEvent.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.JobReference).Equal(jobReference));

			return GetOrchestrationEventIterator(filter);
		}

		public IEnumerable<OrchestrationEventInstance> GetOrchestrationEvents(FilterElement<DomInstance> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			return GetOrchestrationEventIterator(filter);
		}

		#endregion

		#region Iterators

		private IEnumerable<OrchestrationEventInstance> GetOrchestrationEventIterator(FilterElement<DomInstance> filter)
		{
			return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, x => new OrchestrationEventInstance(x));
		}

		#endregion
	}
}
