namespace Skyline.DataMiner.MediaOps.Live.Mediation.Data
{
	using Newtonsoft.Json;
	using Newtonsoft.Json.Converters;

	public interface IConnectionHandlerRequest
	{
		[JsonConverter(typeof(StringEnumConverter))]
		ScriptAction Action { get; }
	}
}
