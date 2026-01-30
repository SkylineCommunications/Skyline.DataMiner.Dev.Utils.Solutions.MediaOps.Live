namespace Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Helpers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Tools;

	internal class SlcOrchestrationHelper : DomModuleHelperBase
	{
		public SlcOrchestrationHelper(IConnection connection) : base(SlcOrchestrationIds.ModuleId, connection)
		{
		}

		#region Iterators

		private IEnumerable<OrchestrationEventInstance> GetOrchestrationEventIterator(FilterElement<DomInstance> filter)
		{
			return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, x => new OrchestrationEventInstance(x));
		}

		private IEnumerable<OrchestrationJobInfoInstance> GetJobInfoIterator(FilterElement<DomInstance> filter)
		{
			return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, x => new OrchestrationJobInfoInstance(x));
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
			OrchestrationJobInfoInstance jobInfoInstance = GetJobInfoById(jobReference);

			if (jobInfoInstance == null)
			{
				return new List<OrchestrationEventInstance>();
			}

			FilterElement<DomInstance> filter = DomInstanceExposers.DomDefinitionId.Equal(SlcOrchestrationIds.Definitions.OrchestrationEvent.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.JobInformation).Equal(jobInfoInstance.ID));

			return GetOrchestrationEventIterator(filter);
		}

		public OrchestrationJobInfoInstance GetJobInfoById(string jobReference)
		{
			FilterElement<DomInstance> jobInfoFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcOrchestrationIds.Definitions.OrchestrationJobInfo.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.JobInfo.JobReference).Equal(jobReference));

			OrchestrationJobInfoInstance jobInfoInstance = GetJobInfoIterator(jobInfoFilter).FirstOrDefault();
			return jobInfoInstance;
		}

		public IEnumerable<OrchestrationEventInstance> GetOrchestrationEvents(FilterElement<DomInstance> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			return GetOrchestrationEventIterator(filter);
		}

		public IEnumerable<OrchestrationJobInfoInstance> GetJobInfos(FilterElement<DomInstance> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			return GetJobInfoIterator(filter);
		}

		public IEnumerable<OrchestrationEventInstance> GetOrchestrationEventsInTimeRange(DateTime start, DateTime end)
		{
			DateTime localStart = start.ToLocalTime();
			DateTime localEnd = end.ToLocalTime();

			if (localStart > localEnd)
			{
				throw new ArgumentException("End time of range filter can not be lower than start time");
			}

			FilterElement<DomInstance> filter = DomInstanceExposers.DomDefinitionId.Equal(SlcOrchestrationIds.Definitions.OrchestrationEvent.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventTime).GreaterThanOrEqual(localStart))
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventTime).LessThanOrEqual(localEnd));

			return GetOrchestrationEventIterator(filter);
		}

		public IEnumerable<OrchestrationEventInstance> GetOrchestrationEventsAfterTime(DateTime time)
		{
			DateTime localStart = time.ToLocalTime();

			FilterElement<DomInstance> filter = DomInstanceExposers.DomDefinitionId.Equal(SlcOrchestrationIds.Definitions.OrchestrationEvent.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventTime).GreaterThanOrEqual(localStart));

			return GetOrchestrationEventIterator(filter);
		}

		public IEnumerable<OrchestrationEventInstance> GetOrchestrationEventsBeforeTime(DateTime time)
		{
			DateTime localEnd = time.ToLocalTime();

			FilterElement<DomInstance> filter = DomInstanceExposers.DomDefinitionId.Equal(SlcOrchestrationIds.Definitions.OrchestrationEvent.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventTime).LessThanOrEqual(localEnd));

			return GetOrchestrationEventIterator(filter);
		}

		public void SaveOrchestrationEventInstances(IEnumerable<OrchestrationEventInstance> eventInstances)
		{
			DomHelper.DomInstances.CreateOrUpdate(eventInstances.Select(inst => inst.ToInstance()).ToList());
		}

		#endregion
	}
}