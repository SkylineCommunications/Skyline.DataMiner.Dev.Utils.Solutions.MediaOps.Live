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
> Executing events that already executed in the past is not allowed. Use `RetryEvents` to run such an event again.

### Retry an event

An event that already ran can be run again, for example after it failed. Only events in the 'Configuring', 'Completed' or 'Failed' state can be retried.
Retrying a 'Draft', 'Confirmed' or 'Cancelled' event throws an `InvalidOperationException`.

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;

api.Orchestration.RetryEvents(new List<Guid> { orchestrationEvent.ID }, api.GetMediaOpsPlanHelper());
```

- The event runs as a whole again, with its global script or its node scripts, and ends as 'Completed' or 'Failed'.
- Node scripts that define phases continue where they stopped (see [Trigger an event again](#trigger-an-event-again)). Other scripts run in full again, so they must be safe to run more than once.
- The failure info of the event is cleared. The actual start time of the first run is kept.
- The retry runs in the calling process and returns when the event is done, or waits for a resume.

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
When executing an event, the orchestration will only consider the global script or the scripts of the nodes. When both global and node scripts are required, the node scripts can be orchestrated from the global script (see [Orchestrate nodes from a global script](#orchestrate-nodes-from-a-global-script)).

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
    ["Schedule/Start"] = new DateTimeOffset(2026, 9, 24, 12, 0, 0, TimeSpan.Zero),
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
It receives the engine, so options and ranges can be derived from DOM instances or other data in the system.
It must not remember values between calls: the values it works with always come from `providedValues`.
Within one request it can run a few times, until defaults no longer change the inputs, so cache data you read from the system in a field of the script.

Dynamic inputs don't use profile parameters, profile definitions or profile instances. Every input is defined by the script itself.

#### Field types

| Builder method | Value | Type-specific properties |
| --- | --- | --- |
| `AddText` | text | |
| `AddNumber` | number | `Minimum`, `Maximum`, `StepSize`, `Decimals`, `Unit` |
| `AddDiscrete` | one of the options | `Options` (display text and value) |
| `AddDateTime` | date and time (`DateTimeOffset`), held in UTC | `Minimum`, `Maximum`, `Precision` |
| `AddTimeSpan` | duration | `Minimum`, `Maximum`, `Precision` |
| `AddGroup` | none, it bundles other items | |

Every field also has `IsRequired`, `DefaultValue`, `Description`, `TriggersReevaluation` and `IsDisabled`.
For checks the definition can't express, such as two destinations using the same endpoint, set `IsValid` to `false` and explain why in `ValidationMessage`.

`SupportsLinks` tells whether an app such as MediaOps Plan may link the field to a value that is only known later, such as a job property or the name of the assigned resource.
It is `false` for date/time and duration fields. Fields with `TriggersReevaluation` aren't offered for linking either, because the structure of the inputs has to be known while configuring.

#### Presets

Where a classic script uses profile instances as presets, a dynamic script uses a dropdown of its own. Mark it with `TriggersReevaluation`
and derive the defaults of the other fields from the selected preset. A value the operator entered explicitly still wins over the preset.

```csharp
var preset = providedValues.GetString("Preset") ?? "HD";
var isUhd = preset == "UHD";

builder.AddDiscrete("Preset", field =>
{
    field.DefaultValue = "HD";
    field.TriggersReevaluation = true;
}, "HD", "UHD");

builder.AddNumber("Bitrate", field =>
{
    field.Unit = "Mbps";
    field.DefaultValue = isUhd ? 50 : 20;
});
```

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

#### Inputs and resource matching

Input parameters of orchestration events never take part in resource capability or capacity matching, for classic and dynamic scripts alike.
They only mean something to the script. Resources are matched on the capabilities and capacities of the node configuration.
Dynamic inputs are stored by path with the event.

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

`InputDefinition` is `null` for a classic script, which doesn't implement the orchestration script info entry point. When the script fails or times out while it determines its inputs, the request throws an exception instead, so a failing dynamic script is never mistaken for a classic one.

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

## Orchestration Phases

An orchestration script can be split into an ordered list of named **phases** instead of a single `Orchestrate` method.
Use phases when an orchestration takes several steps that can fail separately, has to wait for something, or must be safe to run again.
For example: start a cloud service, wait until it's available, then configure it.

The framework runs the phases, records the status of every phase, keeps data that later phases and events need, and waits for you.
When an event is triggered again, the orchestration continues where it stopped instead of starting over.

### Define phases

Override `GetPhases` in a class that derives from `OrchestrationScript` or `DynamicOrchestrationScript`:

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script;
using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases;

public class Script : OrchestrationScript
{
    public override IEnumerable<IOrchestrationParameters> GetParameters() => Enumerable.Empty<IOrchestrationParameters>();

    public override IEnumerable<OrchestrationPhase> GetPhases() => new OrchestrationPhase[]
    {
        Phase.Execute("spin-up", SpinUp).WithStatus(IsSpunUp),
        Phase.WaitFor("spin-up").PollEvery(TimeSpan.FromSeconds(15)).Timeout(TimeSpan.FromMinutes(30)),
        Phase.Execute("configure", Configure).WithStatus(IsConfigured),
    };

    private PhaseStatus SpinUp(OrchestrationPhaseContext context)
    {
        var cloudServiceId = cloud.StartService();
        context.Job.Values.Set("cloudServiceId", cloudServiceId);

        return PhaseStatus.InProgress($"Cloud service {cloudServiceId} requested.");
    }

    private PhaseStatus IsSpunUp(OrchestrationPhaseContext context)
    {
        if (!context.Job.Values.TryGet("cloudServiceId", out var cloudServiceId))
        {
            return PhaseStatus.NotStarted("No cloud service requested yet.");
        }

        return cloud.IsRunning(cloudServiceId)
            ? PhaseStatus.Completed($"Cloud service {cloudServiceId} is running.")
            : PhaseStatus.InProgress($"Cloud service {cloudServiceId} is provisioning.");
    }

    // Configure and IsConfigured follow the same pattern.
}
```

- `Phase.Execute(key, execute)` creates a phase that does work. The key identifies the phase across executions, so keep it stable and unique within the script.
- `WithStatus(status)` adds a status method that reports the actual state of the phase, for example by asking the device or the cloud.
  It is called before the phase is executed, on every trigger, and much more often than the phase itself, so it must be cheap and free of side effects.
- The set of phases is fixed: it can't depend on the input values. Input values only influence what a phase does.

> [!IMPORTANT]
> A status method must return within a few seconds. The framework doesn't limit how long a call takes, and the retries only cover calls that throw:
> a call that hangs blocks the script until the script timeout, and the timeout of a wait isn't checked in the meantime.
>
> - Give every external call a short timeout, for example `HttpClient.Timeout`, or a receive timeout on a socket.
> - Be careful with SDKs and policies that retry on their own; limit their retries or their total duration.
> - Don't poll inside a status method. Return `InProgress` and let a [wait phase](#wait-for-a-phase) poll.
>
> The same applies to phases themselves: a phase that takes long, such as provisioning a cloud service, should start the work, return `InProgress`, and leave the waiting to a wait phase.

> [!NOTE]
> When a script defines phases, `Orchestrate` is not called. Scripts that don't define phases keep working unchanged: `GetPhases` returns no phases by default and `Orchestrate` is called as before.
> `Orchestrate` no longer has to be overridden by a script that defines phases.

### Phase status

A phase reports a `PhaseStatus`: a state plus an optional message for the operator.

| Created with | State | Meaning |
| --- | --- | --- |
| `PhaseStatus.NotStarted(message)` | `NotStarted` | The phase still has to be executed. |
| `PhaseStatus.InProgress(message)` | `InProgress` | The phase was executed, but its result is not available yet. |
| `PhaseStatus.Completed(message)` | `Completed` | The phase is done. |
| `PhaseStatus.Failed(reason)` | `Failed` | The phase failed. The reason is shown to the operator. |

The message is optional and never influences the orchestration. It is trimmed to 255 characters.

Give a failed phase a reason that tells the operator what went wrong, for example `PhaseStatus.Failed("Encoder ENC-A is offline.")`.
The reason is available on `PhaseStatus.FailureReason`, on the phase of the event (see [Read the phases of an event](#read-the-phases-of-an-event)),
and in the failure info of the event: "Phase 'spin-up' failed: Encoder ENC-A is offline.". A failed phase doesn't add a stack trace to the failure info; an unexpected exception elsewhere in the script still does.

### How phases are run

The phases are walked in order. For every phase, the status method is called first, when the phase has one:

| Status | What happens |
| --- | --- |
| `Completed` | The phase is skipped. |
| `NotStarted` | The phase is executed. The status it returns is recorded and acted on in the same way. |
| `InProgress` | The phase is not executed again. The next phase is expected to be a wait on it. |
| `Failed` | The following phases are not executed and the event fails. |

The status method, not the recorded status, decides. A phase that completed earlier is executed again when its status method reports `NotStarted`, for example because the configuration of the device was changed.
A phase without a status method is skipped when it was recorded as `Completed`, and executed otherwise.

- A phase that throws an exception is marked `Failed`, with the exception message as reason.
- A status method that throws or returns `null` is retried up to 3 times. When it keeps failing, the phase is marked `Failed` with "Unable to retrieve status of 'spin-up' phase: ..." and is not executed.
- Aborting the script (`engine.ExitFail`, `engine.ExitSuccess`) and script timeouts are not caught; they end the script as usual.
- When the script times out, the phase that was busy is marked `Failed` first, for example "The script timed out while retrieving the status of phase 'spin-up'.",
  "The script timed out while executing phase 'spin-up'." or, on the phase after a wait, "The script timed out while waiting on phase 'spin-up'.".
  This is best effort: the script still ends when the status can't be stored in time.
- When a phase fails, the script fails, and so does the event.

### Wait for a phase

A wait phase polls the status of another phase until it reports `Completed`. It has no key and isn't shown as a phase of its own:
what it waits for is reported on the phase defined after it, for example "Waiting on phase 'spin-up'.".

```csharp
// A phase of the same node. It must be defined before the wait and have a status method.
Phase.WaitFor("spin-up")
     .PollEvery(TimeSpan.FromSeconds(15))     // default: 10 seconds
     .Timeout(TimeSpan.FromMinutes(30)),      // default: 10 minutes

// A phase of another node of the same event, by node ID or label.
Phase.WaitFor(node: "Transport", phase: "spin-up"),
```

- **Same node:** the status method of that phase is called on every poll.
- **Other node:** the status that node stored for the same event is read. That node must keep evaluating its phase, for example with a wait of its own, otherwise its stored status doesn't change.
  A phase that never stored a status counts as `NotStarted`. The status of the other node is never written by the waiting node.
- **Timeout:** when the phase doesn't complete in time, the phase after the wait fails with "Phase 'spin-up' did not complete within '30 minutes'."
  or "Phase 'spin-up' of node 'Transport' never reported a status within '30 minutes'.". The timeout counts from the moment the wait started, also across executions.

#### Short and long waits

When the next poll is less than 10 seconds away, the script waits in-process.
When it's 10 seconds or more away, the script ends and a scheduler task resumes it at the next poll moment, so a long wait doesn't occupy an Automation thread.

- The resume task runs the regular orchestration script for the event, on the DataMiner Agent that ran the node.
  Its description is "MediaOps Live Orchestration Resume". It's removed when it ran, when the event is triggered again, and when the event or job is deleted.
- When a node script doesn't take over its pending resume, for example because the script no longer exists or fails before its phases run, the resume is removed once that script ends.
  The node then no longer waits, so the event can end. The sliding window cleanup also removes past resume tasks that no node waits for anymore.
- A resume that never ran, for example because the DataMiner Agent was down at that moment, fails the event once it's more than the past range of the sliding window (1 hour) overdue:
  "The orchestration of node 'x' was not resumed: the resume that was due at 2026-11-06 10:15:00Z did not run.". The event is reported to MediaOps.PLAN, and can be retried.
  This is done by `SyncCurrentSlidingWindow(IMediaOpsPlanHelper)`; the overload without a MediaOps.PLAN helper keeps such resumes.
- A resumed execution continues at the wait, without evaluating the earlier phases again.
  When the phases of the script changed in the meantime, it walks all phases from the first one instead, and waits start their timeout anew.
- Only the node scripts that are waiting are resumed. The global orchestration script of the event isn't run again.
- When the resume task can't be created, the phase after the wait and the event fail with "Unable to schedule the resume of the orchestration: ...".

Waiting in another process only happens for a node of an event. In other executions, such as running the script manually, the script always waits in-process.

### Event state

The event stays `Configuring` as long as one of its nodes waits for a resume.
The execution that ends the last wait sets the event to `Completed` or `Failed`, sets the orchestration duration, and reports the result to MediaOps Plan.
When a phase fails while other nodes still wait, the event fails right away and the other nodes continue their waits. An event that already failed or was cancelled keeps its state.

### Trigger an event again

The phase statuses are kept per event and per node, so an event that is triggered again continues where it stopped:
phases whose status method reports `Completed` are skipped, and failed or unfinished phases are executed again.
Any resume that was still scheduled for the node is replaced.

An event that already ran is triggered again with `RetryEvents` (see [Retry an event](#retry-an-event)).

Only one execution at a time can run the phases of a node for an event. A second one stops right away with
"The phases of node 'x' are already being executed for this event by another execution. This execution was stopped.".
This guard lives in the SLAutomation process, so it only covers executions on the same DataMiner Agent, and is released when that process stops.

### Context

Every phase and status method receives an `OrchestrationPhaseContext`:

| Property | Content |
| --- | --- |
| `Engine` | The link with the SLAutomation process. |
| `Job.Reference`, `Job.Name`, `Job.MonitoringService` | The job the orchestration runs for, read once per execution. |
| `Job.Values` | The job context values of the current node (see below). |
| `Node.Id`, `Node.Label` | The node the orchestration runs for. |
| `Event` | The event: `Id`, `Name`, `Type`, its `Nodes` (`Id`, `Label`, `OrchestrationScriptName`) and its `Connections` (`SourceNodeId`, `DestinationNodeId`). |

The phases are methods of the script, so they can also use the script itself, for example `GetParameterValue` or `InputValues`.

#### Job context values

`context.Job.Values` keeps plain text values per job and per node. A phase writes what later phases, later executions and later events of the node need,
for example the ID of the cloud service it started, so the stop event can tear it down. The values survive the end of the execution, a failure, and a restart of the system.

```csharp
context.Job.Values.Set("cloudServiceId", cloudServiceId);

string id = context.Job.Values.Get("cloudServiceId");          // throws KeyNotFoundException when missing
bool exists = context.Job.Values.TryGet("cloudServiceId", out id);
bool removed = context.Job.Values.Remove("cloudServiceId");
IReadOnlyCollection<string> keys = context.Job.Values.Keys;

// The values of another node of the job are read-only.
var transport = context.Event.Nodes.Single(node => node.Label == "Transport");
string streamUrl = context.Job.Values.ForNode(transport.Id).Get("streamUrl");
```

- A node only writes its own values; keys of different nodes never collide. The values of other nodes can be read, not changed.
- `ForNode` accepts the ID or label of a node of the current event, or the ID of a node that only takes part in other events of the job.
  Labels of nodes of other events aren't accepted, because a label can differ per event. A node that isn't part of the job fails with "node 'x' is not part of the job".
- A key is at most 128 characters, a value at most 4,096 characters, and a node holds at most 50 keys per job. Writes beyond a limit fail; keys and values are never trimmed.
- Values are read from the system on every access, because another event of the same node can change them in the meantime.
- The values are removed when the job is deleted.

> [!WARNING]
> The values are stored as plain text and are not meant for secrets. Store a reference to a secret instead of the secret itself.

### Where phases are available

| Execution | Phase statuses | Long waits | Waits on other nodes | `Job.Values` |
| --- | --- | --- | --- | --- |
| Node script of an event | Stored per event and node | Resumed by a scheduler task | Yes | Yes |
| Node script run by a global script | Stored per event and node | Wait in-process | Yes | Yes |
| Global script of an event | Only for this execution | Wait in-process | No | No |
| Run manually | Only for this execution | Wait in-process | No | No |

Using an unavailable feature fails with an explicit message, for example "The job context values are only available when the script runs for a node of an event.".

### Orchestrate nodes from a global script

A global script runs the script of a node with `OrchestrateNode` or `TryOrchestrateNode`. Both return only when the node completed or failed.

```csharp
public override void Orchestrate(IEngine engine)
{
    if (TryGetNodeConfiguration("Transport", out NodeConfiguration transport))
    {
        // Throws an OrchestrationNodeFailedException when the node fails.
        OrchestrateNode(transport);
    }

    if (TryGetNodeConfiguration("Backup", out NodeConfiguration backup))
    {
        NodeOrchestrationResult result = TryOrchestrateNode(backup);
        if (!result.IsSuccess)
        {
            engine.GenerateInformation($"Backup not available: {result.FailureReason}");
        }
    }

    OrchestrateAllConnections();
}
```

- `OrchestrateNode` throws an `OrchestrationNodeFailedException` when the node fails. Unless the global script catches it, the event fails with
  "Error during global orchestration: Node 'Transport' failed: Phase 'spin-up' failed: Encoder offline.", without a stack trace.
- `TryOrchestrateNode` doesn't throw. It returns a `NodeOrchestrationResult` with `IsSuccess` and `FailureReason`.
  A failure the global script handled this way doesn't fail the event; the failed phases of the node remain visible on the [phases of the event](#read-the-phases-of-an-event).
- `IsExecuted` is `false` when nothing was run: the node has no orchestration script, or the script doesn't run as the global script of an event.
- A node run by a global script never ends while it waits: its long waits are held in-process, so the global script only continues when the node is done.
  The node script stays busy during the wait, so its script timeout must be longer than its waits. When it isn't, the phase after the wait fails with "The script timed out while waiting on phase '...'.".
- Its phase statuses are stored per event and node like for any node script. When the event is retried, the global script runs again, and the node continues where it stopped.

> [!NOTE]
> Before this version, `OrchestrateNode` ignored the result of the node, and the event completed even when the node failed.
> Global scripts that relied on this should use `TryOrchestrateNode`.

### Read the phases of an event

Every event exposes the phases of its node scripts through the read-only `Phases` property, ordered by node ID and then by the order of the phases in the script.

```csharp
OrchestrationEvent orchestrationEvent = api.Orchestration.GetEventConfigurationsById(new List<Guid> { eventId }).Single();

foreach (OrchestrationEventPhase phase in orchestrationEvent.Phases)
{
    // For example: "node-a/spin-up: Failed (Encoder ENC-A is offline.)"
    string line = $"{phase.NodeId}/{phase.Key}: {phase.State}";
    string reason = phase.FailureReason;
}
```

| Property | Content |
| --- | --- |
| `NodeId` | The node the phase belongs to. |
| `Key`, `Order` | The key of the phase and its position in the script. |
| `State` | `NotStarted`, `InProgress`, `Completed` or `Failed`. |
| `Message` | The last message the phase or its status method reported. |
| `FailureReason` | The message when the phase failed, otherwise `null`. |
| `LastUpdated`, `Started`, `Ended` | When the status was last recorded, and when the phase was last executed and ended. |

- The phases are read once, the first time `Phases` is accessed. Read the event again to see later changes.
- Only phases of node scripts are stored. An event without node scripts with phases, or that didn't run yet, has no phases.

### Storage

Phase statuses and job context values are stored in the `(slc)orchestration` DOM module:

- **Orchestration Node Phases**: one instance per event and node, with the status, message, start and end time of every phase, and the pending resume. It is removed with the event.
- **Orchestration Node Context Value**: one instance per job, node and key. It is removed with the job.

Each node writes only its own instances, so nodes and events that run at the same time never overwrite each other.
