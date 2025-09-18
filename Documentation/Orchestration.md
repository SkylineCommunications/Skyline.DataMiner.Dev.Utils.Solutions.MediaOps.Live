# Orchestration

## General
The Orchestration module of the MediaOps Live solution allows for automated scheduling of orchestration events.
By integrating the shipped API, any system can created and schedule these events to automate future orchestration actions,
such as connecting and disconnecting signals and/or executing required scripts to perform additional DataMiner actions.
Additionally, with the help of a dedicated customizable service, monitoring/alarming the status of the applied configurations is possible.

## Components

The Orchestration module makes use of the following DataMiner features:
- Automation scripts
- Scheduler
- DOM
- Profiles
- Services


> [!NOTE]
> A DataMiner version of at least 10.5.7 is required.

## Orchestration Events

Orchestration events are the core of the Orchestration module.
Each event represents a collection of specific action that needs to be executed at a scheduled time.

### Create an event
```
OrchestrationEvent orchestrationEvent = new OrchestrationEvent
{
	Name = "Event Name",
	EventType = SlcOrchestrationIds.Enums.EventType.Other,
	EventState = SlcOrchestrationIds.Enums.EventState.Draft,
	EventTime = DateTimeOffset.Now + TimeSpan.FromHours(1),
}
```

## Orchestration Jobs
Events are always part of a job.
This job provides context and allows grouping multiple events together, such as start and stop events, or preroll and postroll events.

### Create/Find a job
```
MediaOpsLiveApi api = engine.GetMediaOpsLiveApi();
OrchestrationJob orchestrationJob = api.Orchestration.GetOrCreateNewOrchestrationJob("MyJobReference");
```

### Add an event to a job
```
orchestrationJob.OrchestrationEvents.Add(orchestrationEvent);
```

### Save a job
```
api.Orchestration.SaveOrchestrationJob(orchestrationJob);
```

> [!NOTE]
> When saving a job, the following validations are performed:
> - A job can not contain events that were already part of another job.
>
> - If a starting event exists, a stopping event must also exist (and vice versa).
> - Only one starting and one stopping event are allowed per job.
> - The starting event must be scheduled before the stopping event.
> 
> A starting event is defined as an event with an EventType of 'Start' or 'PrerollStart'.
> A stopping event is defined as an event with an EventType of 'Stop' or 'PostRollStop'.

> [!TIP]
> When a job only requires a single event, such as for a one-time action, the event type 'Other' can be used.
> Events of this type are not considered for any validation rules.

## Orchestration Event Configurations

### Orchestration events <-> Orchestration event Configurations
While orchestration events do follow the full workflow and will be scheduled for execution, they do not contain any actual configuration.
The main purpose of the orchestration event object is to provide the most important information about the event, such as name, type, state and time,
without loading the full added configuration.

To provide any actual orchestration information, the orchestration event configuration object is used,
which extends the options from the orchestration event object

### Create an orchestration event with configuration
```
OrchestrationEventConfiguration orchestrationEventConfiguration = new OrchestrationEventConfiguration
{
	Name = "Event Name",
	EventType = SlcOrchestrationIds.Enums.EventType.Other,
	EventState = SlcOrchestrationIds.Enums.EventState.Draft,
	EventTime = DateTimeOffset.Now + TimeSpan.FromHours(1),
}
```

## Orchestration Job Configurations
Events are always part of a job.
This job provides context and allows grouping multiple events together, such as start and stop events, or preroll and postroll events.

### Create/Find a job configuration 
```
MediaOpsLiveApi api = engine.GetMediaOpsLiveApi();
OrchestrationJobConfiguration orchestrationJobConfiguration = api.Orchestration.GetOrCreateNewOrchestrationJobConfiguration("MyJobReference");
```

### Add an event to a job
```
orchestrationJobConfiguration.OrchestrationEvents.Add(orchestrationEventConfiguration);
```

### Save a job
```
api.Orchestration.SaveOrchestrationJobConfiguration(orchestrationJobConfiguration);
```

## Scheduling events

### On saving a job
Once the job is saved, all events with the 'Confirmed' event state will be scheduled for execution at the defined time.
This is done via the DataMiner scheduler module.
If at any point, a job and it's events are updated, the scheduling will be updated accordingly or removed (e.g. if the event was 'Cancelled').

### Manually executing an event
If needed, an event can also be executed immediately, regardless of it's scheduled time.
The executed event can also be a completely new event that is not yet part of a job.
If the executed event was already scheduled in the future, the scheduled instance will be removed.

```
api.Orchestration.ExecuteEventsNow(new List<OrchestrationEvent> { orchestrationEvent });
```

```
api.Orchestration.ExecuteEventsNow(new List<OrchestrationEventConfiguration> { orchestrationEventConfiguration });
```

> [!NOTE]
> Executing events that already executed in the past is not allowed.

## Orchestration Actions

### Add nodes to an event
To specify a collection of resources that require orchestration actions, nodes can be added to an event configuration.
```
OrchestrationEventConfiguration orchestrationEventConfiguration = new OrchestrationEventConfiguration
{
	Name = "Event Name",
	EventType = SlcOrchestrationIds.Enums.EventType.Other,
	EventState = SlcOrchestrationIds.Enums.EventState.Draft,
	EventTime = DateTimeOffset.Now + TimeSpan.FromHours(1),
	Configuration =
	{
		NodeConfigurations =
		{
			new NodeConfiguration
			{
				NodeId = "1",
				NodeLabel = "Node Label",
			},
		},
	},
};
```

### Add connections between nodes
When multiple nodes are added to an event configuration, connections between these nodes can be defined, by referencing the NodeId of the nodes.
A reference to the Virtual Signal Groups gives meaning to the connection and will be used during execution to connect/disconnect the actual signals.

```
	Configuration =
	{
		NodeConfigurations =
		{
			new NodeConfiguration
			{
				NodeId = "1",
				NodeLabel = "Node Label 1",
			},
			new NodeConfiguration
			{
				NodeId = "2",
				NodeLabel = "Node Label 2",
			},
		},
		Connections =
		{
			new Connection
			{
				SourceNodeId = "1",
				DestinationNodeId = "2",
				SourceVsg = Guid.NewGuid(), // Instance ID of the VSG
				DestinationVsg = Guid.NewGuid(), // Instance ID of the VSG
			},
		},
	},
```

Without additional configuration, the above connection will fully connect all available signals on the the source and destination VSG.
In case a more refined connection is needed, level mapping can be added.

In the example below, we only want to connect audio and video signals between the two nodes.
Additionally, the audio channels are shuffled.
Any other signals will not be connected.
```
		Connections =
		{
			new Connection
			{
				SourceNodeId = "1",
				DestinationNodeId = "2",
				SourceVsg = Guid.NewGuid(), // Instance ID of the VSG
				DestinationVsg = Guid.NewGuid(), // Instance ID of the VSG
				LevelMappings =
				{
					new LevelMapping(new Level("Audio1", 1), new Level("Audio2", 2)),
					new LevelMapping(new Level("Audio2", 2), new Level("Audio1", 1)),
					new LevelMapping(new Level("Video", 3), new Level("Video", 3)),
				},
			},
		},
```

> [!IMPORTANT]
> The 'Connections' property is used both to define connections, as well as disconnections.
> Currently there is no customization available to define which action needs to be performed.
> By default, if the event type is 'Start' or 'PrerollStart', connections will be made.
> If the event type is 'Stop' or 'PostRollStop', disconnections will be made.

### Add orchestration scripts to an event
For more custom actions, scripts can be added to an event configuration.
Furthermore, scripts can be added on either an event (global) level or on the node level.



