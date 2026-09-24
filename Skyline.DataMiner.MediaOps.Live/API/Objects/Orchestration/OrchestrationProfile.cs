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
				if (value?.Name != null && (value.Value?.Type == ParameterValue.ValueType.String || value.Value?.Type == ParameterValue.ValueType.Double))
				{
					values[value.Name] = OrchestrationInputValue.FromParameterValue(value.Value);
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
				.Select(value => new OrchestrationProfileValue { Name = value.Key, Value = value.Value.ToParameterValue() })
				.ToList();
		}
	}
}
