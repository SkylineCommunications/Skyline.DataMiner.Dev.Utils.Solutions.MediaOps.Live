namespace Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Mvc.DisplayTypes
{
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Mvc.Sections;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Objects;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	internal class PresetGroupDisplayInfo : IParameterGroupDisplayInfo
	{
		public string Label { get; set; }

		public List<Option<PresetInfo>> Presets { get; set; }

		public ParameterGroupSection CreateParameterGroupSection()
		{
			return new PresetGroupSection(this);
		}

		internal class PresetInfo
		{
			public List<(ParameterInfo info, object value)> ParameterValues { get; } = new List<(ParameterInfo info, object value)>();
		}
	}
}