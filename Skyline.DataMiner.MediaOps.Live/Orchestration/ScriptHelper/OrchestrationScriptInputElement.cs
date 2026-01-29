namespace Skyline.DataMiner.MediaOps.Live.Orchestration.ScriptHelper
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.Net;

	public class OrchestrationScriptInputElement
	{
		public OrchestrationScriptInputElement(IDmsAutomationScriptDummy inputDummy)
		{
			InputDummy = inputDummy ?? throw new ArgumentNullException(nameof(inputDummy));
		}

		public IDmsAutomationScriptDummy InputDummy { get; }

		public string Name => InputDummy.Description;

		public string ProtocolName => InputDummy.Protocol.Name;

		public string ProtocolVersion => InputDummy.Protocol.Version;

		public ICollection<IDmsElement> GetApplicableElements(IDms dms)
		{
			if (dms is null)
			{
				throw new ArgumentNullException(nameof(dms));
			}

			return dms.GetElements()
				.Where(e => e.Protocol.Name == ProtocolName && e.Protocol.Version == ProtocolVersion)
				.ToList();
		}

		public ICollection<IDmsElement> GetApplicableElements(IConnection connection)
		{
			if (connection is null)
			{
				throw new ArgumentNullException(nameof(connection));
			}

			return GetApplicableElements(connection.GetDms());
		}

		public ICollection<IDmsElement> GetApplicableElements(IMediaOpsLiveApi api)
		{
			if (api is null)
			{
				throw new ArgumentNullException(nameof(api));
			}

			return GetApplicableElements(api.Connection);
		}
	}
}
