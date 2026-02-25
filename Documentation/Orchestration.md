# Orchestration

## General

The Orchestration module of the MediaOps Live solution allows for automated scheduling of orchestration events. By integrating the shipped API, any system can create and schedule these events to automate future orchestration actions, such as connecting and disconnecting signals and/or executing required scripts to perform additional DataMiner actions. Additionally, with the help of a dedicated customizable service, monitoring/alarming the status of the applied configurations is possible.

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

Orchestration events are the core of the Orchestration module. Each event represents a collection of specific actions that need to be executed at a scheduled time.

### Create an event

```csharp
OrchestrationEvent orchestrationEvent = new OrchestrationEvent
{
    Name = "Event Name",
    EventType = SlcOrchestrationIds.Enums.EventType.Other,
    EventState = SlcOrchestrationIds.Enums.EventState.Draft,
    EventTime = DateTimeOffset.Now + TimeSpan.FromHours(1),
}
```

## Orchestration Jobs

Events are always part of a job. This job provides context and allows grouping multiple events together, such as start and stop events, or pre-roll and post-roll events.

### Create/Find a job

```csharp
MediaOpsLiveApi api = engine.GetMediaOpsLiveApi();
OrchestrationJob orchestrationJob = api.Orchestration.GetOrCreateNewOrchestrationJob("MyJobReference");
```

### Add an event to a job

```csharp
orchestrationJob.OrchestrationEvents.Add(orchestrationEvent);
```

### Save a job

```csharp
api.Orchestration.SaveOrchestrationJob(orchestrationJob);
```

> [!NOTE]
> When saving a job, the following validations are performed:
>
> - A job cannot contain events that were already part of another job.
> - If a starting event (event of type 'Start' or 'PrerollStart') exists, a stopping event (event of type 'Stop' or 'PostrollStop') must also exist (and vice versa).
> - Only one starting and one stopping event are allowed per job.
> - The starting event must be scheduled before the stopping event.

> [!TIP]
> When a job only requires a single event, such as for a one-time action, the event type 'Other' can be used.
> Events of this type are not considered for any validation rules.

## Orchestration Event Configurations

### Orchestration events <-> Orchestration event Configurations

While orchestration events do follow the full workflow and will be scheduled for execution, they do not contain any actual configuration. The main purpose of the orchestration event object is to provide the most important information about the event, such as name, type, state and time, without loading the full added configuration.

To provide any actual orchestration information, the orchestration event configuration object is used, which extends the options from the orchestration event object.

### Create an orchestration event with configuration

```csharp
OrchestrationEventConfiguration orchestrationEventConfiguration = new OrchestrationEventConfiguration
{
    Name = "Event Name",
    EventType = SlcOrchestrationIds.Enums.EventType.Other,
    EventState = SlcOrchestrationIds.Enums.EventState.Draft,
    EventTime = DateTimeOffset.Now + TimeSpan.FromHours(1),
}
```

## Orchestration Job Configurations

Events are always part of a job. This job provides context and allows grouping multiple events together, such as start and stop events, or pre-roll and post-roll events.

### Create/Find a job configuration

```csharp
MediaOpsLiveApi api = engine.GetMediaOpsLiveApi();
OrchestrationJobConfiguration orchestrationJobConfiguration = api.Orchestration.GetOrCreateNewOrchestrationJobConfiguration("MyJobReference");
```

### Add an event to a job

```csharp
orchestrationJobConfiguration.OrchestrationEvents.Add(orchestrationEventConfiguration);
```

### Save a job

```csharp
api.Orchestration.SaveOrchestrationJobConfiguration(orchestrationJobConfiguration);
```

## Scheduling events

### On saving a job

Once the job is saved, all events with the 'Confirmed' event state will be scheduled for execution at the defined time. This is done via the DataMiner scheduler module.
If at any point, a job and its events are updated, the scheduling will be updated accordingly or removed (e.g. if the event was 'Cancelled').

### Manually executing an event

If needed, an event can also be executed immediately, regardless of its scheduled time. The executed event can also be a completely new event that is not yet part of a job.
If the executed event was already scheduled in the future, the scheduled instance will be removed.

```csharp
api.Orchestration.ExecuteEventsNow(new List<OrchestrationEvent> { orchestrationEvent });
```

```csharp
api.Orchestration.ExecuteEventsNow(new List<OrchestrationEventConfiguration> { orchestrationEventConfiguration });
```

> [!NOTE]
> Executing events that already executed in the past is not allowed.

## Orchestration Configurations

### Add nodes to an event

To specify a collection of resources that require orchestration actions, nodes can be added to an event configuration.

```csharp
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

```csharp
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

Without additional configuration, the above connection will fully connect all available signals on the source and destination VSG.
In case a more refined connection is needed, level mapping can be added.

In the example below, we only want to connect audio and video signals between the two nodes. Additionally, the audio channels are shuffled. Any other signals will not be connected.

```csharp
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
> The 'Connections' property is used both to define connections, as well as disconnections. Currently there is no customization available to define which action needs to be performed.
> By default, if the event type is 'Start' or 'PrerollStart', connections will be made. If the event type is 'Stop' or 'PostrollStop', disconnections will be made.

### Add orchestration scripts to an event

For more custom actions, scripts can be added to an event configuration. Furthermore, scripts can be added on either an event (global) level or on the node configuration level.
When executing an event, the orchestration will only consider the global script or the scripts of the nodes. When both global and node scripts are required, the node scripts can be orchestrated from the global script.

```csharp
OrchestrationEventConfiguration orchestrationEventConfiguration = new OrchestrationEventConfiguration
{
    Name = "Event Name",
    EventType = SlcOrchestrationIds.Enums.EventType.Other,
    EventState = SlcOrchestrationIds.Enums.EventState.Draft,
    EventTime = DateTimeOffset.Now + TimeSpan.FromHours(1),
    GlobalOrchestrationScript = "NameOfMyOrchestrationScript",
}
```

### Provide input to an orchestration script

When the orchestration script requires input parameters these can be provided via the OrchestrationScriptArguments property.

```csharp
    GlobalOrchestrationScriptArguments =
    {
        new OrchestrationScriptArgument(OrchestrationScriptArgumentType.Parameter, "ScriptParameter1Name", "ScriptParameter1Value"),
        new OrchestrationScriptArgument(OrchestrationScriptArgumentType.Parameter, "ScriptParameter2Name", "ScriptParameter2Value"),
    },
```

To add script dummies, the OrchestrationScriptArgumentType.Element type can be used. As value, either the name or the ID of the dummy element can be used (AgentId/ElementId).

```csharp
    GlobalOrchestrationScriptArguments =
    {
        new OrchestrationScriptArgument(OrchestrationScriptArgumentType.Element, "ScriptDummyName", "ElementNameOrId"),
    }
```

Lastly, custom metadata information can be forwarded to the script. This information is not critical for the script to start, but can provide additional information to be used inside of the script.

```csharp
    GlobalOrchestrationScriptArguments =
    {
        new OrchestrationScriptArgument(OrchestrationScriptArgumentType.Metadata, "MetadataParameterName", "MetadataParameterValue"),
    }
```

### Use a profile as input to an orchestration script

Some orchestration scripts need specific profile instances or profile parameters as input. (See Orchestration Scripts).
Additionally, profiles can also be used to provide values for script input parameters. In this case, matching is done based on the parameter names.

Profile information can be provided as a whole profile instance:

```csharp
    Profile = 
    {
        Definition = "NameOfProfileDefinition",
        Instance = "NameOfProfileInstance",
    }
```

or as a list of profile parameters. In this case, the profile definition and instance are not required:

```csharp
    Profile =
    {
        Values =
        {
            new OrchestrationProfileValue
            {
                Name = "Integer Parameter Name",
                Value = new ParameterValue
                {
                    DoubleValue = 123,
                    Type = ParameterValue.ValueType.Double,
                },
            },
            new OrchestrationProfileValue
            {
                Name = "String Parameter Name",
                Value = new ParameterValue
                {
                    StringValue = "StringValue",
                    Type = ParameterValue.ValueType.String,
                },
            },
        },
    },
```

Both options can also be combined, in which case additional parameters can be provided next to the profile instance.

> [!NOTE]
> Only a single profile instance can be provided per orchestration script configuration. If multiple profile instances are required, the configuration should be split up in different events.
> Alternatively, the profile instance can also be loaded from within the script itself.

## Orchestration Scripts

### Get available orchestration scripts

The Orchestration module provides a way to retrieve all available orchestration scripts in the system.

```csharp
List<string> orchestrationScripts = api.Orchestration.Scripts.GetOrchestrationScripts();
```

### Request script input information

It is possible to request all required input information for a specific orchestration script.
This will return the following information:

- ProfileDefinition: The GUID of the profile definition that is required for the script.
- Parameters: A list of input parameters that are required by the script.
    This is a combination of profile parameters that are not part of the profile definition and script parameters.
- Elements: A list of script dummies that are required by the script. The required protocol and version is also provided.

```csharp
OrchestrationScriptInputInfo scriptInputInformation = api.Orchestration.Scripts.GetOrchestrationScriptInputInfo("NameOfOrchestrationScript");

Guid definition = scriptInputInformation.ProfileDefinition;
List<OrchestrationScriptInputParameter> parameters = scriptInputInformation.Parameters;
List<OrchestrationScriptInputElement> elements = scriptInputInformation.Elements;
```

### Get a list of available script input profile instances

From the requested script input information, all available profile instances can be retrieved. A profile helper is required to perform this action.

```csharp
ProfileHelper profileHelper = new ProfileHelper(engine.SendSLNetMessages);
List<ProfileInstance> availableInstances = scriptInputInformation.GetApplicableInstances(profileHelper);
```

### Get a list of available elements for a script dummy

Although the script input information provides all the element requirements, it is also possible to immediately retrieve a list of valid elements.

```csharp
OrchestrationScriptInputElement orchestrationScriptInputElement = elements.First();
orchestrationScriptInputElement.GetApplicableElements(engine.GetUserConnection());
```
