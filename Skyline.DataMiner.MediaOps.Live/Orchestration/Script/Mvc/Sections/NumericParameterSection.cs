namespace Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Mvc.Sections
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.Orchestration.Script.Mvc.DisplayTypes;

	internal class NumericParameterSection : ParameterSection
	{
		public Numeric Value { get; }

		public Label Unit { get; }

		public NumericParameterSection(NumericParameterDisplayInfo info) : base(info)
		{
			if (info == null)
			{
				throw new ArgumentNullException(nameof(info));
			}

			// Init widgets
			Value = new Numeric
			{
				StepSize = info.Step,
				Decimals = info.Decimals,
			};

			if (info.Min != double.MinValue)
			{
				Value.Minimum = info.Min;
			}

			if (info.Max != double.MaxValue)
			{
				Value.Maximum = info.Max;
			}

			Unit = string.IsNullOrWhiteSpace(info.Unit) ? null : new Label(info.Unit);
		}

		protected override void DefineLayout()
		{
			base.DefineLayout();

			if (Unit is null)
			{
				AddWidget(Value, 0, 1);
			}
			else
			{
				var section = new Section();
				section.AddWidget(Value, 0, 0);
				section.AddWidget(Unit, 0, 1);
				AddSection(section, 0, 1);
			}
		}

		public override void SetValue(object value)
		{
			Value.Value = value is null ? 0 : (double)value;
		}

		public override object GetValue()
		{
			return Value.Value;
		}
	}
}