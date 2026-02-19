# Advanced Topics

This document covers advanced features of the `Skyline.DataMiner.Solutions.MediaOps.Live` API including caching, subscriptions, validation, logging, and error handling.

## Caching

The `MediaOpsLiveCache` provides a singleton cache that maintains subscriptions and cached data for improved performance in long-running processes.
The cache is automatically kept up-to-date in the background through subscriptions to relevant objects.

### Getting the Cache

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Live.API;
using Skyline.DataMiner.Solutions.MediaOps.Live.API.Caching;

var api = engine.GetMediaOpsLiveApi();

// Get or create the cache singleton
var cache = api.GetCache();
```

### Available Caches

The `MediaOpsLiveCache` provides several specialized caches:

```csharp
// Virtual Signal Groups and Endpoints cache
var vsgCache = cache.VirtualSignalGroupEndpointsCache;

// Levels cache
var levelsCache = cache.LevelsCache;

// Transport Types cache
var transportTypesCache = cache.TransportTypesCache;
```

### Connectivity Info Providers

The cache also provides pre-configured connectivity info providers that use cached data:

```csharp
// Lite connectivity provider (uses cached data)
var liteProvider = cache.LiteConnectivityInfoProvider;

// Full connectivity provider (uses cached data with subscriptions)
var connectivityProvider = cache.ConnectivityInfoProvider;

// Connection monitor for tracking connection changes
var monitor = cache.ConnectionMonitor;
```

## Subscriptions

Subscriptions allow you to receive real-time notifications when objects are created, updated, or deleted.

> [!IMPORTANT]
> Always dispose subscriptions when they are no longer needed to prevent memory leaks.

### Repository Subscriptions

Subscribe to changes in a repository:

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Live.API.Subscriptions;

var api = engine.GetMediaOpsLiveApi();

// Subscribe to all endpoints
var subscription = api.Endpoints.Subscribe();

// Handle changes
subscription.Changed += (sender, e) =>
{
    foreach (var created in e.Created)
    {
        Console.WriteLine($"Created: {created.Name}");
    }
    
    foreach (var updated in e.Updated)
    {
        Console.WriteLine($"Updated: {updated.Name}");
    }
    
    foreach (var deleted in e.Deleted)
    {
        Console.WriteLine($"Deleted: {deleted.Name}");
    }
};

// Don't forget to dispose when done
subscription.Dispose();
```

### Filtered Subscriptions

Subscribe to a subset of objects using filters:

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;

// Subscribe only to source endpoints
var filter = EndpointExposers.Role.Equal(EndpointRole.Source);
var subscription = api.Endpoints.Subscribe(filter);

subscription.Changed += (sender, e) =>
{
    // Only source endpoint changes will trigger this
};
```

## Logging

The API supports custom logging through the `ILogger` interface.

### Setting a Logger

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Live.Logging;

var api = engine.GetMediaOpsLiveApi();

// Set a custom logger
api.SetLogger(myLogger);
```

### Implementing a Custom Logger

Create your own logger by implementing `ILogger`:

```csharp
public class MyCustomLogger : ILogger
{
    public void Log(LogType logType, string message)
    {
        switch (logType)
        {
            case LogType.Debug:
                Debug.WriteLine($"[DEBUG] {message}");
                break;
            case LogType.Info:
                Console.WriteLine($"[INFO] {message}");
                break;
            case LogType.Warning:
                Console.WriteLine($"[WARNING] {message}");
                break;
            case LogType.Error:
                Console.Error.WriteLine($"[ERROR] {message}");
                break;
        }
    }
}
```

## Installation and Setup

### Checking Installation Status

Verify that the MediaOps.LIVE DOM modules are installed:

```csharp
var api = engine.GetMediaOpsLiveApi();

if (!api.IsInstalled())
{
    // DOM modules are not installed
    api.InstallDomModules(message => engine.GenerateInformation(message));
}
```

### Getting API Version

Retrieve the current API version:

```csharp
var api = engine.GetMediaOpsLiveApi();
var version = api.GetVersion();
Console.WriteLine($"MediaOps.LIVE API Version: {version}");
```

## Next Steps

- **[Quick Reference](Quick%20Reference.md)** - Common snippets for repositories, querying, and connectivity
- **[Getting Started](Getting%20Started.md)** - Installation and basic usage
- **[Orchestration](Orchestration.md)** - Orchestration concepts and usage
