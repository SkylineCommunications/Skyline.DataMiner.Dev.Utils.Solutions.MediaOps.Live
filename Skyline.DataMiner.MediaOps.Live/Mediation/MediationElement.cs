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
				yield return new PendingConnectionAction(row);
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
