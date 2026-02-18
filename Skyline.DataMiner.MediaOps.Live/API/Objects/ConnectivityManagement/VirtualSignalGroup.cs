namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Validation;
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Model.SlcConnectivityManagement;

	using Categories = Skyline.DataMiner.Solutions.Categories.API;

	public class VirtualSignalGroup : ApiObject<VirtualSignalGroup>
	{
		private readonly VirtualSignalGroupInstance _domInstance;

		private readonly WrappedList<VirtualSignalGroupLevelSection, LevelEndpoint> _wrappedLevels;
		private readonly WrappedList<Guid, Categories.ApiObjectReference<Categories.Category>> _wrappedCategories;

		public VirtualSignalGroup() : this(new VirtualSignalGroupInstance())
		{
		}

		public VirtualSignalGroup(Guid id) : this(new VirtualSignalGroupInstance(id))
		{
		}

		internal VirtualSignalGroup(VirtualSignalGroupInstance domInstance) : base(domInstance)
		{
			_domInstance = domInstance ?? throw new ArgumentNullException(nameof(domInstance));

			_wrappedLevels = new WrappedList<VirtualSignalGroupLevelSection, LevelEndpoint>(
				_domInstance.VirtualSignalGroupLevel,
				x => new LevelEndpoint(x),
				x => x.DomSection);

			_wrappedCategories = new WrappedList<Guid, Categories.ApiObjectReference<Categories.Category>>(
				_domInstance.VirtualSignalGroupInfo.Categories,
				x => new Categories.ApiObjectReference<Categories.Category>(x),
				x => x.ID);

			// Set default values for required fields if they are not already set
			if (!_domInstance.VirtualSignalGroupInfo.Role.HasValue)
			{
				_domInstance.VirtualSignalGroupInfo.Role = SlcConnectivityManagementIds.Enums.Role.Source;
			}
		}

		internal VirtualSignalGroup(DomInstance domInstance) : this(new VirtualSignalGroupInstance(domInstance))
		{
		}

		internal static DomDefinitionId DomDefinition => SlcConnectivityManagementIds.Definitions.VirtualSignalGroup;

		public string Name
		{
			get
			{
				return _domInstance.VirtualSignalGroupInfo.Name;
			}

			set
			{
				_domInstance.VirtualSignalGroupInfo.Name = value;
			}
		}

		public string Description
		{
			get
			{
				return _domInstance.VirtualSignalGroupInfo.Description;
			}

			set
			{
				_domInstance.VirtualSignalGroupInfo.Description = value;
			}
		}

		public EndpointRole Role
		{
			get
			{
				if (_domInstance.VirtualSignalGroupInfo.Role.HasValue)
				{
					return (EndpointRole)(int)_domInstance.VirtualSignalGroupInfo.Role.Value;
				}

				return default;
			}

			set
			{
				_domInstance.VirtualSignalGroupInfo.Role = (SlcConnectivityManagementIds.Enums.Role)(int)value;
			}
		}

		public IList<LevelEndpoint> Levels
		{
			get
			{
				return _wrappedLevels;
			}

			set
			{
				_wrappedLevels.Clear();
				_wrappedLevels.AddRange(value);
			}
		}

		public IList<Categories.ApiObjectReference<Categories.Category>> Categories
		{
			get
			{
				return _wrappedCategories;
			}

			set
			{
				_wrappedCategories.Clear();
				_wrappedCategories.AddRange(value);
			}
		}

		public bool IsSource => Role == EndpointRole.Source;

		public bool IsDestination => Role == EndpointRole.Destination;

		public IEnumerable<(ApiObjectReference<Level> Level, ApiObjectReference<Endpoint> Endpoint)> GetLevelEndpoints()
		{
			if (Levels == null)
			{
				yield break;
			}

			foreach (var item in Levels)
			{
				if (item.Level == ApiObjectReference<Level>.Empty ||
					item.Endpoint == ApiObjectReference<Endpoint>.Empty)
				{
					continue;
				}

				yield return (item.Level, item.Endpoint);
			}
		}

		public IEnumerable<ApiObjectReference<Level>> GetAssignedLevels()
		{
			if (Levels == null)
			{
				yield break;
			}

			foreach (var item in Levels)
			{
				if (item.Level == ApiObjectReference<Level>.Empty)
				{
					continue;
				}

				yield return item.Level;
			}
		}

		public IEnumerable<ApiObjectReference<Endpoint>> GetAssignedEndpoints()
		{
			if (Levels == null)
			{
				yield break;
			}

			foreach (var item in Levels)
			{
				if (item.Endpoint == ApiObjectReference<Endpoint>.Empty)
				{
					continue;
				}

				yield return item.Endpoint;
			}
		}

		public bool ContainsEndpoint(ApiObjectReference<Endpoint> endpoint)
		{
			if (Levels == null)
			{
				return false;
			}

			return Levels.Any(x => x.Endpoint == endpoint);
		}

		public bool ContainsLevel(ApiObjectReference<Level> level)
		{
			if (Levels == null)
			{
				return false;
			}

			return Levels.Any(x => x.Level == level);
		}

		public bool TryGetEndpointForLevel(ApiObjectReference<Level> level, out ApiObjectReference<Endpoint> endpoint)
		{
			if (Levels != null)
			{
				var levelEndpoint = Levels.FirstOrDefault(x => x.Level == level);

				if (levelEndpoint != null && levelEndpoint.Endpoint != null)
				{
					endpoint = levelEndpoint.Endpoint;
					return true;
				}
			}

			endpoint = null;
			return false;
		}

		public IEnumerable<ApiObjectReference<Level>> GetLevelsWithEndpoint(ApiObjectReference<Endpoint> endpoint)
		{
			return GetLevelEndpoints()
				.Where(x => x.Endpoint == endpoint)
				.Select(x => x.Level);
		}

		/// <summary>
		/// Assigns an endpoint to a level in the virtual signal group. If the level already has an endpoint assigned, it will be replaced.
		/// If the level does not exist in the virtual signal group, it will be added with the specified endpoint.
		/// </summary>
		/// <param name="level">The level to assign the endpoint to.</param>
		/// <param name="endpoint">The endpoint to assign to the level.</param>
		/// <exception cref="ArgumentNullException">Thrown when either level or endpoint is null.</exception>
		public void AssignEndpointToLevel(ApiObjectReference<Level> level, ApiObjectReference<Endpoint> endpoint)
		{
			if (level == null)
			{
				throw new ArgumentNullException(nameof(level));
			}

			if (endpoint == null)
			{
				throw new ArgumentNullException(nameof(endpoint));
			}

			var existing = Levels.FirstOrDefault(x => x.Level == level);
			if (existing != null)
			{
				existing.Endpoint = endpoint;
			}
			else
			{
				Levels.Add(new LevelEndpoint(level, endpoint));
			}
		}

		/// <summary>
		/// Assigns an endpoint to a level in the virtual signal group. If the level already has an endpoint assigned, it will be replaced.
		/// If the level does not exist in the virtual signal group, it will be added with the specified endpoint.
		/// </summary>
		/// <param name="level">The level to assign the endpoint to.</param>
		/// <param name="endpoint">The endpoint to assign to the level.</param>
		/// <exception cref="ArgumentNullException">Thrown when either level or endpoint is null.</exception>
		public void AssignEndpointToLevel(Level level, Endpoint endpoint)
		{
			if (level == null)
			{
				throw new ArgumentNullException(nameof(level));
			}

			if (endpoint == null)
			{
				throw new ArgumentNullException(nameof(endpoint));
			}

			// Check if the endpoint's role matches the virtual signal group's role
			if (endpoint.Role != Role)
			{
				throw new InvalidOperationException($"Endpoint and virtual signal group must have the same role.");
			}

			// Check if the endpoint's transport type matches the level's transport type
			if (endpoint.TransportType != level.TransportType)
			{
				throw new InvalidOperationException($"Endpoint and level must have the same transport type.");
			}

			// Check if the endpoint is already assigned to another level in this virtual signal group
			var existingLevelEndpoint = Levels.FirstOrDefault(x => x.Endpoint == endpoint && x.Level != level);
			if (existingLevelEndpoint != null)
			{
				throw new InvalidOperationException($"Endpoint '{endpoint.Name}' is already assigned to another level in this virtual signal group.");
			}

			var existing = Levels.FirstOrDefault(x => x.Level == level);
			if (existing != null)
			{
				existing.Endpoint = endpoint;
			}
			else
			{
				Levels.Add(new LevelEndpoint(level, endpoint));
			}
		}

		/// <summary>
		/// Unassigns the endpoint from a level in the virtual signal group. If the level does not have an endpoint assigned, nothing happens.
		/// </summary>
		/// <param name="level">The level to unassign the endpoint from.</param>
		/// <exception cref="ArgumentNullException">Thrown when level is null.</exception>
		public void UnassignEndpointFromLevel(ApiObjectReference<Level> level)
		{
			if (level == null)
			{
				throw new ArgumentNullException(nameof(level));
			}

			var existing = Levels.FirstOrDefault(x => x.Level == level);
			if (existing != null)
			{
				Levels.Remove(existing);
			}
		}

		/// <summary>
		/// Unassigns the specified endpoint from all levels in the virtual signal group.
		/// </summary>
		/// <param name="endpoint">The endpoint to unassign.</param>
		public void UnassignEndpoint(ApiObjectReference<Endpoint> endpoint)
		{
			if (endpoint == null)
			{
				throw new ArgumentNullException(nameof(endpoint));
			}

			var toRemove = Levels.Where(x => x.Endpoint == endpoint).ToList();

			foreach (var levelEndpoint in toRemove)
			{
				Levels.Remove(levelEndpoint);
			}
		}

		/// <summary>
		/// Checks if the virtual signal group is assigned to the specified category.
		/// </summary>
		/// <param name="category">The category to check.</param>
		/// <returns>True if the virtual signal group is assigned to the category, false otherwise.</returns>
		public bool IsAssignedToCategory(Categories.ApiObjectReference<Categories.Category> category)
		{
			if (Categories == null)
			{
				return false;
			}

			return Categories.Contains(category);
		}

		/// <summary>
		/// Assigns the virtual signal group to the specified category.
		/// </summary>
		/// <param name="category">The category to assign the virtual signal group to.</param>
		public void AssignToCategory(Categories.ApiObjectReference<Categories.Category> category)
		{
			if (category == null)
			{
				throw new ArgumentNullException(nameof(category));
			}

			if (!IsAssignedToCategory(category))
			{
				Categories.Add(category);
			}
		}

		/// <summary>
		/// Unassigns the virtual signal group from the specified category.
		/// </summary>
		/// <param name="category">The category to unassign the virtual signal group from.</param>
		public void UnassignFromCategory(Categories.ApiObjectReference<Categories.Category> category)
		{
			if (category == null)
			{
				throw new ArgumentNullException(nameof(category));
			}

			if (IsAssignedToCategory(category))
			{
				Categories.Remove(category);
			}
		}

		public ValidationResult Validate()
		{
			var result = new ValidationResult();

			if (!NameUtil.Validate(Name, out var error))
			{
				result.AddError(error, this, x => x.Name);
			}

			if (Description != null && Description.Length > 200)
			{
				result.AddError("Description cannot be longer than 200 characters.", this, x => x.Description);
			}

			// Error when multiple endpoints are assigned to the same level
			var duplicateLevels = Levels
				.GroupBy(x => x.Level)
				.Where(g => g.Count() > 1)
				.Select(g => g.Key);

			foreach (var level in duplicateLevels)
			{
				result.AddError($"Multiple endpoints are assigned to level with ID '{level.ID}'.", this, x => x.Levels);
			}

			return result;
		}
	}

	public static class VirtualSignalGroupExposers
	{
		public static readonly Exposer<VirtualSignalGroup, Guid> ID = new Exposer<VirtualSignalGroup, Guid>(x => x.ID, nameof(VirtualSignalGroup.ID));
		public static readonly Exposer<VirtualSignalGroup, string> Name = new Exposer<VirtualSignalGroup, string>(x => x.Name, nameof(VirtualSignalGroup.Name));
		public static readonly Exposer<VirtualSignalGroup, string> Description = new Exposer<VirtualSignalGroup, string>(x => x.Description, nameof(VirtualSignalGroup.Description));
		public static readonly Exposer<VirtualSignalGroup, EndpointRole> Role = new Exposer<VirtualSignalGroup, EndpointRole>(x => x.Role, nameof(VirtualSignalGroup.Role));
		public static readonly DynamicListExposer<VirtualSignalGroup, ApiObjectReference<Level>> Level = DynamicListExposer<VirtualSignalGroup, ApiObjectReference<Level>>.CreateFromListExposer(new Exposer<VirtualSignalGroup, IEnumerable>(x => x.Levels.Select(c => c.Level), nameof(LevelEndpoint.Level)));
		public static readonly DynamicListExposer<VirtualSignalGroup, ApiObjectReference<Endpoint>> Endpoint = DynamicListExposer<VirtualSignalGroup, ApiObjectReference<Endpoint>>.CreateFromListExposer(new Exposer<VirtualSignalGroup, IEnumerable>(x => x.Levels.Select(c => c.Endpoint), nameof(LevelEndpoint.Endpoint)));
		public static readonly DynamicListExposer<VirtualSignalGroup, Categories.ApiObjectReference<Categories.Category>> Categories = DynamicListExposer<VirtualSignalGroup, Categories.ApiObjectReference<Categories.Category>>.CreateFromListExposer(new Exposer<VirtualSignalGroup, IEnumerable>(x => x.Categories.Select(c => c.ID), nameof(VirtualSignalGroup.Categories)));
	}
}
