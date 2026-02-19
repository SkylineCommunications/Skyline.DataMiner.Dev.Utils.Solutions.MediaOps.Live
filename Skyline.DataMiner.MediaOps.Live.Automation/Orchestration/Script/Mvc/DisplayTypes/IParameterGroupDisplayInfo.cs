namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Mvc.DisplayTypes
{
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Mvc.Sections;

	public interface IParameterGroupDisplayInfo
	{
		string Label { get; }

		ParameterGroupSection CreateParameterGroupSection();
	}
}