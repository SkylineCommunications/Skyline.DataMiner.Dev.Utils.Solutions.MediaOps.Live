namespace Skyline.DataMiner.MediaOps.Live.Mediation
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Connectivity;
	using Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement;
	using Skyline.DataMiner.MediaOps.Live.Mediation.Data;
	using Skyline.DataMiner.Net.Messages;

	public class MediationElement
	{
		internal MediationElement(IDmsElement dmsElement)
		{
			DmsElement = dmsElement ?? throw new ArgumentNullException(nameof(dmsElement));
		}

		public IDmsElement DmsElement { get; }

		public DmsElementId Id => DmsElement.DmsElementId;

		public int DmaId => DmsElement.AgentId;

		public int ElementId => DmsElement.Id;

		public string Name => DmsElement.Name;

		public IEnumerable<PendingConnectionAction> GetPendingConnectionActions()
		{
			if (DmsElement.State != Core.DataMinerSystem.Common.ElementState.Active)
			{
				yield break;
			}

			var table = DmsElement.GetTable(3000).GetData();

			foreach (var row in table.Values)
			{
				yield return new PendingConnectionAction(row);
			}
		}

		public string GetConnectionHandlerScriptName(IDmsElement destinationElement)
		{
			if (destinationElement is null)
			{
				throw new ArgumentNullException(nameof(destinationElement));
			}

			var scriptColumn = DmsElement.GetTable(1000).GetColumn<string>(1003);

			var script = scriptColumn.GetValue(destinationElement.DmsElementId.Value, KeyType.PrimaryKey);

			if (String.IsNullOrEmpty(script))
			{
				throw new InvalidOperationException($"No connection handler script found for element '{destinationElement.Name}' in mediation element '{Name}'.");
			}

			return script;
		}

		public static IEnumerable<MediationElement> GetAllMediationElements(IDms dms)
		{
			if (dms is null)
			{
				throw new ArgumentNullException(nameof(dms));
			}

			var request = new GetLiteElementInfo
			{
				ProtocolName = Constants.MediationProtocolName,
			};

			var responses = dms.Communication.SendMessage(request);

			foreach (var liteElementInfo in responses.OfType<LiteElementInfoEvent>())
			{
				var elementId = new DmsElementId(liteElementInfo.DataMinerID, liteElementInfo.ElementID);
				var dmsElement = dms.GetElementReference(elementId);

				yield return new MediationElement(dmsElement);
			}
		}

		public static IDictionary<EndpointInfo, MediationElement> GetMediationElements(MediaOpsLiveApi api, IEnumerable<EndpointInfo> endpoints)
		{
			if (api is null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			if (endpoints is null)
			{
				throw new ArgumentNullException(nameof(endpoints));
			}

			var dms = api.Connection.GetDms();

			var endpointToElement = endpoints
				.GroupBy(e => e.Element)
				.SelectMany(group =>
				{
					var element = dms.GetElementReference(new DmsElementId(group.Key));
					return group.Select(endpoint => new { endpoint, element });
				})
				.ToDictionary(x => x.endpoint, x => x.element);

			var allMediationElements = GetAllMediationElements(dms)
				.ToDictionary(e => e.DmsElement.Host.Id);

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

		public static IDictionary<Endpoint, MediationElement> GetMediationElements(MediaOpsLiveApi api, IEnumerable<Endpoint> endpoints)
		{
			if (api is null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			if (endpoints is null)
			{
				throw new ArgumentNullException(nameof(endpoints));
			}

			var endpointInfoMap = endpoints.ToDictionary(EndpointInfo.Create);
			var mediationElementMap = GetMediationElements(api, endpointInfoMap.Keys);

			var result = new Dictionary<Endpoint, MediationElement>();

			foreach (var endpointInfo in endpointInfoMap.Keys)
			{
				result[endpointInfoMap[endpointInfo]] = mediationElementMap[endpointInfo];
			}

			return result;
		}

		public static MediationElement GetMediationElement(MediaOpsLiveApi api, EndpointInfo endpoint)
		{
			if (api is null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			if (endpoint is null)
			{
				throw new ArgumentNullException(nameof(endpoint));
			}

			var mediationElements = GetMediationElements(api, [endpoint]);

			if (mediationElements.Count != 1)
			{
				throw new InvalidOperationException($"Expected exactly one mediation element for endpoint '{endpoint.Name}', but found {mediationElements.Count}.");
			}

			return mediationElements[endpoint];
		}

		public static MediationElement GetMediationElement(MediaOpsLiveApi api, Endpoint endpoint)
		{
			if (api is null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			if (endpoint is null)
			{
				throw new ArgumentNullException(nameof(endpoint));
			}

			return GetMediationElement(api, EndpointInfo.Create(endpoint));
		}
	}
}
