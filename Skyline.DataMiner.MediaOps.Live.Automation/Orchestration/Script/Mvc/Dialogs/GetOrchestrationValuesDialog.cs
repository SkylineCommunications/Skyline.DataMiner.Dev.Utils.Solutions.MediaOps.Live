namespace Skyline.DataMiner.MediaOps.Live.Automation.Orchestration.Script.Mvc.Dialogs
{
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.Automation.Orchestration.Script.Mvc.Sections;
	using Skyline.DataMiner.MediaOps.Live.Automation.Orchestration.Script.Objects;
	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Objects;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	public class GetOrchestrationValuesDialog : Dialog
	{
		public IReadOnlyCollection<(ParameterInfo info, ParameterSection section)> Sections { get; }

		public GetOrchestrationValuesDialog(IEngine engine, IReadOnlyCollection<ParameterInfo> parameterInfos) : base(engine)
		{
			InitializeSections(parameterInfos, out var parameterSections, out var sectionsToDisplay);
			Sections = parameterSections;

			// Set title
			Title = "Enter missing values";
			Button = new Button("Apply");

			// Define layout
			foreach (var section in sectionsToDisplay)
			{
				AddSection(section, RowCount, 0);
			}

			AddWidget(new WhiteSpace(), RowCount, 0);
			AddWidget(Button, RowCount, 0);
		}

		private static void InitializeSections(IReadOnlyCollection<ParameterInfo> parameterInfos, out List<(ParameterInfo info, ParameterSection section)> parameterSections, out List<Section> sectionsToDisplay)
		{
			parameterSections = new List<(ParameterInfo info, ParameterSection section)>(parameterInfos.Count);
			sectionsToDisplay = new List<Section>(parameterInfos.Count);

			foreach (IGrouping<ParameterGroup, ParameterInfo> grouping in parameterInfos.Where(paramInfo => paramInfo.Group != null).GroupBy(paramInfo => paramInfo.Group))
			{
				var section = grouping.Key.DisplayInfo.CreateParameterGroupSection();
				parameterSections.AddRange(section.InitializeSection(parameterInfos));
				sectionsToDisplay.Add(section);
			}

			foreach (ParameterInfo parameterInfo in parameterInfos.Where(paramInfo => paramInfo.Group == null))
			{
				var section = InitializeParameterSection(parameterInfo);
				parameterSections.Add((parameterInfo, section));
				sectionsToDisplay.Add(section);
			}
		}

		private static ParameterSection InitializeParameterSection(ParameterInfo info)
		{
			var section = info.DisplayInfo.CreateParameterSection();
			section.InitializeSection();
			section.SetValue(info.Value);
			return section;
		}

		public void UpdateValues()
		{
			foreach (var (info, section) in Sections)
			{
				info.Value = section.GetValue();
			}
		}

		public Button Button { get; }
	}
}