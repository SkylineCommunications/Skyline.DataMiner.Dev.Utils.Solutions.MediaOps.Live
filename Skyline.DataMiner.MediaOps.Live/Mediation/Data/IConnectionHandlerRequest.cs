namespace Skyline.DataMiner.MediaOps.Live.Mediation.Data
{
	using Skyline.DataMiner.MediaOps.Live.Mediation.ConnectionHandlers;

	internal interface IConnectionHandlerRequest
	{
		ScriptAction Action { get; }
	}
}
