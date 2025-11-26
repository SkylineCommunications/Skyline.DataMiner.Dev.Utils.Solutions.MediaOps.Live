namespace Skyline.DataMiner.MediaOps.Live.API.Repositories.Orchestration
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer;

	using SLDataGateway.API.Types.Querying;

	using Comparer = Skyline.DataMiner.Net.Messages.SLDataGateway.Comparer;

	internal class OrchestrationEventRepository : Repository<OrchestrationEvent>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationEventRepository"/> class.
		/// </summary>
		/// <param name="api">Api that calls the repository.</param>
		internal OrchestrationEventRepository(MediaOpsLiveApi api) : base(api, api.SlcOrchestrationHelper)
		{
		}

		protected internal override DomDefinitionId DomDefinition => OrchestrationEvent.DomDefinition;

		internal IEnumerable<OrchestrationEvent> GetOrchestrationEventsInTimeRange(DateTime start, DateTime end)
		{
			DateTime localStart = start.ToUniversalTime();
			DateTime localEnd = end.ToUniversalTime();

			if (localStart > localEnd)
			{
				throw new ArgumentException("End time of range filter can not be lower than start time");
			}

			FilterElement<DomInstance> filter = DomInstanceExposers.DomDefinitionId.Equal(SlcOrchestrationIds.Definitions.OrchestrationEvent.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventTime).GreaterThanOrEqual(localStart))
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventTime).LessThanOrEqual(localEnd));

			return Read(filter);
		}

		internal IEnumerable<OrchestrationEvent> GetOrchestrationEventsAfterTime(DateTime time)
		{
			DateTime localStart = time.ToUniversalTime();

			FilterElement<DomInstance> filter = DomInstanceExposers.DomDefinitionId.Equal(SlcOrchestrationIds.Definitions.OrchestrationEvent.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventTime).GreaterThanOrEqual(localStart));

			return Read(filter);
		}

		internal IEnumerable<OrchestrationEvent> GetOrchestrationEventsBeforeTime(DateTime time)
		{
			DateTime localEnd = time.ToUniversalTime();

			FilterElement<DomInstance> filter = DomInstanceExposers.DomDefinitionId.Equal(SlcOrchestrationIds.Definitions.OrchestrationEvent.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventTime).LessThanOrEqual(localEnd));

			return Read(filter);
		}

		/// <summary>
		///     Get all <see cref="OrchestrationEvent" /> objects that contains the given job reference value.
		/// </summary>
		/// <param name="jobInfo">Job reference object to filter.</param>
		/// <param name="performanceTracker">Performance tracking object.</param>
		/// <returns>A collection of <see cref="OrchestrationEvent" /> objects that contains the given job reference value.</returns>
		/// <exception cref="ArgumentException">Job reference can not be null or whitespace.</exception>
		internal IEnumerable<OrchestrationEvent> GetEventsByJobInfoReference(OrchestrationJobInfo jobInfo, PerformanceTracker performanceTracker)
		{
			using (new PerformanceTracker(performanceTracker))
			{
				if (jobInfo == null)
				{
					return new List<OrchestrationEvent>();
				}

				ManagedFilter<DomInstance, IEnumerable> filter = DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.JobInformation).Equal(jobInfo.ID);

				return Read(filter);
			}
		}

		/// <summary>
		///     Get the <see cref="OrchestrationEvent" /> object that matches the given event ID value.
		/// </summary>
		/// <param name="eventIds">The ID of the instance to lookup.</param>
		/// <param name="performanceTracker">Performance tracking object.</param>
		/// <returns>
		///     A <see cref="OrchestrationEvent" /> object that matches the given event ID value, or null if no match is
		///     found.
		/// </returns>
		/// <exception cref="ArgumentException">Event ID can not be an empty Guid.</exception>
		private IEnumerable<OrchestrationEvent> GetEventsById(IEnumerable<Guid> eventIds, PerformanceTracker performanceTracker)
		{
			using (new PerformanceTracker(performanceTracker))
			{
				return GetEventsById(eventIds);
			}
		}

		internal IEnumerable<OrchestrationEvent> GetEventsById(IEnumerable<Guid> eventIds)
		{
			List<Guid> instanceIds = eventIds.ToList();

			if (instanceIds == null || instanceIds.Any(guid => guid == Guid.Empty))
			{
				throw new ArgumentException($"'{nameof(eventIds)}' cannot contain empty Guids.", nameof(eventIds));
			}

			ORFilterElement<DomInstance> combinedFilter = new ORFilterElement<DomInstance>(instanceIds.Select(id => FilterElementFactory.Create(DomInstanceExposers.Id, Comparer.Equals, id)).ToArray());

			IEnumerable<OrchestrationEvent> result = Read(combinedFilter);

			return result;
		}

		protected internal override OrchestrationEvent CreateInstance(DomInstance domInstance)
		{
			return new OrchestrationEvent(domInstance);
		}

		protected override void ValidateBeforeSave(ICollection<OrchestrationEvent> instances)
		{
		}

		protected override void ValidateBeforeDelete(ICollection<OrchestrationEvent> instances)
		{
		}

		protected internal override FilterElement<DomInstance> CreateFilter(string fieldName, Comparer comparer, object value)
		{
			switch (fieldName)
			{
				case nameof(OrchestrationEvent.Name):
					return FilterElementFactory.Create<string>(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventName), comparer, value);

				case nameof(OrchestrationEvent.EventType):
					return FilterElementFactory.Create<int>(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventType), comparer, value);

				case nameof(OrchestrationEvent.EventState):
					return FilterElementFactory.Create<int>(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventState), comparer, value);

				case nameof(OrchestrationEvent.EventTime):
					return FilterElementFactory.Create<DateTimeOffset>(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventTime), comparer, value);

				case nameof(OrchestrationEvent.SchedulerReference):
					return FilterElementFactory.Create<string>(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.SchedulerReference), comparer, value);

				case nameof(OrchestrationEvent.JobInfoReference):
					return FilterElementFactory.Create<Guid>(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.JobInformation), comparer, value);

				case nameof(OrchestrationEvent.FailureInfo):
					return FilterElementFactory.Create<string>(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.FailureInfo), comparer, value);

				case nameof(OrchestrationEvent.GlobalOrchestrationScript):
					return FilterElementFactory.Create<string>(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.GlobalConfiguration.OrchestrationScriptName), comparer, value);
			}

			return base.CreateFilter(fieldName, comparer, value);
		}

		protected internal override IOrderByElement CreateOrderBy(string fieldName, SortOrder sortOrder, bool naturalSort = false)
		{
			switch (fieldName)
			{
				case nameof(OrchestrationEvent.Name):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventName), sortOrder, naturalSort);

				case nameof(OrchestrationEvent.EventType):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventType), sortOrder, naturalSort);

				case nameof(OrchestrationEvent.EventState):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventState), sortOrder, naturalSort);

				case nameof(OrchestrationEvent.EventTime):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.EventTime), sortOrder, naturalSort);

				case nameof(OrchestrationEvent.SchedulerReference):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.SchedulerReference), sortOrder, naturalSort);

				case nameof(OrchestrationEvent.JobInfoReference):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.JobInformation), sortOrder, naturalSort);

				case nameof(OrchestrationEvent.FailureInfo):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.OrchestrationEventInfo.FailureInfo), sortOrder, naturalSort);

				case nameof(OrchestrationEvent.GlobalOrchestrationScript):
					return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcOrchestrationIds.Sections.GlobalConfiguration.OrchestrationScriptName), sortOrder, naturalSort);
			}

			return base.CreateOrderBy(fieldName, sortOrder, naturalSort);
		}
	}
}