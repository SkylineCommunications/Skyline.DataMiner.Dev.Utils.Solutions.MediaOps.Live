namespace Skyline.DataMiner.MediaOps.Live.DOM.Helpers
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.MediaOps.Live.DOM.Tools;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	public class SlcOrchestrationHelper : DomModuleHelperBase
	{
		public SlcOrchestrationHelper(ICommunication communication) : base(SlcOrchestrationIds.ModuleId, communication.SendMessages)
		{
		}

		public SlcOrchestrationHelper(IConnection connection) : base(SlcOrchestrationIds.ModuleId, connection.HandleMessages)
		{
		}

		#region Iterators

		private IEnumerable<OrchestrationEventInstance> GetOrchestrationEventIterator(FilterElement<DomInstance> filter)
		{
			return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, x => new OrchestrationEventInstance(x));
		}

		#endregion

		#region Orchestration Events

		public IEnumerable<OrchestrationEventInstance> GetAllOrchestrationEvents()
		{
			ManagedFilter<DomInstance, Guid> filter = DomInstanceExposers.DomDefinitionId.Equal(SlcOrchestrationIds.Definitions.OrchestrationEvent.Id);

			return GetOrchestrationEventIterator(filter);
		}

		public IEnumerable<OrchestrationEventInstance> GetAllOrchestrationEvents(string jobReference)
		{
			FilterElement<DomInstance> filter = DomInstanceExposers.DomDefinitionId.Equal(SlcOrchestrationIds.Definitions.OrchestrationEvent.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.JobReference).Equal(jobReference));

			return GetOrchestrationEventIterator(filter);
		}

		public IEnumerable<OrchestrationEventInstance> GetOrchestrationEvents(FilterElement<DomInstance> filter)
		{
			if (filter == null) throw new ArgumentNullException(nameof(filter));

			return GetOrchestrationEventIterator(filter);
		}

		public IEnumerable<OrchestrationEventInstance> GetOrchestrationEventsInTimeRange(DateTime start, DateTime end)
		{
			DateTime localStart = start.ToLocalTime();
			DateTime localEnd = end.ToLocalTime();

			if (localStart > localEnd) throw new ArgumentException("End time of range filter can not be lower than start time");

			FilterElement<DomInstance> filter = DomInstanceExposers.DomDefinitionId.Equal(SlcOrchestrationIds.Definitions.OrchestrationEvent.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventTime).GreaterThanOrEqual(localStart))
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventTime).LessThanOrEqual(localEnd));

			return GetOrchestrationEventIterator(filter);
		}

		#endregion
	}
}