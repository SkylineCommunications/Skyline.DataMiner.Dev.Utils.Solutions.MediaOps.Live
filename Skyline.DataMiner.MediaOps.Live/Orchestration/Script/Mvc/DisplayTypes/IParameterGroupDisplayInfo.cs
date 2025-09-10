namespace Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Mvc.DisplayTypes
{
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Mvc.Sections;

	public interface IParameterGroupDisplayInfo
	{
		string Label { get; }

		ParameterGroupSection CreateParameterGroupSection();
	}
}