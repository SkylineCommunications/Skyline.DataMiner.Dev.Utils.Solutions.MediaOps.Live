namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.Profiles;

	using Parameter = Skyline.DataMiner.Net.Profiles.Parameter;

	/// <summary>
	/// Replaces the profile backed fields of an input definition by the typed field that matches the profile parameter,
	/// applying the narrowing the script asked for.
	/// </summary>
	public class OrchestrationInputProfileResolver
	{
		private readonly ProfileHelper _profileHelper;
		private readonly Dictionary<string, Parameter> _parametersByName = new Dictionary<string, Parameter>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationInputProfileResolver"/> class.
		/// </summary>
		/// <param name="profileHelper">The helper used to read profile parameters.</param>
		public OrchestrationInputProfileResolver(ProfileHelper profileHelper)
		{
			_profileHelper = profileHelper ?? throw new ArgumentNullException(nameof(profileHelper));
		}

		/// <summary>
		/// Resolves every profile backed field of the specified definition.
		/// </summary>
		/// <param name="definition">The definition to resolve.</param>
		/// <exception cref="InvalidOperationException">Thrown when a profile parameter cannot be found or when the narrowing is not valid.</exception>
		public void Resolve(OrchestrationInputDefinition definition)
		{
			if (definition == null)
			{
				throw new ArgumentNullException(nameof(definition));
			}

			ResolveItems(definition.Items);
		}

		private static OrchestrationInputField CreateResolvedField(OrchestrationProfileInputField source, Parameter parameter)
		{
			switch (parameter.Type)
			{
				case Parameter.ParameterType.Discrete:
					return CreateDiscreteField(source, parameter);

				case Parameter.ParameterType.Number:
					return CreateNumberField(source, parameter);

				case Parameter.ParameterType.Text:
					return CreateTextField(source, parameter);

				default:
					throw new InvalidOperationException($"Profile parameter '{parameter.Name}' has unsupported type {parameter.Type}.");
			}
		}

		private static OrchestrationInputField CreateDiscreteField(OrchestrationProfileInputField source, Parameter parameter)
		{
			if (source.Minimum.HasValue || source.Maximum.HasValue || source.StepSize.HasValue)
			{
				throw new InvalidOperationException($"'{source.Path}' narrows a range, but profile parameter '{parameter.Name}' is discrete.");
			}

			if (source.AllowedTextValues.Count > 0 && source.AllowedNumberValues.Count > 0)
			{
				throw new InvalidOperationException($"'{source.Path}' narrows profile parameter '{parameter.Name}' with both text and numeric values.");
			}

			var allowedValues = source.GetAllowedValues().ToList();

			var field = new OrchestrationDiscreteInputField();

			var displayValues = new Queue<string>(parameter.DiscreetDisplayValues);

			foreach (var discrete in parameter.Discretes)
			{
				var display = displayValues.Count > 0 ? displayValues.Dequeue() : discrete;

				// An empty override means the profile parameter is taken as is.
				if (allowedValues.Count > 0
					&& !allowedValues.Any(allowed => OrchestrationInputValueConverter.AreEqual(allowed, discrete)))
				{
					continue;
				}

				field.Options.Add(new OrchestrationInputOption(display, discrete));
			}

			var unknown = allowedValues
				.Where(allowed => !parameter.Discretes.Any(discrete => OrchestrationInputValueConverter.AreEqual(allowed, discrete)))
				.ToList();

			if (unknown.Count > 0)
			{
				throw new InvalidOperationException(
					$"'{source.Path}' allows {String.Join(", ", unknown.Select(x => $"'{OrchestrationInputValueConverter.ToStringValue(x)}'"))}, which profile parameter '{parameter.Name}' does not define.");
			}

			return field;
		}

		private static OrchestrationInputField CreateNumberField(OrchestrationProfileInputField source, Parameter parameter)
		{
			if (source.HasAllowedValues)
			{
				throw new InvalidOperationException($"'{source.Path}' narrows the allowed values, but profile parameter '{parameter.Name}' is not discrete.");
			}

			var minimum = Widest(source.Minimum, parameter.RangeMin, (scriptValue, profileValue) => scriptValue < profileValue, source.Path, parameter.Name, "minimum");
			var maximum = Widest(source.Maximum, parameter.RangeMax, (scriptValue, profileValue) => scriptValue > profileValue, source.Path, parameter.Name, "maximum");

			if (minimum.HasValue && maximum.HasValue && minimum.Value > maximum.Value)
			{
				throw new InvalidOperationException($"'{source.Path}' narrows the range of profile parameter '{parameter.Name}' to an empty range.");
			}

			return new OrchestrationNumberInputField
			{
				Minimum = minimum,
				Maximum = maximum,
				StepSize = source.StepSize ?? NullIfNaN(parameter.Stepsize),
				Decimals = parameter.Decimals,
				Unit = parameter.Units,
			};
		}

		private static OrchestrationInputField CreateTextField(OrchestrationProfileInputField source, Parameter parameter)
		{
			if (source.HasOverride)
			{
				throw new InvalidOperationException($"'{source.Path}' narrows the definition, but profile parameter '{parameter.Name}' is free text.");
			}

			return new OrchestrationTextInputField();
		}

		private static double? Widest(double? scriptValue, double profileValue, Func<double, double, bool> isWiderThanProfile, string path, string parameterName, string bound)
		{
			var profileBound = NullIfNaN(profileValue);

			if (!scriptValue.HasValue)
			{
				return profileBound;
			}

			if (profileBound.HasValue && isWiderThanProfile(scriptValue.Value, profileBound.Value))
			{
				throw new InvalidOperationException($"'{path}' widens the {bound} of profile parameter '{parameterName}'. An override can only narrow it.");
			}

			return scriptValue;
		}

		private static double? NullIfNaN(double value)
		{
			return Double.IsNaN(value) ? (double?)null : value;
		}

		private void ResolveItems(IList<OrchestrationInputItem> items)
		{
			for (var index = 0; index < items.Count; index++)
			{
				switch (items[index])
				{
					case OrchestrationInputGroup group:
						ResolveItems(group.Children);
						break;

					case OrchestrationProfileInputField profileField:
						items[index] = ResolveField(profileField);
						break;
				}
			}
		}

		private OrchestrationInputField ResolveField(OrchestrationProfileInputField source)
		{
			var parameter = GetProfileParameter(source.ProfileParameterName);

			var resolved = CreateResolvedField(source, parameter);

			resolved.Name = source.Name;
			resolved.Path = source.Path;
			resolved.Description = source.Description;
			resolved.IsRequired = source.IsRequired;
			resolved.TriggersReevaluation = source.TriggersReevaluation;
			resolved.Value = source.Value;
			resolved.DefaultValue = source.DefaultValue ?? GetDefaultValue(parameter);
			resolved.ProfileParameterName = parameter.Name;
			resolved.ProfileParameterId = parameter.ID;

			return resolved;
		}

		private static object GetDefaultValue(Parameter parameter)
		{
			if (parameter.DefaultValue == null)
			{
				return null;
			}

			if (parameter.DefaultValue.StringValue != null)
			{
				return parameter.DefaultValue.StringValue;
			}

			return Double.IsNaN(parameter.DefaultValue.DoubleValue) ? null : (object)parameter.DefaultValue.DoubleValue;
		}

		private Parameter GetProfileParameter(string name)
		{
			if (_parametersByName.TryGetValue(name, out var cached))
			{
				return cached;
			}

			var parameters = _profileHelper.ProfileParameters.Read(ParameterExposers.Name.Equal(name));

			if (parameters.Count == 0)
			{
				throw new InvalidOperationException($"No profile parameter found with name '{name}'.");
			}

			if (parameters.Count > 1)
			{
				throw new InvalidOperationException($"Multiple profile parameters found with name '{name}'.");
			}

			_parametersByName[name] = parameters[0];

			return parameters[0];
		}
	}
}
