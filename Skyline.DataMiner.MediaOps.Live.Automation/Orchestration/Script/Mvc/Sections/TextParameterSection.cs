namespace Skyline.DataMiner.MediaOps.Live.Automation.Orchestration.Script.Mvc.Sections
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.Automation.Orchestration.Script.Mvc.DisplayTypes;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	internal class TextParameterSection : ParameterSection
	{
		public TextBox Value { get; }

		public TextParameterSection(TextParameterDisplayInfo info) : base(info)
		{
			if (info == null)
			{
				throw new ArgumentNullException(nameof(info));
			}

			// Init widgets
			Value = new TextBox();
		}

		protected override void DefineLayout()
		{
			base.DefineLayout();

			var section = new Section();
			section.AddWidget(Value, 0, 0);
			AddSection(section, 0, 1);
		}

		public override void SetValue(object value)
		{
			Value.Text = value.ToString();
		}

		public override object GetValue()
		{
			return Value.Text;
		}
	}
}
