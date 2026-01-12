namespace Skyline.DataMiner.MediaOps.Live.Orchestration.ScriptHelper
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;

	public class OrchestrationScriptInputElement
	{
		public OrchestrationScriptInputElement(AutomationProtocolInfo protocolInfo)
		{
			ProtocolInfo = protocolInfo ?? throw new ArgumentNullException(nameof(protocolInfo));
		}

		public AutomationProtocolInfo ProtocolInfo { get; }

		public string Name => ProtocolInfo.Description;

		public ICollection<IDmsElement> GetApplicableElements(IConnection connection)
		{
			IDms dms = connection.GetDms();

			return dms.GetElements()
				.Where(e => e.Protocol.Name == ProtocolInfo.ProtocolName && e.Protocol.Version == ProtocolInfo.ProtocolVersion)
				.ToList();
		}
	}
}
