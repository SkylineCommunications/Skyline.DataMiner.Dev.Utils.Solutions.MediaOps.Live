namespace Skyline.DataMiner.MediaOps.Live.WebApp.Controllers
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Net;
	using System.Net.Http;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Web.Http;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Caching;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Connectivity;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Take;
	using Skyline.DataMiner.Utils.PerformanceAnalyzer;

	public class ControlSurfaceController : ApiController
	{
		private static readonly Lazy<MediaOpsLiveCache> CacheInstance = new Lazy<MediaOpsLiveCache>(CreateCache);
		private static readonly Lazy<IMediaOpsLiveApi> ApiInstance = new Lazy<IMediaOpsLiveApi>(CreateApi);
		private static readonly ConcurrentDictionary<string, SseClient> SseClients = new ConcurrentDictionary<string, SseClient>();
		private static bool _subscribed;

		private static MediaOpsLiveCache CreateCache()
		{
			var envVars = LoadEnvFile();
			var url = envVars["DMA_URL"];
			var username = envVars["DMA_USERNAME"];
			var password = envVars["DMA_PASSWORD"];

			var cache = MediaOpsLiveCache.GetOrCreate(() =>
			{
				var connection = Net.ConnectionSettings.GetConnection(url, ConnectionAttributes.NoProtoBufSerialization);
				connection.Authenticate(username, password);
				connection.Subscribe();
				return connection;
			});

			return cache;
		}

		private static IMediaOpsLiveApi CreateApi()
		{
			// Trigger cache creation first (which creates the connection), then create a separate API instance
			var _ = CacheInstance.Value;
			var envVars = LoadEnvFile();
			var connection = Net.ConnectionSettings.GetConnection(envVars["DMA_URL"], ConnectionAttributes.NoProtoBufSerialization);
			connection.Authenticate(envVars["DMA_USERNAME"], envVars["DMA_PASSWORD"]);
			connection.Subscribe();
			return new MediaOpsLiveApi(connection);
		}

		private static Dictionary<string, string> LoadEnvFile()
		{
			var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			var envPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");
			if (!File.Exists(envPath))
			{
				envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
			}

			if (!File.Exists(envPath))
			{
				throw new FileNotFoundException("Could not find .env file. Please create one with DMA_URL, DMA_USERNAME, and DMA_PASSWORD.");
			}

			foreach (var line in File.ReadAllLines(envPath))
			{
				var trimmed = line.Trim();
				if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
				var eqIdx = trimmed.IndexOf('=');
				if (eqIdx <= 0) continue;
				var key = trimmed.Substring(0, eqIdx).Trim();
				var value = trimmed.Substring(eqIdx + 1).Trim();
				result[key] = value;
			}

			return result;
		}

		private static IMediaOpsLiveApi Api => ApiInstance.Value;

		private static ConnectivityInfoProvider GetConnectivityProvider()
		{
			var provider = CacheInstance.Value.ConnectivityInfoProvider;
			if (!_subscribed)
			{
				provider.ConnectionsUpdated += OnConnectionsUpdated;
				_subscribed = true;
			}

			return provider;
		}

		private static void OnConnectionsUpdated(object sender, ConnectionsUpdatedEvent e)
		{
			var ids = e.VirtualSignalGroups.Select(v => v.VirtualSignalGroup.ID.ToString()).ToList();
			var msg = ids.Count > 0 ? string.Join(",", ids) : "all";
			foreach (var client in SseClients.Values)
			{
				client.Signal(msg);
			}
		}

		[HttpGet]
		[Route("api/controlsurface/sources")]
		public IHttpActionResult GetSources()
		{
			try
			{
				var vsgCache = CacheInstance.Value.VirtualSignalGroupEndpointsCache.VirtualSignalGroups;
				var connectivityProvider = GetConnectivityProvider();
				var sources = vsgCache.GetAllVirtualSignalGroups()
				.Where(vsg => vsg.Role == EndpointRole.Source)
				.OrderBy(vsg => vsg.Name)
				.ToList();

				var result = sources.Select(s =>
				{
					var connectivity = connectivityProvider.GetConnectivity(s);
					var connectedDests = new List<string>();
					var pendingDisconnectDests = new List<string>();
					if (connectivity?.ConnectedDestinations != null)
					{
						connectedDests = connectivity.ConnectedDestinations.Select(d => d.Name).OrderBy(n => n).ToList();
					}
					if (connectivity?.PendingConnectedDestinations != null)
					{
						pendingDisconnectDests = connectivity.PendingConnectedDestinations.Select(d => d.Name).OrderBy(n => n).ToList();
					}

					return new SourceDto { Id = s.ID, Name = s.Name, ConnectedDestinations = connectedDests, PendingDisconnectDestinations = pendingDisconnectDests };
				}).ToList();
				return Ok(result);
			}
			catch (Exception ex)
			{
				return InternalServerError(ex);
			}
		}

		[HttpGet]
		[Route("api/controlsurface/destinations")]
		public IHttpActionResult GetDestinations()
		{
			try
			{
				var connectivityProvider = GetConnectivityProvider();
				var vsgCache = CacheInstance.Value.VirtualSignalGroupEndpointsCache.VirtualSignalGroups;

				var destinations = vsgCache.GetAllVirtualSignalGroups()
				.Where(vsg => vsg.Role == EndpointRole.Destination)
				.OrderBy(vsg => vsg.Name)
				.ToList();

				var result = destinations.Select(d => BuildDestinationDto(connectivityProvider, d)).ToList();

				return Ok(result);
			}
			catch (Exception ex)
			{
				return InternalServerError(ex);
			}
		}

		[HttpGet]
		[Route("api/controlsurface/destinations/{id}")]
		public IHttpActionResult GetDestination(Guid id)
		{
			try
			{
				var connectivityProvider = GetConnectivityProvider();
				var vsgCache = CacheInstance.Value.VirtualSignalGroupEndpointsCache.VirtualSignalGroups;
				if (!vsgCache.TryGetVirtualSignalGroup(new ApiObjectReference<VirtualSignalGroup>(id), out var vsg) || vsg.Role != EndpointRole.Destination)
				{
					return NotFound();
				}

				return Ok(BuildDestinationDto(connectivityProvider, vsg));
			}
			catch (Exception ex)
			{
				return InternalServerError(ex);
			}
		}

		private static DestinationDto BuildDestinationDto(ConnectivityInfoProvider connectivityProvider, VirtualSignalGroup d)
		{
			var connectivity = connectivityProvider.GetConnectivity(d);
			string connectedSourceName = null;
			string pendingConnectedSourceName = null;

			if (connectivity?.ConnectedSources != null && connectivity.ConnectedSources.Count > 0)
			{
				connectedSourceName = string.Join(", ", connectivity.ConnectedSources.Select(s => s.Name));
			}

			if (connectivity?.PendingConnectedSources != null && connectivity.PendingConnectedSources.Count > 0)
			{
				pendingConnectedSourceName = connectivity.PendingConnectedSources.First().Name;
			}

			var levelMappings = new List<LevelMappingInfoDto>();
			var pendingDisconnects = new List<PendingDisconnectDto>();
			if (connectivity?.Levels != null && connectivity.Levels.Count > 0)
			{
				var srcEndpointInfo = new Dictionary<Guid, (string VsgName, Guid LevelId)>();
				if (connectivity.ConnectedSources != null)
				{
					foreach (var srcVsg in connectivity.ConnectedSources)
					{
						foreach (var le in srcVsg.GetLevelEndpoints())
						{
							srcEndpointInfo[le.Endpoint.ID] = (srcVsg.Name, le.Level.ID);
						}
					}
				}

				var levelsCache = CacheInstance.Value.LevelsCache;

				foreach (var kvp in connectivity.Levels.OrderBy(l => l.Key.Number))
				{
					var srcEndpoint = kvp.Value.ConnectedSource;
					if (srcEndpoint != null)
					{
						string sourceLevelName = srcEndpoint.Name;
						string sourceVsgName = null;
						if (srcEndpointInfo.TryGetValue(srcEndpoint.ID, out var info))
						{
							sourceVsgName = info.VsgName;
							if (levelsCache.TryGetLevel(new ApiObjectReference<Level>(info.LevelId), out var srcLevel))
							{
								sourceLevelName = srcLevel.Name;
							}
						}

						levelMappings.Add(new LevelMappingInfoDto
						{
							SourceVsgName = sourceVsgName,
							SourceLevel = sourceLevelName,
							DestinationLevel = kvp.Key.Name,
						});
					}

					// Check if this level's endpoint is being disconnected
					if (kvp.Value.IsDisconnecting && kvp.Value.ConnectedSource != null)
					{
						string sourceLevelName = kvp.Value.ConnectedSource.Name;
						string sourceVsgName = null;
						if (srcEndpointInfo.TryGetValue(kvp.Value.ConnectedSource.ID, out var info))
						{
							sourceVsgName = info.VsgName;
							if (levelsCache.TryGetLevel(new ApiObjectReference<Level>(info.LevelId), out var srcLevel))
							{
								sourceLevelName = srcLevel.Name;
							}
						}

						var existingPending = pendingDisconnects.FirstOrDefault(p => p.SourceVsgName == sourceVsgName);
						if (existingPending == null)
						{
							existingPending = new PendingDisconnectDto { SourceVsgName = sourceVsgName, LevelNames = new List<string>() };
							pendingDisconnects.Add(existingPending);
						}
						existingPending.LevelNames.Add(kvp.Key.Name);
					}
				}
			}

			var levCache = CacheInstance.Value.LevelsCache;
			var dstLevelNames = d.GetAssignedLevels()
				.Select(r => levCache.TryGetLevel(r, out var l) ? l : null)
				.Where(l => l != null)
				.OrderBy(l => l.Number)
				.Select(l => l.Name)
				.ToList();

			return new DestinationDto
			{
				Id = d.ID,
				Name = d.Name,
				ConnectedSourceName = connectedSourceName,
				PendingConnectedSourceName = pendingConnectedSourceName,
				ConnectedLevelMappings = levelMappings.Count > 0 ? levelMappings : null,
				PendingDisconnects = pendingDisconnects.Count > 0 ? pendingDisconnects : null,
				LevelNames = dstLevelNames,
			};
		}

		[HttpGet]
		[Route("api/controlsurface/vsgs/{id}/levels")]
		public IHttpActionResult GetVsgLevels(Guid id)
		{
			try
			{
				var vsgCache = CacheInstance.Value.VirtualSignalGroupEndpointsCache.VirtualSignalGroups;
				var levelsCache = CacheInstance.Value.LevelsCache;

				if (!vsgCache.TryGetVirtualSignalGroup(new ApiObjectReference<VirtualSignalGroup>(id), out var vsg))
				{
					return NotFound();
				}

				var result = vsg.GetAssignedLevels()
					.Select(r => levelsCache.TryGetLevel(r, out var l) ? l : null)
					.Where(l => l != null)
					.OrderBy(l => l.Number)
					.Select(l => new LevelDto { Id = l.ID, Name = l.Name, Number = l.Number })
					.ToList();

				return Ok(result);
			}
			catch (Exception ex)
			{
				return InternalServerError(ex);
			}
		}

		[HttpGet]
		[Route("api/controlsurface/events")]
		public HttpResponseMessage GetEvents(CancellationToken clientDisconnected)
		{
			// Ensure the connectivity provider subscription is active
			var _ = GetConnectivityProvider();

			var clientId = Guid.NewGuid().ToString();
			var sseClient = new SseClient();
			SseClients[clientId] = sseClient;

			var response = new HttpResponseMessage(HttpStatusCode.OK);
			response.Content = new PushStreamContent(async (stream, content, context) =>
			{
				try
				{
					var writer = new System.IO.StreamWriter(stream) { AutoFlush = true };
					await writer.WriteAsync("data: connected\n\n");

					while (!clientDisconnected.IsCancellationRequested)
						{
							// Wait for an update signal or heartbeat timeout (30 s)
							string message = await sseClient.WaitAsync(TimeSpan.FromSeconds(30), clientDisconnected);
							if (clientDisconnected.IsCancellationRequested) break;
							await writer.WriteAsync(message != null ? "data: " + message + "\n\n" : ": heartbeat\n\n");
						}
				}
				finally
				{
					SseClients.TryRemove(clientId, out SseClient removed);
					sseClient.Dispose();
					stream.Close();
				}
			}, "text/event-stream");

			response.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };
			return response;
		}

		[HttpPost]
		[Route("api/controlsurface/connect")]
		public IHttpActionResult Connect([FromBody] ConnectRequest request)
		{
			try
			{
				var api = Api;
				var vsgCache = CacheInstance.Value.VirtualSignalGroupEndpointsCache.VirtualSignalGroups;
				vsgCache.TryGetVirtualSignalGroup(new ApiObjectReference<VirtualSignalGroup>(request.SourceId), out var source);
				vsgCache.TryGetVirtualSignalGroup(new ApiObjectReference<VirtualSignalGroup>(request.DestinationId), out var destination);

				if (source == null || destination == null)
				{
					return BadRequest("Source or destination not found.");
				}

				ICollection<LevelMapping> levelMappings = null;
				if (request.LevelMappings != null && request.LevelMappings.Count > 0)
				{
					levelMappings = request.LevelMappings
						.Select(m => new LevelMapping(
							new ApiObjectReference<Level>(m.SourceLevelId),
							new ApiObjectReference<Level>(m.DestinationLevelId)))
						.ToList();
				}

				var connectionHandler = api.GetConnectionHandler();
				using (var performanceCollector = new PerformanceCollector(new NoOpPerformanceLogger()))
				using (var performanceTracker = new PerformanceTracker(performanceCollector))
				{
					connectionHandler.Take(
					new[] { new VsgConnectionRequest(source, destination, levelMappings) },
					performanceTracker,
					new TakeOptions { WaitForCompletion = false });
				}

				return Ok(new { success = true });
			}
			catch (Exception ex)
			{
				return InternalServerError(ex);
			}
		}

		[HttpPost]
		[Route("api/controlsurface/disconnect")]
		public IHttpActionResult Disconnect([FromBody] DisconnectRequest request)
		{
			try
			{
				var api = Api;
				var vsgCache = CacheInstance.Value.VirtualSignalGroupEndpointsCache.VirtualSignalGroups;
				vsgCache.TryGetVirtualSignalGroup(new ApiObjectReference<VirtualSignalGroup>(request.DestinationId), out var destination);

				if (destination == null)
				{
					return BadRequest("Destination not found.");
				}

				ICollection<ApiObjectReference<Level>> levels = null;
				if (request.LevelIds != null && request.LevelIds.Count > 0)
				{
					levels = request.LevelIds.Select(id => new ApiObjectReference<Level>(id)).ToList();
				}

				var connectionHandler = api.GetConnectionHandler();
				using (var performanceCollector = new PerformanceCollector(new NoOpPerformanceLogger()))
				using (var performanceTracker = new PerformanceTracker(performanceCollector))
				{
					connectionHandler.Disconnect(
					new[] { new VsgDisconnectRequest(destination, levels) },
					performanceTracker,
					new DisconnectOptions { WaitForCompletion = false });
				}

				return Ok(new { success = true });
			}
			catch (Exception ex)
			{
				return InternalServerError(ex);
			}
		}
	}

	public class VsgDto
	{
		public Guid Id { get; set; }

		public string Name { get; set; }
	}

	public class SourceDto
	{
		public Guid Id { get; set; }

		public string Name { get; set; }

		public List<string> ConnectedDestinations { get; set; }

		public List<string> PendingDisconnectDestinations { get; set; }
	}

	public class LevelDto
	{
		public Guid Id { get; set; }

		public string Name { get; set; }

		public long Number { get; set; }
	}

	public class DestinationDto
	{
		public Guid Id { get; set; }

		public string Name { get; set; }

		public string ConnectedSourceName { get; set; }

		public string PendingConnectedSourceName { get; set; }

		public List<LevelMappingInfoDto> ConnectedLevelMappings { get; set; }

		public List<PendingDisconnectDto> PendingDisconnects { get; set; }

		public List<string> LevelNames { get; set; }
	}

	public class PendingDisconnectDto
	{
		public string SourceVsgName { get; set; }

		public List<string> LevelNames { get; set; }
	}

	public class LevelMappingInfoDto
	{
		public string SourceVsgName { get; set; }

		public string SourceLevel { get; set; }

		public string DestinationLevel { get; set; }
	}

	public class LevelMappingDto
	{
		public Guid SourceLevelId { get; set; }

		public Guid DestinationLevelId { get; set; }
	}

	public class ConnectRequest
	{
		public Guid SourceId { get; set; }

		public Guid DestinationId { get; set; }

		public List<LevelMappingDto> LevelMappings { get; set; }
	}

	public class DisconnectRequest
	{
		public Guid DestinationId { get; set; }

		public List<Guid> LevelIds { get; set; }
	}

	internal class NoOpPerformanceLogger : Skyline.DataMiner.Utils.PerformanceAnalyzer.Loggers.IPerformanceLogger
	{
		public void Report(System.Collections.Generic.List<Skyline.DataMiner.Utils.PerformanceAnalyzer.Models.PerformanceData> data)
		{
		}
	}

	internal sealed class SseClient : IDisposable
	{
		private readonly ConcurrentQueue<string> _messages = new ConcurrentQueue<string>();
		private readonly SemaphoreSlim _signal = new SemaphoreSlim(0, 1);

		public void Signal(string message)
		{
			_messages.Enqueue(message);
			if (_signal.CurrentCount == 0)
			{
				_signal.Release();
			}
		}

		/// <summary>Returns the queued message if signalled, null if timed out (heartbeat).</summary>
		public async Task<string> WaitAsync(TimeSpan timeout, CancellationToken ct)
		{
			bool signalled = await _signal.WaitAsync(timeout, ct).ConfigureAwait(false);
			if (!signalled) return null;

			// Drain all queued messages into a combined set of IDs
			var allIds = new HashSet<string>();
			while (_messages.TryDequeue(out var msg))
			{
				if (msg == "all")
				{
					return "all";
				}

				foreach (var id in msg.Split(','))
				{
					if (!string.IsNullOrEmpty(id)) allIds.Add(id);
				}
			}

			return allIds.Count > 0 ? string.Join(",", allIds) : "all";
		}

		public void Dispose() => _signal.Dispose();
	}
}
