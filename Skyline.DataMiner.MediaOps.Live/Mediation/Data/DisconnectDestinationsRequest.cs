namespace Skyline.DataMiner.MediaOps.Live.Mediation.Data
{
	using System.Collections.Generic;

	using Newtonsoft.Json;
	using Newtonsoft.Json.Converters;

	using Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers;

	public class DisconnectDestinationsRequest : IConnectionHandlerRequest
	{
		[JsonConverter(typeof(StringEnumConverter))]
		public ScriptAction Action => ScriptAction.Disconnect;

		public ICollection<EndpointInfo> Destinations { get; set; }
	}
}
