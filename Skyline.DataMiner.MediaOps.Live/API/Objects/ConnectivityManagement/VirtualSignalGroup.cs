namespace Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.API.Validation;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	public class VirtualSignalGroup : ApiObject<VirtualSignalGroup>
	{
		private readonly VirtualSignalGroupInstance _domInstance;

		private readonly WrappedList<VirtualSignalGroupLevelsSection, LevelEndpoint> _wrappedLevels;
		private readonly WrappedList<Guid, ApiObjectReference<Category>> _wrappedCategories;

		public VirtualSignalGroup() : this(new VirtualSignalGroupInstance())
		{
		}

		public VirtualSignalGroup(Guid id) : this(new VirtualSignalGroupInstance(id))
		{
		}

		internal VirtualSignalGroup(VirtualSignalGroupInstance domInstance) : base(domInstance)
		{
			_domInstance = domInstance ?? throw new ArgumentNullException(nameof(domInstance));

			_wrappedLevels = new WrappedList<VirtualSignalGroupLevelsSection, LevelEndpoint>(
				_domInstance.VirtualSignalGroupLevels,
				x => new LevelEndpoint(x),
				x => x.DomSection);

			_wrappedCategories = new WrappedList<Guid, ApiObjectReference<Category>>(
				_domInstance.VirtualSignalGroupInfo.Categories,
				x => new ApiObjectReference<Category>(x),
				x => x.ID);
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

		public Role Role
		{
			get
			{
				return (Role)(int)_domInstance.VirtualSignalGroupInfo.Role;
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

		public IList<ApiObjectReference<Category>> Categories
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

		public bool IsSource => Role == Role.Source;

		public bool IsDestination => Role == Role.Destination;

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
		/// Unassigns the endpoint from a level in the virtual signal group. If the level does not have an endpoint assigned, nothing happens.
		/// </summary>
		/// <param name="level">The level to unassign the endpoint from.</param>
		/// <exception cref="ArgumentNullException">Thrown when level is null.</exception>
		public void RemoveEndpointFromLevel(ApiObjectReference<Level> level)
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

		public ValidationResult Validate()
		{
			var result = new ValidationResult();

			if (!NameUtil.Validate(Name, out var error))
			{
				result.AddError(error, nameof(Name));
			}

			if (Description != null && Description.Length > 200)
			{
				result.AddError("Description cannot be longer than 200 characters.", nameof(Description));
			}

			return result;
		}
	}

	public static class VirtualSignalGroupExposers
	{
		public static readonly Exposer<VirtualSignalGroup, Guid> ID = new Exposer<VirtualSignalGroup, Guid>(x => x.ID, nameof(VirtualSignalGroup.ID));
		public static readonly Exposer<VirtualSignalGroup, string> Name = new Exposer<VirtualSignalGroup, string>(x => x.Name, nameof(VirtualSignalGroup.Name));
		public static readonly Exposer<VirtualSignalGroup, string> Description = new Exposer<VirtualSignalGroup, string>(x => x.Description, nameof(VirtualSignalGroup.Description));
		public static readonly Exposer<VirtualSignalGroup, Role> Role = new Exposer<VirtualSignalGroup, Role>(x => x.Role, nameof(VirtualSignalGroup.Role));
		public static readonly DynamicListExposer<VirtualSignalGroup, ApiObjectReference<Level>> Level = DynamicListExposer<VirtualSignalGroup, ApiObjectReference<Level>>.CreateFromListExposer(new Exposer<VirtualSignalGroup, IEnumerable>(x => x.Levels.Select(c => c.Level), nameof(LevelEndpoint.Level)));
		public static readonly DynamicListExposer<VirtualSignalGroup, ApiObjectReference<Endpoint>> Endpoint = DynamicListExposer<VirtualSignalGroup, ApiObjectReference<Endpoint>>.CreateFromListExposer(new Exposer<VirtualSignalGroup, IEnumerable>(x => x.Levels.Select(c => c.Endpoint), nameof(LevelEndpoint.Endpoint)));
		public static readonly DynamicListExposer<VirtualSignalGroup, ApiObjectReference<Category>> Categories = DynamicListExposer<VirtualSignalGroup, ApiObjectReference<Category>>.CreateFromListExposer(new Exposer<VirtualSignalGroup, IEnumerable>(x => x.Categories.Select(c => c.ID), nameof(VirtualSignalGroup.Categories)));
	}
}
