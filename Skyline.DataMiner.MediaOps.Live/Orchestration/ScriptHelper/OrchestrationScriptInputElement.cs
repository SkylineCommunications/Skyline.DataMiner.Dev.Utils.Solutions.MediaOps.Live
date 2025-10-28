namespace Skyline.DataMiner.MediaOps.Live.Orchestration.ScriptHelper
{
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Messages;

	/// <summary>
	/// Represents an element input for an orchestration script.
	/// </summary>
	public class OrchestrationScriptInputElement
	{
		/// <summary>
		/// Gets or sets the automation protocol information.
		/// </summary>
		public AutomationProtocolInfo ProtocolInfo { get; set; }

		/// <summary>
		/// Gets the list of applicable elements for this protocol.
		/// </summary>
		/// <param name="connection">The DataMiner connection.</param>
		/// <returns>A list of applicable DataMiner elements.</returns>
		public List<IDmsElement> GetApplicableElements(IConnection connection)
		{
			IDms dms = connection.GetDms();

			return dms.GetElements()
				.Where(e => e.Protocol.Name == ProtocolInfo.ProtocolName && e.Protocol.Version == ProtocolInfo.ProtocolVersion)
				.ToList();
		}
	}
}
