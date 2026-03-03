namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement
{
	using System;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Validation;
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Model.SlcConnectivityManagement;

	public class Level : ApiObject<Level>
	{
		private readonly LevelInstance _domInstance;

		public Level() : this(new LevelInstance())
		{
		}

		public Level(Guid id) : this(new LevelInstance(id))
		{
		}

		internal Level(LevelInstance domInstance) : base(domInstance)
		{
			_domInstance = domInstance ?? throw new ArgumentNullException(nameof(domInstance));
		}

		internal Level(DomInstance domInstance) : this(new LevelInstance(domInstance))
		{
		}

		internal static DomDefinitionId DomDefinition => SlcConnectivityManagementIds.Definitions.Level;

		public long Number
		{
			get
			{
				if (_domInstance.LevelInfo.Number.HasValue)
				{
					return _domInstance.LevelInfo.Number.Value;
				}

				return default;
			}

			set
			{
				_domInstance.LevelInfo.Number = value;
			}
		}

		public string Name
		{
			get
			{
				return _domInstance.LevelInfo.Name;
			}

			set
			{
				_domInstance.LevelInfo.Name = value;
			}
		}

		public ApiObjectReference<TransportType> TransportType
		{
			get
			{
				if (_domInstance.LevelInfo.TransportType.HasValue)
				{
					return _domInstance.LevelInfo.TransportType.Value;
				}

				return ApiObjectReference<TransportType>.Empty;
			}

			set
			{
				_domInstance.LevelInfo.TransportType = value;
			}
		}

		public ValidationResult Validate()
		{
			var result = new ValidationResult();

			if (!NameUtil.Validate(Name, out var error))
			{
				result.AddError(error, this, x => x.Name);
			}

			if (Number < 0)
			{
				result.AddError($"{nameof(Number)} cannot be negative.", this, x => x.Number);
			}

			if (TransportType == ApiObjectReference<TransportType>.Empty)
			{
				result.AddError($"Transport type is mandatory.", this, x => x.TransportType);
			}

			return result;
		}
	}

	public static class LevelExposers
	{
		public static readonly Exposer<Level, Guid> ID = new Exposer<Level, Guid>(x => x.ID, nameof(Level.ID));
		public static readonly Exposer<Level, long> Number = new Exposer<Level, long>(x => x.Number, nameof(Level.Number));
		public static readonly Exposer<Level, string> Name = new Exposer<Level, string>(x => x.Name, nameof(Level.Name));
		public static readonly Exposer<Level, ApiObjectReference<TransportType>?> TransportType = new Exposer<Level, ApiObjectReference<TransportType>?>(x => x.TransportType, nameof(Level.TransportType));
	}
}
