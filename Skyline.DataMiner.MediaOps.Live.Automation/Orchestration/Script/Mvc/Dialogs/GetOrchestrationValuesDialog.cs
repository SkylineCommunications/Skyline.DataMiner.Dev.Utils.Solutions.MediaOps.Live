namespace Skyline.DataMiner.MediaOps.Live.Automation.Orchestration.Script.Mvc.Dialogs
{
	using System.Collections.Generic;

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
			ParameterGroup lastGroup = null;
			List<ParameterInfo> infosForLastGroup = null;
			foreach (var parameterInfo in parameterInfos)
			{
				if (lastGroup != parameterInfo.Group)
				{
					if (lastGroup != null)
					{
						InitializeGroupSection(lastGroup, infosForLastGroup, parameterSections, sectionsToDisplay);
					}

					lastGroup = parameterInfo.Group;
					if (lastGroup != null)
					{
						infosForLastGroup = new List<ParameterInfo>();
					}
				}

				if (parameterInfo.Group is null)
				{
					var section = InitializeParameterSection(parameterInfo);
					parameterSections.Add((parameterInfo, section));
					sectionsToDisplay.Add(section);
					continue;
				}

				infosForLastGroup.Add(parameterInfo);
			}

			if (lastGroup != null)
			{
				InitializeGroupSection(lastGroup, infosForLastGroup, parameterSections, sectionsToDisplay);
			}
		}

		private static void InitializeGroupSection(ParameterGroup group, IEnumerable<ParameterInfo> parameterInfos, List<(ParameterInfo info, ParameterSection section)> parameterSections, List<Section> sectionsToDisplay)
		{
			var section = group.DisplayInfo.CreateParameterGroupSection();
			parameterSections.AddRange(section.InitializeSection(parameterInfos));
			sectionsToDisplay.Add(section);
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