namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Objects
{
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Mvc.DisplayTypes;

	public class ParameterGroup
	{
		public string Description { get; set; }

		public IParameterGroupDisplayInfo DisplayInfo { get; set; }

		public IDMAObjectRef Reference { get; set; }

		public string Type { get; set; }

		public override string ToString()
		{
			return $"{Type} with reference {Reference} ({Description})";
		}
	}
}