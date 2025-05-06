namespace Skyline.DataMiner.MediaOps.Live.API.Objects
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Enums;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
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

		public Enums.Role Role
		{
			get
			{
				return (Enums.Role)(int)_domInstance.VirtualSignalGroupInfo.Role;
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

		public IEnumerable<ApiObjectReference<Endpoint>> GetEndpoints()
		{
			if (Levels == null)
			{
				yield break;
			}

			foreach (var level in Levels)
			{
				if (level.Endpoint == null)
				{
					continue;
				}

				yield return level.Endpoint.Value;
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
					endpoint = levelEndpoint.Endpoint.Value;
					return true;
				}
			}

			endpoint = null;
			return false;
		}

		public void Validate()
		{
			if (String.IsNullOrWhiteSpace(Name))
			{
				throw new InvalidOperationException($"{nameof(Name)} cannot be null, empty, or whitespace.");
			}
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
