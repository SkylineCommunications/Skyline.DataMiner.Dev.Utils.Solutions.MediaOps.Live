namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Profiles;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs;

	public class OrchestrationProfile
	{
		public OrchestrationProfile()
		{
			Values = new List<OrchestrationProfileValue>();
		}

		public string Definition { get; set; }

		public string Instance { get; set; }

		/// <summary>
		/// Gets or sets the provided values. For a dynamic orchestration script the name of a value is the path of its input field.
		/// </summary>
		public IList<OrchestrationProfileValue> Values { get; set; }

		/// <summary>
		/// Gets the provided values as the input values of a dynamic orchestration script.
		/// </summary>
		/// <returns>The text and numeric values, keyed by field path.</returns>
		public OrchestrationInputValues GetInputValues()
		{
			var values = new Dictionary<string, OrchestrationInputValue>(StringComparer.OrdinalIgnoreCase);

			foreach (var value in Values ?? Enumerable.Empty<OrchestrationProfileValue>())
			{
				var inputValue = value?.Name == null ? null : ToInputValue(value.Value);

				if (inputValue != null)
				{
					values[value.Name] = inputValue;
				}
			}

			return new OrchestrationInputValues(values);
		}

		/// <summary>
		/// Replaces the provided values by the input values of a dynamic orchestration script.
		/// </summary>
		/// <param name="inputValues">The explicitly provided input values, keyed by field path.</param>
		public void SetInputValues(OrchestrationInputValues inputValues)
		{
			if (inputValues == null)
			{
				throw new ArgumentNullException(nameof(inputValues));
			}

			Values = inputValues.ToDictionary()
				.Select(value => new OrchestrationProfileValue { Name = value.Key, Value = ToParameterValue(value.Value) })
				.ToList();
		}

		/// <summary>
		/// Sets or replaces the value of a single input of a dynamic orchestration script.
		/// </summary>
		/// <param name="path">The path of the input field.</param>
		/// <param name="value">The value, or <see langword="null"/> to remove it.</param>
		public void SetInputValue(string path, OrchestrationInputValue value)
		{
			if (String.IsNullOrEmpty(path))
			{
				throw new ArgumentException($"'{nameof(path)}' cannot be null or empty.", nameof(path));
			}

			if (Values == null)
			{
				Values = new List<OrchestrationProfileValue>();
			}

			foreach (var existing in Values.Where(x => String.Equals(x?.Name, path, StringComparison.OrdinalIgnoreCase)).ToList())
			{
				Values.Remove(existing);
			}

			if (value != null)
			{
				Values.Add(new OrchestrationProfileValue { Name = path, Value = ToParameterValue(value) });
			}
		}

		private static OrchestrationInputValue ToInputValue(ParameterValue value)
		{
			switch (value?.Type)
			{
				case ParameterValue.ValueType.Double:
					return Double.IsNaN(value.DoubleValue) ? null : OrchestrationInputValue.FromNumber(value.DoubleValue);

				case ParameterValue.ValueType.String:
					return value.StringValue == null ? null : OrchestrationInputValue.FromText(value.StringValue);

				default:
					return null;
			}
		}

		private static ParameterValue ToParameterValue(OrchestrationInputValue value)
		{
			return value.IsNumber
				? new ParameterValue { Type = ParameterValue.ValueType.Double, DoubleValue = value.Number }
				: new ParameterValue { Type = ParameterValue.ValueType.String, StringValue = value.Text };
		}
	}
}
