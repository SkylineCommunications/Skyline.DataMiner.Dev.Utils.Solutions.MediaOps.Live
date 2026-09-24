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
using Skyline.DataMiner.Solutions.MediaOps.Live.API;
using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;

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
using Skyline.DataMiner.Solutions.MediaOps.Live.API;
using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;

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
using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;

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

## Orchestration Job Configurations

Events are always part of a job. This job provides context and allows grouping multiple events together, such as start and stop events, or pre-roll and post-roll events.

### Create/Find a job configuration

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Live.API;
using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;

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
using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;

api.Orchestration.ExecuteEventsNow(new List<OrchestrationEvent> { orchestrationEvent });
```

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;

api.Orchestration.ExecuteEventsNow(new List<OrchestrationEventConfiguration> { orchestrationEventConfiguration });
```

> [!NOTE]
> Executing events that already executed in the past is not allowed.

## Orchestration Configurations

### Add nodes to an event

To specify a collection of resources that require orchestration actions, nodes can be added to an event configuration.

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;

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
using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;

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
using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;

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
using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;

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
using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;

GlobalOrchestrationScriptArguments =
{
    new OrchestrationScriptArgument(OrchestrationScriptArgumentType.Parameter, "ScriptParameter1Name", "ScriptParameter1Value"),
    new OrchestrationScriptArgument(OrchestrationScriptArgumentType.Parameter, "ScriptParameter2Name", "ScriptParameter2Value"),
},
```

To add script dummies, the OrchestrationScriptArgumentType.Element type can be used. As value, either the name or the ID of the dummy element can be used (AgentId/ElementId).

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;

GlobalOrchestrationScriptArguments =
{
    new OrchestrationScriptArgument(OrchestrationScriptArgumentType.Element, "ScriptDummyName", "ElementNameOrId"),
}
```

Lastly, custom metadata information can be forwarded to the script. This information is not critical for the script to start, but can provide additional information to be used inside of the script.

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;

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
using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;

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

### Provide input to a dynamic orchestration script

A dynamic orchestration script (see [Dynamic orchestration scripts](#dynamic-orchestration-scripts)) takes its input values by field path.
The values are stored with the event, in the profile of the event or node configuration. Only provide the values that differ from the defaults.

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs;

var profile = new OrchestrationProfile();
profile.SetInputValues(new OrchestrationInputValues(new Dictionary<string, OrchestrationInputValue>
{
    ["General/Number of destinations"] = 2,
    ["Destinations/Destination 1/Endpoint"] = "ENC-A",
    ["Destinations/Destination 2/Endpoint"] = "ENC-B",
    ["Schedule/Start"] = new DateTime(2026, 9, 24, 12, 0, 0, DateTimeKind.Utc),
    ["Schedule/Pre-roll"] = TimeSpan.FromMinutes(10),
}));

OrchestrationInputValues stored = profile.GetInputValues();
```

When an event is confirmed, the script evaluates its inputs again with the stored values. The event is rejected when a value is missing or no longer valid.

## Orchestration Scripts

### Dynamic orchestration scripts

A classic orchestration script derives from `OrchestrationScript` and returns a fixed list of profile parameters and definitions from `GetParameters()`.
A dynamic orchestration script derives from `DynamicOrchestrationScript` instead. Its inputs can depend on the values that were already provided:

- inputs can appear or disappear, for example the settings of the selected transport standard;
- groups can be repeated, for example once per destination;
- options, ranges and defaults can change, for example the frequency range of the selected band.

Existing scripts keep working unchanged. Pick the base class that matches the contract you need.

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script;
using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs;

public class Script : DynamicOrchestrationScript
{
    public override OrchestrationInputDefinition GetInputs(IEngine engine, OrchestrationInputValues providedValues)
    {
        var isKuBand = providedValues.HasValue("Downlink/Band", "Ku");
        var destinationCount = providedValues.GetInt32("General/Number of destinations", 1, 8);

        return new OrchestrationInputBuilder()
            .AddGroup("General", general => general.AddNumber("Number of destinations", field =>
            {
                field.Minimum = 1;
                field.Maximum = 8;
                field.DefaultValue = 1;
                field.TriggersReevaluation = true;
            }))
            .AddGroup("Downlink", downlink =>
            {
                downlink.AddDiscrete("Band", field =>
                {
                    field.DefaultValue = "C";
                    field.TriggersReevaluation = true;
                }, "C", "Ku");

                downlink.AddNumber("Frequency", field =>
                {
                    field.Minimum = isKuBand ? 10.7 : 3.7;
                    field.Maximum = isKuBand ? 12.75 : 4.2;
                    field.Unit = "GHz";
                    field.IsRequired = true;
                });
            })
            .AddGroup("Destinations", destinations =>
            {
                for (var index = 1; index <= destinationCount; index++)
                {
                    destinations.AddGroup($"Destination {index}", destination => destination.AddText("Endpoint", field => field.IsRequired = true));
                }
            })
            .Build();
    }

    public override void Orchestrate(IEngine engine, OrchestrationInputValues inputs)
    {
        var frequency = inputs.GetNumber("Downlink/Frequency");
        var endpoint = inputs.GetString("Destinations/Destination 1/Endpoint");
    }
}
```

`GetInputs` is called every time a value changes of a field that has `TriggersReevaluation` set, and once more when the event is confirmed and executed.
It receives the engine, so the inputs can be derived from profile parameters, DOM instances or other data in the system.
It must not keep state between calls: everything it needs comes from `providedValues`.

#### Field types

| Builder method | Value | Type-specific properties |
| --- | --- | --- |
| `AddText` | text | |
| `AddNumber` | number | `Minimum`, `Maximum`, `StepSize`, `Decimals`, `Unit` |
| `AddDiscrete` | one of the options | `Options` (display text and value) |
| `AddDateTime` | date and time, in UTC | `Minimum`, `Maximum`, `Precision` |
| `AddTimeSpan` | duration | `Minimum`, `Maximum`, `Precision` |
| `AddProfileParameter` | as defined by the profile parameter | narrows the discretes or range of the profile parameter |
| `AddGroup` | none, it bundles other items | |

Every field also has `IsRequired`, `DefaultValue`, `Description`, `TriggersReevaluation` and `IsDisabled`.
For checks the definition can't express, such as two destinations using the same endpoint, set `IsValid` to `false` and explain why in `ValidationMessage`.

#### Paths

Every item has a name that is unique among its siblings. The path of a field is the chain of names from the top level, separated by `/`, for example `Destinations/Destination 2/Endpoint`.
The same name can be reused under different parents. Items can be nested at most five levels deep.

Read values in `Orchestrate` with `GetString`, `GetNumber`, `GetInt32`, `GetDateTime` or `GetTimeSpan` of `OrchestrationInputValues`, or with `GetInputValue(path)` of the script.

#### Validation

A definition that can't be used is rejected, with a message that names the item:

- duplicate names among siblings, or a name that contains `/`;
- items nested deeper than five levels, or an item that is added more than once;
- a range whose minimum is larger than its maximum, a step size that isn't positive, or a dropdown without options;
- a default value that the field itself doesn't accept.

Values are checked against the definition that was evaluated for them, and against `IsValid`, when a job or event is confirmed and before `Orchestrate` runs.
When the script is run manually, or with the option to ask for missing values, the operator is asked for the inputs that are missing or not valid.

#### Classic and dynamic inputs

Dynamic inputs belong to the script. They are stored by path with the event and don't take part in resource capability or capacity matching.
Classic scripts keep using profile parameters and profile definitions, and those are the only inputs that are matched against resources.

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
using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;

OrchestrationScriptInputInfo scriptInputInformation = api.Orchestration.Scripts.GetOrchestrationScriptInputInfo("NameOfOrchestrationScript");

Guid definition = scriptInputInformation.ProfileDefinition;
List<OrchestrationScriptInputParameter> parameters = scriptInputInformation.Parameters;
List<OrchestrationScriptInputElement> elements = scriptInputInformation.Elements;
```

For a dynamic orchestration script, `HasDynamicInputs` is `true` and `InputDefinition` holds its inputs.
Pass the values that were provided so far to get the inputs that apply to them. Request the inputs again whenever a field with `TriggersReevaluation` changes.

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs;

var providedValues = new OrchestrationInputValues(new Dictionary<string, OrchestrationInputValue>
{
    ["Downlink/Band"] = "Ku",
});

OrchestrationScriptInputInfo info = api.Orchestration.Scripts.GetOrchestrationScriptInputInfo("NameOfOrchestrationScript", providedValues);

if (info.HasDynamicInputs)
{
    foreach (OrchestrationInputField field in info.InputDefinition.GetAllFields())
    {
        string shownValue = field.FormatValue(field.GetEffectiveValue());
        bool isValid = field.TryValidate(out string error);
    }
}
```

`InputDefinition` is `null` for a classic script, and also when the script could not be executed, for example because it failed or timed out.

### Get a list of available script input profile instances

From the requested script input information, all available profile instances can be retrieved. A profile helper is required to perform this action.

```csharp
using Skyline.DataMiner.Net.Profiles;

ProfileHelper profileHelper = new ProfileHelper(engine.SendSLNetMessages);
List<ProfileInstance> availableInstances = scriptInputInformation.GetApplicableInstances(profileHelper);
```

### Get a list of available elements for a script dummy

Although the script input information provides all the element requirements, it is also possible to immediately retrieve a list of valid elements.

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;

OrchestrationScriptInputElement orchestrationScriptInputElement = elements.First();
orchestrationScriptInputElement.GetApplicableElements(engine.GetUserConnection());
```
