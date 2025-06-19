namespace Skyline.DataMiner.MediaOps.Live.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.MediaOps.Live.DOM.Helpers;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcOrchestration;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Async;
	using Skyline.DataMiner.Net.Exceptions;
	using Skyline.DataMiner.Net.Messages;

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

			Connection = connection;
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

		private bool SchedulerTaskExists()
		{
			HashSet<OrchestrationSchedulerTask> list = new HashSet<OrchestrationSchedulerTask>();
			GetInfoMessage getSchedulerTaskInfoMessage = new GetInfoMessage(InfoType.SchedulerTasks);

			AsyncProgress progress = Connection.Async.Launch(getSchedulerTaskInfoMessage);

			AsyncResponseEvent result = progress.WaitForAsyncResponse(5 * 60);

			if (result == null || !result.Messages.Any())
			{
				throw new DataMinerException("Scheduler task information could not be retrieved");
			}

			if (result.Failure != null)
			{
				throw result.Failure;
			}

			GetSchedulerTasksResponseMessage getSchedulerTasksResponse = (GetSchedulerTasksResponseMessage)result.Messages.FirstOrDefault();

			if (getSchedulerTasksResponse == null)
			{
				throw new InvalidOperationException("Scheduler task could not be retrieved.");
			}

			return getSchedulerTasksResponse.Tasks.ToArray().Any(task => ((SchedulerTask)task).TaskName == Constants.OrchestrationSlidingWindowSchedulerTaskNaming);
		}

		public void SetupSchedulerTasks()
		{
			if (SchedulerTaskExists())
			{
				return;
			}

			IDms dms = Connection.GetDms();

			IDma dma = dms.GetAgents().First();

			dma.Scheduler.CreateTask(GenerateSchedulerTaskData());
		}

		private static object[] GenerateSchedulerTaskData()
		{
			return
			[
				new object[] { GenerateGeneralInfoTaskData() },
				new object[] { GenerateActionsTaskData() },
				new object[] { },
			];
		}

		private static string[] GenerateGeneralInfoTaskData()
		{
			return
			[
				Constants.OrchestrationSlidingWindowSchedulerTaskNaming,
				String.Empty,
				String.Empty,
				"00:00:00",
				"daily",
				$"{Constants.SlidingWindowSchedulerExecutionFrequencyInMinutes}",
				String.Empty,
				Constants.OrchestrationSlidingWindowSchedulerTaskNaming,
				"TRUE",
				String.Empty,
				String.Empty,
			];
		}

		private static string[] GenerateActionsTaskData()
		{
			return
			[
				"automation",
				Constants.OrchestrationSlidingWindowSchedulerScriptName,
				"CHECKSETS:FALSE",
				"DEFER:TRUE",
			];
		}

		private TimeSpan TimeSpanPast { get; }

		private TimeSpan TimeSpanFuture { get; }

		private DateTimeOffset WindowBaseTime { get; }

		private DateTimeOffset WindowEndTime => WindowBaseTime + TimeSpanFuture;

		private DateTimeOffset WindowStartTime => WindowBaseTime - TimeSpanPast;

		private IConnection Connection { get;  }
	}
}
