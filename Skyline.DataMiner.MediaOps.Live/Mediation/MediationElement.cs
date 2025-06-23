namespace Skyline.DataMiner.MediaOps.Live.Mediation
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API.Connectivity;
	using Skyline.DataMiner.Net.Messages;

	internal class MediationElement
	{
		public MediationElement(IDmsElement dmsElement)
		{
			DmsElement = dmsElement ?? throw new ArgumentNullException(nameof(dmsElement));
		}

		internal IDmsElement DmsElement { get; }

		internal string Name => DmsElement.Name;

		public IEnumerable<PendingConnectionAction> GetPendingConnectionActions()
		{
			if (DmsElement.State != Core.DataMinerSystem.Common.ElementState.Active)
			{
				yield break;
			}

			var table = DmsElement.GetTable(3000).GetData();

			foreach (var row in table.Values)
			{
				var destinationIdValue = Convert.ToString(row[0]);
				Guid.TryParse(destinationIdValue, out var destinationId);

				var actionValue = Convert.ToString(row[2]);
				Enum.TryParse<PendingConnectionAction.PendingActionType>(actionValue, out var action);

				var pendingSourceIdValue = Convert.ToString(row[5]);
				Guid? pendingSourceId = null;
				if (!String.IsNullOrWhiteSpace(pendingSourceIdValue) &&
					Guid.TryParse(pendingSourceIdValue, out var parsedPendingSourceId))
				{
					pendingSourceId = parsedPendingSourceId;
				}

				yield return new PendingConnectionAction(destinationId, action, pendingSourceId);
			}
		}

		public static IEnumerable<MediationElement> GetMediationElements(IDms dms)
		{
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
	}
}
