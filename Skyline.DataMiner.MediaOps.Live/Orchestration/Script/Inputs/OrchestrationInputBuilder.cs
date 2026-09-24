namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Builds an <see cref="OrchestrationInputDefinition"/>.
	/// The same builder is used for the root of the definition and for the content of a group.
	/// </summary>
	public sealed class OrchestrationInputBuilder
	{
		private readonly string _parentPath;
		private readonly List<OrchestrationInputItem> _items = new List<OrchestrationInputItem>();

		/// <summary>
		/// Initializes a new instance of the <see cref="OrchestrationInputBuilder"/> class.
		/// </summary>
		public OrchestrationInputBuilder() : this(null)
		{
		}

		private OrchestrationInputBuilder(string parentPath)
		{
			_parentPath = parentPath;
		}

		/// <summary>
		/// Adds a free text input field.
		/// </summary>
		/// <param name="name">The name of the field, unique within its parent.</param>
		/// <param name="configure">An optional callback to further configure the field.</param>
		/// <returns>The current builder.</returns>
		public OrchestrationInputBuilder AddText(string name, Action<OrchestrationTextInputField> configure = null)
		{
			return AddField(name, configure);
		}

		/// <summary>
		/// Adds a numeric input field.
		/// </summary>
		/// <param name="name">The name of the field, unique within its parent.</param>
		/// <param name="configure">An optional callback to further configure the field.</param>
		/// <returns>The current builder.</returns>
		public OrchestrationInputBuilder AddNumber(string name, Action<OrchestrationNumberInputField> configure = null)
		{
			return AddField(name, configure);
		}

		/// <summary>
		/// Adds a date and time input field.
		/// </summary>
		/// <param name="name">The name of the field, unique within its parent.</param>
		/// <param name="configure">An optional callback to further configure the field.</param>
		/// <returns>The current builder.</returns>
		public OrchestrationInputBuilder AddDateTime(string name, Action<OrchestrationDateTimeInputField> configure = null)
		{
			return AddField(name, configure);
		}

		/// <summary>
		/// Adds a duration input field.
		/// </summary>
		/// <param name="name">The name of the field, unique within its parent.</param>
		/// <param name="configure">An optional callback to further configure the field.</param>
		/// <returns>The current builder.</returns>
		public OrchestrationInputBuilder AddTimeSpan(string name, Action<OrchestrationTimeSpanInputField> configure = null)
		{
			return AddField(name, configure);
		}

		/// <summary>
		/// Adds an input field that is limited to the specified options.
		/// </summary>
		/// <param name="name">The name of the field, unique within its parent.</param>
		/// <param name="options">The options the operator can choose from.</param>
		/// <param name="configure">An optional callback to further configure the field.</param>
		/// <returns>The current builder.</returns>
		public OrchestrationInputBuilder AddDiscrete(string name, IEnumerable<OrchestrationInputOption> options, Action<OrchestrationDiscreteInputField> configure = null)
		{
			if (options == null)
			{
				throw new ArgumentNullException(nameof(options));
			}

			return AddField<OrchestrationDiscreteInputField>(
				name,
				field =>
				{
					field.Options.AddRange(options);
					configure?.Invoke(field);
				});
		}

		/// <summary>
		/// Adds an input field that is limited to the specified options.
		/// </summary>
		/// <param name="name">The name of the field, unique within its parent.</param>
		/// <param name="options">The options the operator can choose from. The option text is used as the value.</param>
		/// <returns>The current builder.</returns>
		public OrchestrationInputBuilder AddDiscrete(string name, params string[] options)
		{
			return AddDiscrete(name, null, options);
		}

		/// <summary>
		/// Adds an input field that is limited to the specified options.
		/// </summary>
		/// <param name="name">The name of the field, unique within its parent.</param>
		/// <param name="configure">An optional callback to further configure the field.</param>
		/// <param name="options">The options the operator can choose from. The option text is used as the value.</param>
		/// <returns>The current builder.</returns>
		public OrchestrationInputBuilder AddDiscrete(string name, Action<OrchestrationDiscreteInputField> configure, params string[] options)
		{
			return AddDiscrete(name, (options ?? new string[0]).Select(x => new OrchestrationInputOption(x)), configure);
		}

		/// <summary>
		/// Adds a field that is backed by a profile parameter. The profile parameter determines the value type and what the field accepts;
		/// the optional callback can only narrow that, for example by restricting the discretes or tightening the range.
		/// Its value is stored as a profile parameter value and takes part in capability and capacity matching.
		/// </summary>
		/// <param name="name">The name of the field, unique within its parent.</param>
		/// <param name="profileParameterName">The name of the profile parameter that backs this field.</param>
		/// <param name="configure">An optional callback to narrow what the profile parameter accepts.</param>
		/// <returns>The current builder.</returns>
		public OrchestrationInputBuilder AddProfileParameter(string name, string profileParameterName, Action<OrchestrationProfileInputField> configure = null)
		{
			if (String.IsNullOrEmpty(profileParameterName))
			{
				throw new ArgumentException($"'{nameof(profileParameterName)}' cannot be null or empty.", nameof(profileParameterName));
			}

			return AddField<OrchestrationProfileInputField>(
				name,
				field =>
				{
					field.ProfileParameterName = profileParameterName;
					configure?.Invoke(field);
				});
		}

		/// <summary>
		/// Adds a field that is backed by the profile parameter of the same name.
		/// </summary>
		/// <param name="profileParameterName">The name of the profile parameter that backs this field.</param>
		/// <param name="configure">An optional callback to narrow what the profile parameter accepts.</param>
		/// <returns>The current builder.</returns>
		public OrchestrationInputBuilder AddProfileParameter(string profileParameterName, Action<OrchestrationProfileInputField> configure = null)
		{
			return AddProfileParameter(profileParameterName, profileParameterName, configure);
		}

		/// <summary>
		/// Adds a group that bundles related input items.
		/// Call this in a loop to repeat a group, for example once per destination.
		/// </summary>
		/// <param name="name">The name of the group, unique within its parent.</param>
		/// <param name="configure">A callback that adds the content of the group.</param>
		/// <returns>The current builder.</returns>
		public OrchestrationInputBuilder AddGroup(string name, Action<OrchestrationInputBuilder> configure)
		{
			if (configure == null)
			{
				throw new ArgumentNullException(nameof(configure));
			}

			ValidateName(name);

			var group = new OrchestrationInputGroup
			{
				Name = name,
				Path = OrchestrationInputPath.Combine(_parentPath, name),
			};

			var childBuilder = new OrchestrationInputBuilder(group.Path);
			configure(childBuilder);
			group.Children.AddRange(childBuilder._items);

			_items.Add(group);

			return this;
		}

		/// <summary>
		/// Builds the input definition and verifies that its structure is valid.
		/// </summary>
		/// <returns>The input definition.</returns>
		public OrchestrationInputDefinition Build()
		{
			if (_parentPath != null)
			{
				throw new InvalidOperationException("Only the root builder can build an input definition.");
			}

			var definition = new OrchestrationInputDefinition();
			definition.Items.AddRange(_items);
			definition.ValidateStructure();

			return definition;
		}

		private OrchestrationInputBuilder AddField<TField>(string name, Action<TField> configure)
			where TField : OrchestrationInputField, new()
		{
			ValidateName(name);

			var field = new TField
			{
				Name = name,
				Path = OrchestrationInputPath.Combine(_parentPath, name),
			};

			configure?.Invoke(field);

			_items.Add(field);

			return this;
		}

		private void ValidateName(string name)
		{
			if (!OrchestrationInputPath.IsValidName(name))
			{
				throw new ArgumentException($"'{name}' is not a valid orchestration input name. It cannot be empty or contain '{OrchestrationInputPath.Separator}'.", nameof(name));
			}

			if (_items.Any(x => String.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
			{
				throw new ArgumentException($"An orchestration input item named '{name}' was already added.", nameof(name));
			}
		}
	}
}
