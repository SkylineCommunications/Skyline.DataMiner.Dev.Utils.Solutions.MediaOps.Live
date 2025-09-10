namespace Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Mvc.DisplayTypes
{
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Mvc.Sections;

	public interface IParameterDisplayInfo
	{
		string Label { get; }

		ParameterSection CreateParameterSection();
	}
}