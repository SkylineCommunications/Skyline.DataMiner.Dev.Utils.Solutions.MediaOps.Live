# Quick Reference

Common snippets for the public API in `Skyline.DataMiner.Solutions.MediaOps.Live`.

## Instantiate `MediaOpsLiveApi`

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Live.API;
using Skyline.DataMiner.Net;

IConnection connection = /* create or retrieve connection */;
var api = new MediaOpsLiveApi(connection);

// Or use the extension method:
var api = connection.GetMediaOpsLiveApi();

var api = engine.GetMediaOpsLiveApi(); // For automation scripts
var api = protocol.GetMediaOpsLiveApi(); // For protocols
var api = gqiDms.GetMediaOpsLiveApi(); // For GQI data sources
```

## Access repositories

Repositories are the primary way to interact with stored objects.

- CRUD operations (create, read, update, delete)
- Query objects using filters
- Subscribe to changes
- Batch operations

```csharp
var endpointsRepo = api.Endpoints;
var vsgRepo = api.VirtualSignalGroups;
var levelsRepo = api.Levels;
var transportTypesRepo = api.TransportTypes;
```

## Reading objects

### Basic Reading

```csharp
// By ID
var endpoint = api.Endpoints.Read(id);

// By name
var endpoint = api.Endpoints.Read("My Endpoint");

// Multiple by IDs
var endpoints = api.Endpoints.Read(new[] { id1, id2, id3 });

// Multiple by names
var endpoints = api.Endpoints.Read(new[] { "Endpoint 1", "Endpoint 2" });

// All
var allEndpoints = api.Endpoints.ReadAll();
```

### Filtered Reading

```csharp
using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;

// With filter using exposers
var filter = EndpointExposers.Name.Contains("Network");
var endpoints = api.Endpoints.Read(filter);

// Combine filters with AND
var combinedFilter = EndpointExposers.Name.Contains("Video")
    .AND(EndpointExposers.Role.Equal(EndpointRole.Source));
var videoSources = api.Endpoints.Read(combinedFilter);

// Combine filters with OR
var orFilter = EndpointExposers.Name.Contains("SDI")
    .OR(EndpointExposers.Name.Contains("IP"));
var sdiOrIpEndpoints = api.Endpoints.Read(orFilter);

// Negate filters with NOT
var notFilter = EndpointExposers.Role.NotEqual(EndpointRole.Destination);
var nonDestinations = api.Endpoints.Read(notFilter);
```

### LINQ Queries

```csharp
// Simple query
var endpoints = api.Endpoints.Query()
    .Where(e => e.Role == EndpointRole.Source)
    .OrderBy(e => e.Name)
    .Take(10)
    .ToList();

// Count matching items
var sourceCount = api.Endpoints.Query()
    .Count(e => e.Role == EndpointRole.Source);
```

### Paged Reading

For large datasets, use paged reading to process data in batches:

```csharp
// Read all endpoints in pages of 100
foreach (var page in api.Endpoints.ReadAllPaged(pageSize: 100))
{
    foreach (var endpoint in page)
    {
        // Process endpoint
        Console.WriteLine(endpoint.Name);
    }
}

// Paged reading with filter
var filter = EndpointExposers.Role.Equal(EndpointRole.Source);
foreach (var page in api.Endpoints.ReadPaged(filter, pageSize: 50))
{
    ProcessBatch(page.ToList());
}
```

### Counting

```csharp
// Count all items
var totalCount = api.Endpoints.CountAll();

// Count with filter
var filter = EndpointExposers.Role.Equal(EndpointRole.Source);
var sourceCount = api.Endpoints.Count(filter);
```

## Create and update objects

### Single Operations

```csharp
// Transport Type
var transportType = api.TransportTypes.Create(new TransportType { Name = "SDI" });

// Level
var level = api.Levels.Create(new Level 
{ 
    Name = "Video", 
    Number = 1, 
    TransportType = transportType 
});

// Endpoint
var endpoint = api.Endpoints.Create(new Endpoint 
{ 
    Name = "Camera 1", 
    Role = EndpointRole.Source,
    TransportType = transportType,
    Element = new DmsElementId(1, 100),
    Identifier = "Output_1"
});

// Update Endpoint
endpoint.Name = "Camera 1 Updated";
endpoint = api.Endpoints.Update(endpoint);

// Delete Endpoint
api.Endpoints.Delete(endpoint);
```

### Batch Operations

```csharp
// Create or update multiple items at once
var endpoints = new List<Endpoint>
{
    new Endpoint { Name = "Endpoint 1", Role = EndpointRole.Source },
    new Endpoint { Name = "Endpoint 2", Role = EndpointRole.Source },
    new Endpoint { Name = "Endpoint 3", Role = EndpointRole.Destination },
};

var created = api.Endpoints.CreateOrUpdate(endpoints).ToList();

// Delete multiple items
api.Endpoints.Delete(created);
```

### Virtual Signal Groups

```csharp
// Create VSG with endpoint assignments
var vsg = new VirtualSignalGroup
{
    Name = "Source VSG 1",
    Role = EndpointRole.Source,
};

// Assign endpoints to levels
vsg.AssignEndpointToLevel(videoLevel, videoEndpoint);
vsg.AssignEndpointToLevel(audioLevel, audioEndpoint);

vsg = api.VirtualSignalGroups.Create(vsg);

// Get assigned endpoints
var assignedEndpoints = vsg.GetAssignedEndpoints();

// Get endpoint for a specific level
var videoEp = vsg.GetEndpointForLevel(videoLevel);
```

## Connectivity execution

### Getting Connectivity Information

The GetConnectivity() method can be used to retrieve connectivity information for endpoints and VSGs.
Both source and destination objects are supported. Depending on the type of object, different information is returned.

```csharp
var connectivityInfoProvider = api.GetConnectivityInfoProvider();

// Check if two endpoints (or VSGs) are connected
bool isConnected = connectivityInfoProvider.IsConnected(source, destination);

// Get connectivity for a destination endpoint
var endpointConnectivity = connectivityInfoProvider.GetConnectivity(destinationEndpoint);
var connectedSource = endpointConnectivity.ConnectedSource;
var connectionState = endpointConnectivity.State;

// Get connectivity for a source VSG
var vsgConnectivity = connectivityInfoProvider.GetConnectivity(sourceVsg);
var connectedDestinations = vsgConnectivity.ConnectedDestinations;
```

### Making Connections

The following methods can be used to make connections between endpoints and virtual signal groups (VSGs).
Because the methods accept multiple requests at once, you can batch connections for improved performance.

```csharp
var connectionHandler = api.GetConnectionHandler();

// Connect two virtual signal groups
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

// Connect individual endpoints
connectionHandler.Take(
    new[]
    {
        new EndpointConnectionRequest(sourceEndpoint, destinationEndpoint),
    },
    performanceTracker,
    new TakeOptions
    {
        WaitForCompletion = true,
    });

// Connect multiple at once
connectionHandler.Take(
    new[]
    {
        new VsgConnectionRequest(source1, dest1),
        new VsgConnectionRequest(source2, dest2),
        new VsgConnectionRequest(source3, dest3),
    },
    performanceTracker,
    new TakeOptions
    {
        WaitForCompletion = true,
    });
```

### Disconnecting

```csharp
var connectionHandler = api.GetConnectionHandler();

// Disconnect a VSG
connectionHandler.Disconnect(
    new[]
    {
        new VsgDisconnectRequest(destinationVsg),
    },
    performanceTracker,
    new TakeOptions
    {
        WaitForCompletion = true,
    });

// Disconnect an endpoint
connectionHandler.Disconnect(
    new[]
    {
        new EndpointDisconnectRequest(destinationEndpoint),
    },
    performanceTracker,
    new TakeOptions
    {
        WaitForCompletion = true,
    });
```

### Connection Options

```csharp
var options = new TakeOptions
{
    // Wait for the operation to complete before returning
    WaitForCompletion = true,
};

connectionHandler.Take(requests, performanceTracker, options);
```

## Subscriptions

### Subscribe to Repository Changes

```csharp
// Subscribe to all endpoints
var subscription = api.Endpoints.Subscribe();

subscription.Changed += (sender, e) =>
{
    // Handle created items
    foreach (var endpoint in e.Created)
    {
        Console.WriteLine($"Created: {endpoint.Name}");
    }
    
    // Handle updated items
    foreach (var endpoint in e.Updated)
    {
        Console.WriteLine($"Updated: {endpoint.Name}");
    }
    
    // Handle deleted items
    foreach (var endpoint in e.Deleted)
    {
        Console.WriteLine($"Deleted: {endpoint.Name}");
    }
};

// Remember to dispose when done
subscription.Dispose();
```

### Subscribe with Filter

```csharp
var filter = EndpointExposers.Role.Equal(EndpointRole.Source);
var subscription = api.Endpoints.Subscribe(filter);

// Only source endpoint changes will be received
subscription.Changed += HandleSourceChanges;
```
