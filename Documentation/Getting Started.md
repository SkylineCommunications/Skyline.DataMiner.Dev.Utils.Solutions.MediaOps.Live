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
