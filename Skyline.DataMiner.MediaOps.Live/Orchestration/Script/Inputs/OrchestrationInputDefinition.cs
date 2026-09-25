namespace Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Newtonsoft.Json;

	/// <summary>
	/// The orchestration input items an orchestration script requires, evaluated for a specific set of already provided values.
	/// </summary>
	public class OrchestrationInputDefinition
	{
		/// <summary>
		/// The version of the input definition contract that is produced by this library.
		/// </summary>
		public const int CurrentVersion = 1;

		/// <summary>
		/// The maximum number of nesting levels, where the top level items are the first level.
		/// </summary>
		public const int MaxDepth = 5;

		// A default value is only known after a first evaluation, while it can itself determine the structure.
		private const int MaxEvaluations = 5;

		/// <summary>
		/// Gets or sets the version of the input definition contract.
		/// </summary>
		[JsonProperty("version")]
		public int Version { get; set; } = CurrentVersion;

		/// <summary>
		/// Gets the top level items of the definition.
		/// </summary>
		[JsonProperty("items")]
		public List<OrchestrationInputItem> Items { get; } = new List<OrchestrationInputItem>();

		/// <summary>
		/// Evaluates an input definition for the specified values, repeating the evaluation until the effective values no longer change.
		/// Repeating is required because default values only become known after an evaluation, while they can determine which items are
		/// relevant or how often a group is repeated.
		/// </summary>
		/// <param name="evaluator">Produces the input definition for a set of values.</param>
		/// <param name="providedValues">The values that were already provided.</param>
		/// <returns>The settled input definition, or <see langword="null"/> when the evaluator returned none.</returns>
		public static OrchestrationInputDefinition Evaluate(Func<OrchestrationInputValues, OrchestrationInputDefinition> evaluator, OrchestrationInputValues providedValues)
		{
			if (evaluator == null)
			{
				throw new ArgumentNullException(nameof(evaluator));
			}

			var values = providedValues ?? OrchestrationInputValues.Empty;
			var evaluationValues = values;
			OrchestrationInputDefinition definition = null;

			for (var iteration = 0; iteration < MaxEvaluations; iteration++)
			{
				definition = evaluator(evaluationValues);

				if (definition == null)
				{
					return null;
				}

				definition.ValidateStructure();

				// Only the values that were really provided are applied, so a default never turns into an explicit value.
				definition.ApplyValues(values);

				var effectiveValues = definition.GetValues();

				if (effectiveValues.HasSameValues(evaluationValues))
				{
					break;
				}

				evaluationValues = effectiveValues;
			}

			return definition;
		}

		/// <summary>
		/// Gets every field of the definition, including the fields of nested groups.
		/// </summary>
		/// <returns>All fields in document order.</returns>
		public IEnumerable<OrchestrationInputField> GetAllFields()
		{
			return Flatten(Items).OfType<OrchestrationInputField>();
		}

		/// <summary>
		/// Gets every group of the definition, including nested groups.
		/// </summary>
		/// <returns>All groups in document order.</returns>
		public IEnumerable<OrchestrationInputGroup> GetAllGroups()
		{
			return Flatten(Items).OfType<OrchestrationInputGroup>();
		}

		/// <summary>
		/// Attempts to find the field with the specified path.
		/// </summary>
		/// <param name="path">The path of the field.</param>
		/// <param name="field">When this method returns <see langword="true"/>, contains the field.</param>
		/// <returns><see langword="true"/> when a field was found; otherwise, <see langword="false"/>.</returns>
		public bool TryGetField(string path, out OrchestrationInputField field)
		{
			field = GetAllFields().FirstOrDefault(x => String.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase));

			return field != null;
		}

		/// <summary>
		/// Attempts to find the group with the specified path.
		/// </summary>
		/// <param name="path">The path of the group.</param>
		/// <param name="group">When this method returns <see langword="true"/>, contains the group.</param>
		/// <returns><see langword="true"/> when a group was found; otherwise, <see langword="false"/>.</returns>
		public bool TryGetGroup(string path, out OrchestrationInputGroup group)
		{
			group = GetAllGroups().FirstOrDefault(x => String.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase));

			return group != null;
		}

		/// <summary>
		/// Copies the provided values onto the matching fields of this definition.
		/// Values that no longer match a field are ignored, as the definition is the single source of truth.
		/// </summary>
		/// <param name="values">The values to apply.</param>
		public void ApplyValues(OrchestrationInputValues values)
		{
			if (values == null)
			{
				return;
			}

			foreach (var field in GetAllFields())
			{
				if (values.TryGetValue(field.Path, out var value))
				{
					field.Value = value;
				}
			}
		}

		/// <summary>
		/// Gets the values that are currently held by the fields of this definition.
		/// </summary>
		/// <returns>The values, keyed by field path.</returns>
		public OrchestrationInputValues GetValues()
		{
			var values = new Dictionary<string, OrchestrationInputValue>(StringComparer.OrdinalIgnoreCase);

			foreach (var field in GetAllFields())
			{
				var value = field.GetEffectiveValue();

				if (value != null)
				{
					values[field.Path] = value;
				}
			}

			return new OrchestrationInputValues(values);
		}

		/// <summary>
		/// Verifies that the structure of this definition is valid: names are unique within their parent, paths match the names,
		/// items are nested at most <see cref="MaxDepth"/> levels deep without cycles, and every field has a usable definition.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown when the definition is not structurally valid.</exception>
		public void ValidateStructure()
		{
			ValidateItems(Items, null, 1, new HashSet<OrchestrationInputItem>());
		}

		/// <summary>
		/// Verifies that every field holds an acceptable value, including the validity the script reported for it.
		/// </summary>
		/// <param name="errors">The problems that were found.</param>
		/// <returns><see langword="true"/> when every field holds an acceptable value; otherwise, <see langword="false"/>.</returns>
		public bool TryValidateValues(out IReadOnlyCollection<string> errors)
		{
			var problems = new List<string>();

			foreach (var field in GetAllFields())
			{
				if (!field.TryValidate(out var error))
				{
					problems.Add(error);
				}
			}

			errors = problems;
			return problems.Count == 0;
		}

		private static IEnumerable<OrchestrationInputItem> Flatten(IEnumerable<OrchestrationInputItem> items)
		{
			foreach (var item in items)
			{
				yield return item;

				if (item is OrchestrationInputGroup group)
				{
					foreach (var child in Flatten(group.Children))
					{
						yield return child;
					}
				}
			}
		}

		private static void ValidateItems(IEnumerable<OrchestrationInputItem> items, string parentPath, int depth, HashSet<OrchestrationInputItem> visited)
		{
			var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			foreach (var item in items)
			{
				if (item == null)
				{
					throw new InvalidOperationException($"'{parentPath ?? "The root"}' contains an empty orchestration input item.");
				}

				if (depth > MaxDepth)
				{
					throw new InvalidOperationException($"Orchestration input '{parentPath}' nests items deeper than the maximum of {MaxDepth} levels.");
				}

				// Items don't override equality, so this tracks instances. A repeated instance would otherwise recurse forever.
				if (!visited.Add(item))
				{
					throw new InvalidOperationException($"Orchestration input item '{item.Name}' within '{parentPath ?? "the root"}' is added more than once, which creates a cycle.");
				}

				if (!OrchestrationInputPath.IsValidName(item.Name))
				{
					throw new InvalidOperationException($"'{item.Name}' is not a valid orchestration input name. It cannot be empty or contain '{OrchestrationInputPath.Separator}'.");
				}

				if (!seenNames.Add(item.Name))
				{
					throw new InvalidOperationException($"Duplicate orchestration input name '{item.Name}' within '{parentPath ?? "the root"}'.");
				}

				var expectedPath = OrchestrationInputPath.Combine(parentPath, item.Name);

				if (!String.Equals(item.Path, expectedPath, StringComparison.Ordinal))
				{
					throw new InvalidOperationException($"The path of orchestration input item '{item.Name}' is '{item.Path}' but '{expectedPath}' was expected.");
				}

				if (item is OrchestrationInputGroup group)
				{
					ValidateItems(group.Children, expectedPath, depth + 1, visited);
				}
				else if (item is OrchestrationInputField field)
				{
					field.ValidateDefinition();
				}
			}
		}
	}
}
