namespace Skyline.DataMiner.MediaOps.Live.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.Net;

	internal class OrchestrationSlidingWindowScheduler
	{
		private readonly OrchestrationScheduler _scheduler;
		private readonly SlcOrchestrationHelper _orchestrationHelper;
		private readonly OrchestrationCleanup _orchestrationCleanup;

		internal OrchestrationSlidingWindowScheduler(IConnection connection, TimeSpan timeSpanFuture) : this (connection, TimeSpan.FromMinutes(0), timeSpanFuture)
		{
		}

		internal OrchestrationSlidingWindowScheduler(IConnection connection, TimeSpan timeSpanPast, TimeSpan timeSpanFuture)
		{
			_orchestrationCleanup = new OrchestrationCleanup(connection);
			_scheduler = new OrchestrationScheduler(connection);
			_orchestrationHelper = new SlcOrchestrationHelper(connection);

			TimeSpanPast = timeSpanPast;
			TimeSpanFuture = timeSpanFuture;
			WindowBaseTime = DateTimeOffset.UtcNow;
		}

		public void SyncSchedulerWithWindow()
		{
			RemoveEventsBeforeWindow();
			CreateOrUpdateAllEventsInWindow();
		}

		private void RemoveEventsBeforeWindow()
		{
			_orchestrationCleanup.CleanupSchedulerTasksBeforeTime(WindowStartTime);
		}

		private void CreateOrUpdateAllEventsInWindow()
		{
			IEnumerable<OrchestrationEventInstance> eventInstancesInWindow = _orchestrationHelper.GetOrchestrationEventsInTimeRange(WindowBaseTime.UtcDateTime, WindowEndTime.UtcDateTime);
			_scheduler.CreateOrUpdateEventScheduling(eventInstancesInWindow.Select(instance => new OrchestrationEvent(instance)));
		}

		public void ScheduleEvents(IEnumerable<OrchestrationEvent> events)
		{
			IEnumerable<OrchestrationEvent> eventsInWindow = events.Where(e => e.EventTime > WindowBaseTime && e.EventTime <= WindowEndTime);
			_scheduler.CreateOrUpdateEventScheduling(eventsInWindow);
		}

		public void DeleteEvents(IEnumerable<OrchestrationEvent> events)
		{
			_scheduler.DeleteEventTasks(events);
		}

		public TimeSpan TimeSpanPast { get; set; }

		public TimeSpan TimeSpanFuture { get; set; }

		public DateTimeOffset WindowBaseTime { get; set; }

		private DateTimeOffset WindowEndTime => WindowBaseTime + TimeSpanFuture;

		private DateTimeOffset WindowStartTime => WindowBaseTime - TimeSpanPast;
	}
}
