namespace Skyline.DataMiner.MediaOps.Live.API.Caching
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Tools;

	public class EndpointsCache
	{
		private readonly object _lock = new();

		private readonly Dictionary<ApiObjectReference<Endpoint>, Endpoint> _endpoints = new();
		private readonly Dictionary<string, Endpoint> _endpointsByName = new();
		private readonly ManyToManyMapping<(string, string), Endpoint> _endpointsByTransportMetaData = new();
		private readonly OneToManyMapping<ApiObjectReference<TransportType>, Endpoint> _endpointsByTransportType = new();
		private readonly OneToManyMapping<DmsElementId, Endpoint> _endpointsByElement = new();
		private readonly OneToManyMapping<string, Endpoint> _endpointsByIdentifier = new();

		public EndpointsCache()
		{
		}

		public EndpointsCache(IEnumerable<Endpoint> endpoints)
		{
			if (endpoints != null)
			{
				UpdateEndpoints(endpoints, []);
			}
		}

		public IReadOnlyDictionary<ApiObjectReference<Endpoint>, Endpoint> Endpoints => _endpoints;

		public IReadOnlyDictionary<string, Endpoint> EndpointsByName => _endpointsByName;

		public Endpoint GetEndpoint(ApiObjectReference<Endpoint> id)
		{
			lock (_lock)
			{
				if (!TryGetEndpoint(id, out var endpoint))
				{
					throw new ArgumentException($"Couldn't find endpoint with ID {id.ID}", nameof(id));
				}

				return endpoint;
			}
		}

		public Endpoint GetEndpoint(string name)
		{
			lock (_lock)
			{
				if (!TryGetEndpoint(name, out var endpoint))
				{
					throw new ArgumentException($"Couldn't find endpoint with name '{name}'", nameof(name));
				}

				return endpoint;
			}
		}

		public bool TryGetEndpoint(ApiObjectReference<Endpoint> id, out Endpoint endpoint)
		{
			lock (_lock)
			{
				return _endpoints.TryGetValue(id, out endpoint);
			}
		}

		public bool TryGetEndpoint(string name, out Endpoint endpoint)
		{
			lock (_lock)
			{
				return _endpointsByName.TryGetValue(name, out endpoint);
			}
		}

		public IReadOnlyCollection<Endpoint> GetEndpointsWithTransportMetadata(string fieldName, string value)
		{
			lock (_lock)
			{
				if (String.IsNullOrWhiteSpace(fieldName))
				{
					throw new ArgumentException($"'{nameof(fieldName)}' cannot be null or whitespace.", nameof(fieldName));
				}

				if (_endpointsByTransportMetaData.TryGetForward((fieldName, value), out var endpoints))
				{
					return endpoints.ToList();
				}

				return [];
			}
		}

		public IReadOnlyCollection<Endpoint> GetEndpointsWithTransportMetadata(params (string fieldName, string value)[] metadataFilters)
		{
			if (metadataFilters is null)
			{
				throw new ArgumentNullException(nameof(metadataFilters));
			}

			if (metadataFilters.Length == 0)
			{
				return [];
			}

			lock (_lock)
			{
				var result = new HashSet<Endpoint>();

				for (int i = 0; i < metadataFilters.Length; i++)
				{
					var matchingEndpoints = GetEndpointsWithTransportMetadata(metadataFilters[i].fieldName, metadataFilters[i].value);

					if (i == 0)
					{
						result.UnionWith(matchingEndpoints);
					}
					else
					{
						result.IntersectWith(matchingEndpoints);
					}

					if (result.Count == 0)
					{
						break;
					}
				}

				return result;
			}
		}

		public IReadOnlyCollection<Endpoint> GetEndpointsWithTransportType(ApiObjectReference<TransportType> transportType)
		{
			lock (_lock)
			{
				if (_endpointsByTransportType.TryGetChildren(transportType, out var endpoints))
				{
					return endpoints.ToList();
				}

				return [];
			}
		}

		public IReadOnlyCollection<Endpoint> GetEndpointsWithElement(DmsElementId elementId)
		{
			lock (_lock)
			{
				if (_endpointsByElement.TryGetChildren(elementId, out var endpoints))
				{
					return endpoints.ToList();
				}

				return [];
			}
		}

		public IReadOnlyCollection<Endpoint> GetEndpointsWithIdentifier(string identifier)
		{
			lock (_lock)
			{
				if (_endpointsByIdentifier.TryGetChildren(identifier, out var endpoints))
				{
					return endpoints.ToList();
				}

				return [];
			}
		}

		public IReadOnlyCollection<Endpoint> GetEndpointsWithElementAndIdentifier(DmsElementId elementId, string identifier)
		{
			lock (_lock)
			{
				return GetEndpointsWithElement(elementId)
					.Intersect(GetEndpointsWithIdentifier(identifier))
					.ToList();
			}
		}

		public void LoadInitialData(MediaOpsLiveApi api)
		{
			if (api is null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			var endpoints = api.Endpoints.ReadAll();

			lock (_lock)
			{
				UpdateEndpoints(endpoints, []);
			}
		}

		public void UpdateEndpoints(IEnumerable<Endpoint> updated, IEnumerable<Endpoint> deleted)
		{
			if (updated is null)
			{
				throw new ArgumentNullException(nameof(updated));
			}

			if (deleted is null)
			{
				throw new ArgumentNullException(nameof(deleted));
			}

			lock (_lock)
			{
				foreach (var item in updated)
				{
					// Remove old mappings if they exist
					if (_endpoints.TryGetValue(item.ID, out var existing))
					{
						_endpointsByName.Remove(existing.Name);
						_endpointsByTransportMetaData.TryRemoveReverse(existing);
						_endpointsByTransportType.RemoveChild(existing);
						_endpointsByElement.RemoveChild(existing);
						_endpointsByIdentifier.RemoveChild(existing);
					}

					_endpoints[item.ID] = item;
					_endpointsByName[item.Name] = item;
					_endpointsByTransportType.Add(item.TransportType, item);

					if (item.Element.HasValue)
					{
						_endpointsByElement.Add(item.Element.Value, item);
					}

					if (!String.IsNullOrEmpty(item.Identifier))
					{
						_endpointsByIdentifier.Add(item.Identifier, item);
					}

					foreach (var metadata in item.TransportMetadata)
					{
						_endpointsByTransportMetaData.TryAdd((metadata.FieldName, metadata.Value), item);
					}
				}

				foreach (var item in deleted)
				{
					_endpoints.Remove(item.ID);
					_endpointsByName.Remove(item.Name);
					_endpointsByTransportMetaData.TryRemoveReverse(item);
					_endpointsByTransportType.RemoveChild(item);
					_endpointsByElement.RemoveChild(item);
					_endpointsByIdentifier.RemoveChild(item);
				}
			}
		}
	}
}