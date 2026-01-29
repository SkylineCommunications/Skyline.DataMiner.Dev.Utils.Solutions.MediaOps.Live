namespace Skyline.DataMiner.MediaOps.Live.API.Caching
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;

	public class TransportTypesCache
	{
		private readonly object _lock = new();

		private readonly Dictionary<ApiObjectReference<TransportType>, TransportType> _transportTypes = new();
		private readonly Dictionary<string, TransportType> _transportTypesByName = new();

		public TransportTypesCache()
		{
		}

		public TransportTypesCache(IEnumerable<TransportType> transportTypes)
		{
			if (transportTypes != null)
			{
				UpdateTransportTypes(transportTypes, []);
			}
		}

		public IReadOnlyDictionary<ApiObjectReference<TransportType>, TransportType> TransportTypes => _transportTypes;

		public IReadOnlyDictionary<string, TransportType> TransportTypesByName => _transportTypesByName;

		public IReadOnlyCollection<TransportType> GetAllTransportTypes()
		{
			lock (_lock)
			{
				return _transportTypes.Values.ToList();
			}
		}

		public TransportType GetTransportType(ApiObjectReference<TransportType> id)
		{
			lock (_lock)
			{
				if (!TryGetTransportType(id, out var transportType))
				{
					throw new ArgumentException($"Couldn't find transport type with ID {id.ID}", nameof(id));
				}

				return transportType;
			}
		}

		public TransportType GetTransportType(string name)
		{
			lock (_lock)
			{
				if (!TryGetTransportType(name, out var transportType))
				{
					throw new ArgumentException($"Couldn't find transport type with name '{name}'", nameof(name));
				}

				return transportType;
			}
		}

		public bool TryGetTransportType(ApiObjectReference<TransportType> id, out TransportType transportType)
		{
			lock (_lock)
			{
				return _transportTypes.TryGetValue(id, out transportType);
			}
		}

		public bool TryGetTransportType(string name, out TransportType transportType)
		{
			lock (_lock)
			{
				return _transportTypesByName.TryGetValue(name, out transportType);
			}
		}

		public void LoadInitialData(IMediaOpsLiveApi api)
		{
			if (api is null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			var transportTypes = api.TransportTypes.ReadAll();

			lock (_lock)
			{
				UpdateTransportTypes(transportTypes, []);
			}
		}

		public void UpdateTransportTypes(IEnumerable<TransportType> updated, IEnumerable<TransportType> deleted)
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
					// Remove old name if it exists
					if (_transportTypes.TryGetValue(item.ID, out var existing))
					{
						_transportTypesByName.Remove(existing.Name);
					}

					_transportTypes[item.ID] = item;
					_transportTypesByName[item.Name] = item;
				}

				foreach (var item in deleted)
				{
					_transportTypes.Remove(item.ID);
					_transportTypesByName.Remove(item.Name);
				}
			}
		}
	}
}
