namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Mvc.Dialogs
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs;
	using Skyline.DataMiner.Utils.InteractiveAutomationScript;

	/// <summary>
	/// Asks the operator for the inputs of a dynamic orchestration script, evaluating them again whenever a field that shapes them changes.
	/// </summary>
	internal sealed class GetOrchestrationInputValuesDialog : Dialog
	{
		private const int WidgetColumn = OrchestrationInputDefinition.MaxDepth;

		private readonly Func<OrchestrationInputValues, OrchestrationInputDefinition> _reevaluate;
		private readonly Dictionary<string, OrchestrationInputValue> _providedValues;
		private readonly List<FieldEditor> _editors = new List<FieldEditor>();
		private readonly Button _applyButton = new Button("Apply");

		private OrchestrationInputDefinition _definition;
		private string _errorMessage;

		public GetOrchestrationInputValuesDialog(
			IEngine engine,
			OrchestrationInputDefinition definition,
			OrchestrationInputValues providedValues,
			Func<OrchestrationInputValues, OrchestrationInputDefinition> reevaluate)
			: base(engine)
		{
			_definition = definition ?? throw new ArgumentNullException(nameof(definition));
			_reevaluate = reevaluate ?? throw new ArgumentNullException(nameof(reevaluate));
			_providedValues = (providedValues ?? OrchestrationInputValues.Empty).ToDictionary();

			Title = "Enter the orchestration inputs";
			_applyButton.Pressed += (sender, args) => Apply();

			Build();
		}

		public event EventHandler Completed;

		public OrchestrationInputValues ProvidedValues => new OrchestrationInputValues(_providedValues);

		private void Apply()
		{
			CollectValues();

			if (Reevaluate() && _definition.TryValidateValues(out _))
			{
				Completed?.Invoke(this, EventArgs.Empty);
				return;
			}

			Build();
		}

		private void OnValueChanged()
		{
			CollectValues();
			Reevaluate();
			Build();
		}

		private bool Reevaluate()
		{
			try
			{
				_definition = _reevaluate(ProvidedValues);
				_errorMessage = null;
				return true;
			}
			catch (Exception e)
			{
				// The previous inputs stay on screen, so the operator can correct the value and try again.
				_errorMessage = $"The inputs could not be updated: {e.Message}";
				return false;
			}
		}

		private void CollectValues()
		{
			foreach (var editor in _editors)
			{
				var value = editor.Read();

				if (value == editor.ShownValue)
				{
					continue;
				}

				if (value == null)
				{
					_providedValues.Remove(editor.Field.Path);
				}
				else
				{
					_providedValues[editor.Field.Path] = value;
				}
			}
		}

		private void Build()
		{
			Clear();
			_editors.Clear();

			AddItems(_definition.Items, 0);

			if (!String.IsNullOrEmpty(_errorMessage))
			{
				AddWidget(new Label(_errorMessage) { IsMultiline = true }, RowCount, 0, 1, WidgetColumn + 1);
			}

			AddWidget(new WhiteSpace(), RowCount, 0);
			AddWidget(_applyButton, RowCount, 0, 1, WidgetColumn + 1);
		}

		private void AddItems(IEnumerable<OrchestrationInputItem> items, int depth)
		{
			foreach (var item in items)
			{
				var row = RowCount;

				if (item is OrchestrationInputGroup group)
				{
					AddWidget(new Label(group.Name) { Style = TextStyle.Bold, Tooltip = group.Description }, row, depth, 1, WidgetColumn - depth + 1);
					AddItems(group.Children, depth + 1);
				}
				else if (item is OrchestrationInputField field)
				{
					AddWidget(new Label(field.Name) { Tooltip = field.Description }, row, depth, 1, WidgetColumn - depth);

					var editor = CreateEditor(field);
					_editors.Add(editor);
					AddWidget(editor.Widget, row, WidgetColumn);

					if (editor.Unit != null)
					{
						AddWidget(new Label(editor.Unit), row, WidgetColumn + 1);
					}
				}
			}
		}

		private FieldEditor CreateEditor(OrchestrationInputField field)
		{
			FieldEditor editor;

			switch (field)
			{
				case OrchestrationDiscreteInputField discrete:
					editor = CreateDiscreteEditor(discrete);
					break;

				case OrchestrationNumberInputField number:
					editor = CreateNumberEditor(number);
					break;

				case OrchestrationDateTimeInputField dateTime:
					editor = CreateDateTimeEditor(dateTime);
					break;

				case OrchestrationTimeSpanInputField timeSpan:
					editor = CreateTimeSpanEditor(timeSpan);
					break;

				default:
					editor = CreateTextEditor(field);
					break;
			}

			editor.Widget.IsEnabled = !field.IsDisabled;

			if (editor.Widget is IValidationWidget validationWidget && !field.TryValidate(out var error))
			{
				validationWidget.ValidationState = UIValidationState.Invalid;
				validationWidget.ValidationText = error;
			}

			return editor;
		}

		private FieldEditor CreateTextEditor(OrchestrationInputField field)
		{
			var shownValue = field.GetEffectiveValue();
			var textBox = new TextBox(shownValue?.ToString() ?? String.Empty);

			if (field.TriggersReevaluation)
			{
				textBox.FocusLost += (sender, args) => OnValueChanged();
			}

			return new FieldEditor(field, textBox, shownValue, () => String.IsNullOrEmpty(textBox.Text) ? null : OrchestrationInputValue.FromText(textBox.Text));
		}

		private FieldEditor CreateNumberEditor(OrchestrationNumberInputField field)
		{
			var shownValue = field.GetEffectiveValue();
			var numeric = new Numeric
			{
				Decimals = field.Decimals,
				StepSize = field.StepSize ?? Math.Pow(10, -field.Decimals),
			};

			if (field.Minimum.HasValue)
			{
				numeric.Minimum = field.Minimum.Value;
			}

			if (field.Maximum.HasValue)
			{
				numeric.Maximum = field.Maximum.Value;
			}

			numeric.Value = shownValue != null && shownValue.TryGetNumber(out var number) ? number : field.Minimum ?? 0;

			if (field.TriggersReevaluation)
			{
				numeric.Changed += (sender, args) => OnValueChanged();
			}

			return new FieldEditor(field, numeric, shownValue, () => OrchestrationInputValue.FromNumber(numeric.Value)) { Unit = field.Unit };
		}

		private FieldEditor CreateDiscreteEditor(OrchestrationDiscreteInputField field)
		{
			var shownValue = field.GetEffectiveValue();
			var options = field.Options.Select(option => new Option<OrchestrationInputValue>(option.GetDisplayText(), option.Value)).ToList();
			var selected = options.FirstOrDefault(option => option.Value == shownValue);

			if (selected == null)
			{
				selected = new Option<OrchestrationInputValue>(String.Empty, null);
				options.Insert(0, selected);
			}

			var dropDown = new DropDown<OrchestrationInputValue>(options, selected);

			if (field.TriggersReevaluation)
			{
				dropDown.Changed += (sender, args) => OnValueChanged();
			}

			return new FieldEditor(field, dropDown, shownValue, () => dropDown.Selected);
		}

		private FieldEditor CreateDateTimeEditor(OrchestrationDateTimeInputField field)
		{
			var shownValue = field.GetEffectiveValue();
			var picker = new DateTimePicker
			{
				IsTimePickerVisible = field.Precision != OrchestrationTimePrecision.Day,
			};

			if (field.Minimum.HasValue)
			{
				picker.Minimum = OrchestrationInputValue.ToUniversal(field.Minimum.Value).ToLocalTime();
			}

			if (field.Maximum.HasValue)
			{
				picker.Maximum = OrchestrationInputValue.ToUniversal(field.Maximum.Value).ToLocalTime();
			}

			picker.DateTime = shownValue != null && shownValue.TryGetDateTime(out var dateTime)
				? dateTime.ToLocalTime()
				: Truncate(DateTime.Now, field.Precision);

			if (field.TriggersReevaluation)
			{
				picker.Changed += (sender, args) => OnValueChanged();
			}

			return new FieldEditor(field, picker, shownValue, () => OrchestrationInputValue.FromDateTime(ToUniversal(Truncate(picker.DateTime, field.Precision))));
		}

		private FieldEditor CreateTimeSpanEditor(OrchestrationTimeSpanInputField field)
		{
			var shownValue = field.GetEffectiveValue();
			var time = new Time
			{
				HasSeconds = field.Precision == OrchestrationTimePrecision.Second,
			};

			if (field.Minimum.HasValue)
			{
				time.Minimum = field.Minimum.Value;
			}

			if (field.Maximum.HasValue)
			{
				time.Maximum = field.Maximum.Value;
			}

			time.TimeSpan = shownValue != null && shownValue.TryGetTimeSpan(out var timeSpan) ? timeSpan : field.Minimum ?? TimeSpan.Zero;

			if (field.TriggersReevaluation)
			{
				time.Changed += (sender, args) => OnValueChanged();
			}

			return new FieldEditor(field, time, shownValue, () => OrchestrationInputValue.FromTimeSpan(time.TimeSpan));
		}

		// The picker works in the time zone of the server; a value without a kind is taken as such.
		private static DateTime ToUniversal(DateTime dateTime)
		{
			return dateTime.Kind == DateTimeKind.Unspecified
				? DateTime.SpecifyKind(dateTime, DateTimeKind.Local).ToUniversalTime()
				: dateTime.ToUniversalTime();
		}

		private static DateTime Truncate(DateTime dateTime, OrchestrationTimePrecision precision)
		{
			switch (precision)
			{
				case OrchestrationTimePrecision.Day:
					return dateTime.Date;

				case OrchestrationTimePrecision.Hour:
					return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0, dateTime.Kind);

				case OrchestrationTimePrecision.Minute:
					return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0, dateTime.Kind);

				default:
					return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Kind);
			}
		}

		private sealed class FieldEditor
		{
			public FieldEditor(OrchestrationInputField field, InteractiveWidget widget, OrchestrationInputValue shownValue, Func<OrchestrationInputValue> read)
			{
				Field = field;
				Widget = widget;
				ShownValue = shownValue;
				Read = read;
			}

			public OrchestrationInputField Field { get; }

			public InteractiveWidget Widget { get; }

			// Only values the operator changed become provided values, so untouched defaults stay defaults.
			public OrchestrationInputValue ShownValue { get; }

			public Func<OrchestrationInputValue> Read { get; }

			public string Unit { get; set; }
		}
	}
}
