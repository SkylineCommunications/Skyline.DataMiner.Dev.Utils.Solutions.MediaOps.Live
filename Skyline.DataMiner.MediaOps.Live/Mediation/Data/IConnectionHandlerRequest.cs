namespace Skyline.DataMiner.MediaOps.Live.Mediation.Data
{
	using Newtonsoft.Json;
	using Newtonsoft.Json.Converters;

	using Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers;

	internal interface IConnectionHandlerRequest
	{
		[JsonConverter(typeof(StringEnumConverter))]
		ScriptAction Action { get; }
	}
}
