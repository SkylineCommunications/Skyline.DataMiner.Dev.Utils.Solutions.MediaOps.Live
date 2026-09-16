namespace Skyline.DataMiner.Solutions.MediaOps.Live.Tests.Automation.Orchestration
{
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Mvc.DisplayTypes;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Mvc.Sections;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Objects;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	[TestClass]
	public class PresetGroupSectionTests
	{
		[DataTestMethod]
		[DataRow("")]
		[DataRow("A")]
		[DataRow("ABCD")]
		[DataRow("ABCDE")]
		public void InitializeSection_WithShortLabel_DoesNotThrow(string label)
		{
			var info = new PresetGroupDisplayInfo
			{
				Label = label,
				Presets = new List<Option<PresetGroupDisplayInfo.PresetInfo>>(),
			};
			var section = new PresetGroupSection(info);

			section.InitializeSection(Enumerable.Empty<ParameterInfo>()).ToList();
		}
	}
}
