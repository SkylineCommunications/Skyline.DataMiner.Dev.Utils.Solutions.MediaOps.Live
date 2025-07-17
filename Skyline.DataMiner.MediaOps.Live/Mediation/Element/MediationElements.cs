namespace Skyline.DataMiner.MediaOps.Live.Mediation.Element
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Data;
	using Skyline.DataMiner.Net.Messages;

	internal sealed class MediationElements
	{
		private readonly MediaOpsLiveApi _api;
		private readonly Lazy<IReadOnlyCollection<MediationElement>> _lazyMediationElements;

		internal MediationElements(MediaOpsLiveApi api)
		{
			_api = api ?? throw new ArgumentNullException(nameof(api));

			_lazyMediationElements = new Lazy<IReadOnlyCollection<MediationElement>>(LoadMediationElements);
		}

		public IReadOnlyCollection<MediationElement> AllElements => _lazyMediationElements.Value;

		public IDictionary<EndpointInfo, MediationElement> GetMediationElements(IEnumerable<EndpointInfo> endpoints)
		{
			if (endpoints is null)
			{
				throw new ArgumentNullException(nameof(endpoints));
			}

			var dms = _api.Connection.GetDms();

			var endpointToElement = endpoints
				.GroupBy(e => e.Element)
				.SelectMany(group =>
				{
					var element = dms.GetElementReference(new DmsElementId(group.Key));
					return group.Select(endpoint => new { endpoint, element });
				})
				.ToDictionary(x => x.endpoint, x => x.element);

			var allMediationElements = AllElements.ToDictionary(e => e.DmsElement.Host.Id);

			var result = new Dictionary<EndpointInfo, MediationElement>();

			foreach (var group in endpointToElement.GroupBy(kvp => kvp.Value.Host.Id))
			{
				if (!allMediationElements.TryGetValue(group.Key, out var mediationElement))
				{
					throw new InvalidOperationException($"Couldn't find MediaOps mediation element on hosting agent {group.Key}");
				}

				foreach (var kvp in group)
				{
					result[kvp.Key] = mediationElement;
				}
			}

			return result;
		}

		public IDictionary<Endpoint, MediationElement> GetMediationElements(IEnumerable<Endpoint> endpoints)
		{
			if (endpoints is null)
			{
				throw new ArgumentNullException(nameof(endpoints));
			}

			var endpointInfoMap = endpoints.ToDictionary(x => new EndpointInfo(x));
			var mediationElementMap = GetMediationElements(endpointInfoMap.Keys);

			return endpointInfoMap.ToDictionary(
				kvp => kvp.Value,
				kvp => mediationElementMap[kvp.Key]);
		}

		public MediationElement GetMediationElement(EndpointInfo endpoint)
		{
			if (endpoint is null)
			{
				throw new ArgumentNullException(nameof(endpoint));
			}

			var mediationElements = GetMediationElements([endpoint]);

			if (mediationElements.Count != 1)
			{
				throw new InvalidOperationException($"Expected exactly one mediation element for endpoint '{endpoint.Name}', but found {mediationElements.Count}.");
			}

			return mediationElements[endpoint];
		}

		public MediationElement GetMediationElement(Endpoint endpoint)
		{
			if (endpoint is null)
			{
				throw new ArgumentNullException(nameof(endpoint));
			}

			return GetMediationElement(new EndpointInfo(endpoint));
		}

		private List<MediationElement> LoadMediationElements()
		{
			var elements = new List<MediationElement>();

			var request = new GetLiteElementInfo
			{
				ProtocolName = Constants.MediationProtocolName,
			};

			var responses = _api.Connection.HandleMessage(request);

			var dms = _api.Connection.GetDms();

			foreach (var liteElementInfo in responses.OfType<LiteElementInfoEvent>())
			{
				var elementId = new DmsElementId(liteElementInfo.DataMinerID, liteElementInfo.ElementID);
				var dmsElement = dms.GetElementReference(elementId);

				var element = new MediationElement(_api, dmsElement);
				elements.Add(element);
			}

			return elements;
		}
	}
}
