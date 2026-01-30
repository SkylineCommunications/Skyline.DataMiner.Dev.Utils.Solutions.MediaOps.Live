namespace Skyline.DataMiner.Solutions.MediaOps.Live.Mediation.Element
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Solutions.MediaOps.Live;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Tools;

	public sealed class MediationElements
	{
		private readonly MediaOpsLiveApi _api;
		private readonly ExpiringCache<IReadOnlyCollection<MediationElement>> _elementCache = new(TimeSpan.FromMinutes(10));

		internal MediationElements(MediaOpsLiveApi api)
		{
			_api = api ?? throw new ArgumentNullException(nameof(api));
		}

		public IReadOnlyCollection<MediationElement> GetAllElements()
		{
			var elements = LoadMediationElements();

			// Update the cache with the loaded elements
			_elementCache.SetValue(elements);

			return elements;
		}

		public IReadOnlyCollection<MediationElement> GetAllElementsCached()
		{
			return _elementCache.GetOrRefresh(LoadMediationElements);
		}

		public IDictionary<Endpoint, MediationElement> GetElementsForEndpoints(IEnumerable<Endpoint> endpoints)
		{
			if (endpoints is null)
			{
				throw new ArgumentNullException(nameof(endpoints));
			}

			var dms = _api.Connection.GetDms();

			var endpointToElement = endpoints
				.Where(e => e.Element.HasValue)
				.GroupBy(e => e.Element.Value)
				.SelectMany(group =>
				{
					var element = dms.GetElement(group.Key);
					return group.Select(endpoint => new { endpoint, element });
				})
				.ToDictionary(x => x.endpoint, x => x.element);

			var allMediationElements = GetAllElementsCached()
				.ToDictionary(e => e.DmsElement.Host.Id);

			var result = new Dictionary<Endpoint, MediationElement>();

			foreach (var hostGroup in endpointToElement.GroupBy(kvp => kvp.Value.Host.Id))
			{
				if (!allMediationElements.TryGetValue(hostGroup.Key, out var mediationElement))
				{
					throw new InvalidOperationException($"Couldn't find MediaOps mediation element on hosting agent {hostGroup.Key}");
				}

				foreach (var kvp in hostGroup)
				{
					result[kvp.Key] = mediationElement;
				}
			}

			return result;
		}

		public MediationElement GetElementForEndpoint(Endpoint endpoint)
		{
			if (endpoint is null)
			{
				throw new ArgumentNullException(nameof(endpoint));
			}

			var mediationElements = GetElementsForEndpoints([endpoint]);

			if (mediationElements.Count != 1)
			{
				throw new InvalidOperationException($"Expected exactly one mediation element for endpoint '{endpoint.Name}', but found {mediationElements.Count}.");
			}

			return mediationElements[endpoint];
		}

		private IReadOnlyCollection<MediationElement> LoadMediationElements()
		{
			var elements = new List<MediationElement>();
			var dms = _api.Connection.GetDms();

			var request = new GetLiteElementInfo
			{
				ProtocolName = Constants.MediationProtocolName,
			};

			var responses = _api.Connection.HandleMessage(request);

			foreach (var liteElementInfo in responses.OfType<LiteElementInfoEvent>())
			{
				var elementId = new DmsElementId(liteElementInfo.DataMinerID, liteElementInfo.ElementID);
				var dmsElement = dms.GetElement(elementId);

				var element = new MediationElement(_api, dmsElement);
				elements.Add(element);
			}

			return elements;
		}
	}
}
