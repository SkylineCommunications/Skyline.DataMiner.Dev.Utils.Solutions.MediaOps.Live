namespace Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Mvc.DisplayTypes
{
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Mvc.Sections;

	internal class DropdownParameterDisplayInfo : IParameterDisplayInfo
	{
		public string Label { get; set; }

		public List<Option<object>> Options { get; set; }

		public ParameterSection CreateParameterSection()
		{
			return new DropdownParameterSection(this);
		}
	}
}