namespace Skyline.DataMiner.MediaOps.Live.Automation.Orchestration.Script.Mvc.DisplayTypes
{
	using Skyline.DataMiner.MediaOps.Live.Automation.Orchestration.Script.Mvc.Sections;

	internal class TextParameterDisplayInfo : IParameterDisplayInfo
	{
		public string Label { get; set; }

		public ParameterSection CreateParameterSection()
		{
			return new TextParameterSection(this);
		}
	}
}