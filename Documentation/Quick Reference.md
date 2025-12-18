# Quick Reference

Common snippets for the public API in `Skyline.DataMiner.MediaOps.Live`.

## Instantiate `MediaOpsLiveApi`

```csharp
using Skyline.DataMiner.MediaOps.Live.API;
using Skyline.DataMiner.Net;

IConnection connection = /* create or retrieve connection */;
var api = new MediaOpsLiveApi(connection);

// Or use the extension method:
var api = connection.GetMediaOpsLiveApi();

var api = engine.GetMediaOpsLiveApi(); // For automation scripts
// ...

```

## Access repositories

Repositories are the primary way to interact with stored objects.

	- CRUD operations (create, read, update, delete)
	- Query objects using filters
	- ...

```csharp
var endpointsRepo = api.Endpoints;
var vsgRepo = api.VirtualSignalGroups;
var levelsRepo = api.Levels;
var transportTypesRepo = api.TransportTypes;

```

## Reading objects

```csharp

// By ID
var endpoint = api.Endpoints.Read(id);

// By name
var endpoint = api.Endpoints.Read("My Endpoint");

// Multiple by IDs
var endpoints = api.Endpoints.Read(new[] { id1, id2, id3 });

// All
var allEndpoints = api.Endpoints.ReadAll();

// With filter
var filter = EndpointExposers.Name.Contains("Network");
var endpoints = api.Endpoints.Read(filter);

// LINQ query
var endpoints = api.Endpoints.Query()
	.Where(e => e.Role == EndpointRole.Source)
	.OrderBy(e => e.Name)
	.Take(10)
	.ToList();

```

## Create and update objects

```csharp

// Transport Type
var transportType = api.TransportTypes.Create(new TransportType { Name = "SDI" });

// Level
var level = api.Levels.Create(new Level { Name = "Video", Number = 1, TransportType = transportType });

// Endpoint
var endpoint = api.Endpoints.Create(new Endpoint { Name = "Camera 1", Level = level });

// Update Endpoint
endpoint.Name = "Camera 1 Updated";
endpoint = api.Endpoints.Update(endpoint);

// Delete Endpoint
api.Endpoints.Delete(endpoint);

// ...

```

## Connectivity execution

Besides managing the model, the API can help with connectivity operations:

Retrieve connectivity information between endpoints or virtual signal groups.
Different information is available in the returned objects, depending on whether the endpoint/virtual signal group is a source or a destination.

```csharp
var connectivityInfoProvider = api.GetConnectivityInfoProvider();

// Get if two endpoints (or virtual signal groups) are connected
// A virtual signal group is considered connected if at least one of its endpoints is connected
bool isConnected = connectivityInfoProvider.IsConnected(source, destination);

// Get connectivity information for a specific (source or destination) endpoint
var endpointConnectivity = connectivityInfoProvider.GetConnectivity(destination);
var connectedSource = endpointConnectivity.ConnectedSource;

// Get connectivity information for a specific (source or destination) virtual signal group
var vsgConnectivity = connectivityInfoProvider.GetConnectivity(source);
var connectedDestinations = vsgConnectivity.ConnectedDestinations;

// ...

```

Connect or disconnect endpoints or virtual signal groups.

```csharp

var connectionHandler = api.GetConnectionHandler();

// Connect two virtual signal groups.
// Same method can be used to connect two individual endpoints by using EndpointConnectionRequest instead of VsgConnectionRequest.
connectionHandler.Take(
	new[]
	{
		new VsgConnectionRequest(sourceVsg, destinationVsg),
	},
	performanceTracker,
	new TakeOptions
	{
		WaitForCompletion = true, // Wait for the take to complete
	});

// Disconnect one or more virtual signal groups.
// Same method can be used to disconnect individual endpoints by using EndpointDisconnectRequest instead of VsgDisconnectRequest.
connectionHandler.Disconnect(
	new[]
	{
		new VsgDisconnectRequest(destinationVsg),
	},
	performanceTracker,
	new TakeOptions
	{
		WaitForCompletion = true, // Wait for the take to complete
	});

// ...

```