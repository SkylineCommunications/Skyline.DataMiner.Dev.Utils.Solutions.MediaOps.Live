namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Mvc.Sections
{
	using System;

	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	using IParameterDisplayInfo = Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Mvc.DisplayTypes.IParameterDisplayInfo;

	public abstract class ParameterSection : Section
	{
		public Label Label { get; }

		protected ParameterSection(IParameterDisplayInfo displayInfo)
		{
			if (displayInfo == null)
			{
				throw new ArgumentNullException(nameof(displayInfo));
			}

			// Init widgets
			Label = new Label(displayInfo.Label);
		}

		public void InitializeSection()
		{
			DefineLayout();
		}

		protected virtual void DefineLayout()
		{
			AddWidget(Label, 0, 0);
		}

		public abstract void SetValue(object value);

		public abstract object GetValue();
	}
}