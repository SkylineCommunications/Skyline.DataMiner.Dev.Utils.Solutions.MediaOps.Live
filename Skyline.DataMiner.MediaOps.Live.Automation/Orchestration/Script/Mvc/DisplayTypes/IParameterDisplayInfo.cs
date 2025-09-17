namespace Skyline.DataMiner.MediaOps.Live.Automation.Orchestration.Script.Mvc.DisplayTypes
{
	using Skyline.DataMiner.MediaOps.Live.Automation.Orchestration.Script.Mvc.Sections;

	public interface IParameterDisplayInfo
	{
		string Label { get; }

		ParameterSection CreateParameterSection();
	}
}