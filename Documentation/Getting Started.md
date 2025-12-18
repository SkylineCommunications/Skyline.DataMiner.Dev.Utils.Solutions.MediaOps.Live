# Getting Started

This documentation describes how to use the public API exposed by `Skyline.DataMiner.MediaOps.Live`.

## Installation

Add the NuGet package to your solution:

```bash
dotnet add package Skyline.DataMiner.MediaOps.Live
```

Depending on your project type, one of the following additional packages is also required:
- Automation scripts: `Skyline.DataMiner.MediaOps.Live.Automation`
- Protocols: `Skyline.DataMiner.MediaOps.Live.Protocol`
- GQI Ad-hoc Data Sources and custom operators: `Skyline.DataMiner.MediaOps.Live.GQI`

> [!NOTE]
> This library targets `.NET Framework 4.8`.

## Entry Point

The `MediaOpsLiveApi` class is the main entry point to the MediaOps.LIVE API.

It exposes:

- **Repositories** for reading/writing DOM-backed objects (Endpoints, Virtual Signal Groups, Levels, Transport Types)
- **Connectivity helpers** for querying and managing signal connections
- **Caching** for improved performance in long-running processes
- **Orchestration** for automated scheduling and execution of events

### Obtaining an API Instance

To obtain an instance of the `MediaOpsLiveApi`, use the `GetMediaOpsLiveApi` extension method.
This extension method is available for automation scripts, connectors, GQI ad-hoc data sources, and custom operators.

```csharp
using Skyline.DataMiner.MediaOps.Live.API;
using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

// Automation scripts
var api = engine.GetMediaOpsLiveApi();

// Protocols
var api = protocol.GetMediaOpsLiveApi();

// GQI ad-hoc data sources and custom operators
var api = gqiDms.GetMediaOpsLiveApi();
```

On other places the instance can also be created starting from an `IConnection` object:

```csharp
IConnection connection;
var api = connection.GetMediaOpsLiveApi();

// Or:
var api = new MediaOpsLiveApi(connection);
```

## Core Concepts

### Transport Types

Transport Types define the physical or logical transport mechanism (e.g., SDI, IP, ...).

```csharp
var sdi = api.TransportTypes.Create(new TransportType
{
    Name = "SDI",
});
```

### Levels

Levels represent signal layers within a transport type (e.g., Video, Audio 1, Audio 2, Data).

```csharp
var videoLevel = api.Levels.Create(new Level
{
    Name = "Video",
    Number = 1,
    TransportType = sdi,
});

var audio1Level = api.Levels.Create(new Level
{
    Name = "Audio 1",
    Number = 2,
    TransportType = sdi,
});
```

### Endpoints

Endpoints represent physical or virtual connection points on devices. Each endpoint has a role (Source or Destination) and is linked to a DataMiner element.

```csharp
var videoSource = api.Endpoints.Create(new Endpoint
{
    Name = "Video Source 1",
    Role = EndpointRole.Source,
    TransportType = sdi,
    Element = new DmsElementId(1, 100), // DataMiner Agent ID / Element ID
    Identifier = "Output_1", // Unique identifier within the element
});
```

### Virtual Signal Groups (VSGs)

Virtual Signal Groups aggregate multiple endpoints across different levels into a logical signal bundle.

```csharp
var vsg = new VirtualSignalGroup
{
    Name = "Source VSG 1",
    Role = EndpointRole.Source,
};

// Assign endpoints to levels
vsg.AssignEndpointToLevel(videoLevel, videoSource);
vsg.AssignEndpointToLevel(audio1Level, audio1Source);

vsg = api.VirtualSignalGroups.Create(vsg);
```

## Basic Usage

Once you have an instance of the `MediaOpsLiveApi` class, you can start using its features.

### Creating a Complete Signal Chain

```csharp
// Create the SDI transport type
var sdi = api.TransportTypes.Create(new TransportType
{
    Name = "SDI",
});

// Create levels for video and audio
var videoLevel = api.Levels.Create(new Level
{
    Name = "Video",
    Number = 1,
    TransportType = sdi,
});

var audioLevel = api.Levels.Create(new Level
{
    Name = "Audio",
    Number = 2,
    TransportType = sdi,
});

// Create source endpoints
var videoSource = api.Endpoints.Create(new Endpoint
{
    Name = "Camera 1 - Video",
    Role = EndpointRole.Source,
    TransportType = sdi,
    Element = new DmsElementId(1, 100),
    Identifier = "Video_Out",
});

var audioSource = api.Endpoints.Create(new Endpoint
{
    Name = "Camera 1 - Audio",
    Role = EndpointRole.Source,
    TransportType = sdi,
    Element = new DmsElementId(1, 100),
    Identifier = "Audio_Out",
});

// Create a virtual signal group and assign endpoints
var sourceVsg = new VirtualSignalGroup
{
    Name = "Camera 1",
    Role = EndpointRole.Source,
};
sourceVsg.AssignEndpointToLevel(videoLevel, videoSource);
sourceVsg.AssignEndpointToLevel(audioLevel, audioSource);
sourceVsg = api.VirtualSignalGroups.Create(sourceVsg);
```

### Reading Objects

```csharp
// Read all existing virtual signal groups
var virtualSignalGroups = api.VirtualSignalGroups.ReadAll().ToList();

// Read by ID
var vsg = api.VirtualSignalGroups.Read(vsgId);

// Read by name
var vsg = api.VirtualSignalGroups.Read("Camera 1");

// Read with filter
var sources = api.VirtualSignalGroups
    .Query()
    .Where(v => v.Role == EndpointRole.Source)
    .ToList();
```

### Making Connections

```csharp
var connectionHandler = api.GetConnectionHandler();

// Connect source VSG to destination VSG
connectionHandler.Take(
    new[]
    {
        new VsgConnectionRequest(sourceVsg, destinationVsg),
    },
    performanceTracker,
    new TakeOptions
    {
        WaitForCompletion = true,
    });
```

### Checking Connectivity

```csharp
var connectivityProvider = api.GetConnectivityInfoProvider();

// Check if connected
bool isConnected = connectivityProvider.IsConnected(sourceVsg, destinationVsg);

// Get detailed connectivity info
var connectivity = connectivityProvider.GetConnectivity(destinationVsg);
var connectedSource = connectivity.ConnectedSource;
```

## Next steps

- **[Quick Reference](Quick%20Reference.md)** - Common snippets for repositories, querying, and connectivity
- **[Advanced Topics](Advanced%20Topics.md)** – Caching, subscriptions, validation, and more
- **[Tutorials](https://github.com/SkylineCommunications/SLC-AS-MediaOps.LIVE/blob/main/Documentation/Tutorials/Tutorials.md)** - Step-by-step guides to build common solutions
- **[Orchestration](Orchestration.md)** – Orchestration concepts and usage
