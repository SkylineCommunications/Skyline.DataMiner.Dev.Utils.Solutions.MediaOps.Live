# Getting Started

This documentation describes how to use the public API exposed by `Skyline.DataMiner.MediaOps.Live`.

## Installation

Add the NuGet package to your solution:

```bash
dotnet add package Skyline.DataMiner.MediaOps.Live
```

Depending on your project type, one of the following additional packages is also required:
- Automation scripts: `Skyline.DataMiner.MediaOps.Live.Automation`
- Protocols: `Skyline.DataMiner.MediaOps.Live.Protocols`
- GQI Ad-hoc Data Sources and custom operators: `Skyline.DataMiner.MediaOps.Live.GQI`

> [!NOTE]
> This library targets `.NET Framework 4.8`.

## Entry Point

The `MediaOpsLiveApi` class is the main entry point to the MediaOps.LIVE API.

It exposes:

- Repositories for reading/writing DOM-backed objects
- Helpers and providers for connectivity information
- An orchestration helper for orchestration-related features

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

## Usage

Once you have an instance of the `MediaOpsLiveApi`, you can start using its features.

For example, use the repositories to create and read MediaOps.LIVE objects:

```csharp

// Create the SDI transport type
var sdi = api.TransportTypes.Create(new TransportType
{
	Name = "SDI",
});

// Create the video level using the SDI transport type
var videoLevel = api.Levels.Create(new Level
{
	Name = "Video",
	Number = 1,
	TransportType = sdi,
});

// Create an endpoint using the video level
var videoSource = api.Endpoints.Create(new Endpoint
{
	Name = "Video Source 1",
	Role = EndpointRole.Source,
	TransportType = sdi,
	Element = new DmsElementId(1, 100),
	Identifier = "Rowkey 1",
});

// Create a virtual signal group and assign the endpoint to the video level
var vsg = new VirtualSignalGroup
{
	Name = "Source VSG 1",
	Role = EndpointRole.Source,
};
vsg.AssignEndpointToLevel(videoLevel, videoSource);

vsg = api.VirtualSignalGroups.Create(vsg);

// Read all existing virtual signal groups
var virtualSignalGroups = api.VirtualSignalGroups.ReadAll().ToList();

```

## Next steps

- **[Examples](Examples.md)** - common usage patterns
- **[Advanced Topics](Advanced%20Topics.md)** – caching, subscriptions, validation, and more
- **[Tutorials](https://github.com/SkylineCommunications/SLC-AS-MediaOps.LIVE/blob/main/Documentation/Tutorials/Tutorials.md)** - Step-by-step guides to build common solutions
- **[Orchestration](Orchestration.md)** – orchestration concepts and usage
