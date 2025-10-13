namespace Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement
{
	using System;
	using Skyline.DataMiner.MediaOps.Live.API.Objects;

	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.API.Validation;
	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	public class Level : ApiObject<Level>
	{
		private readonly LevelInstance _domInstance;

		public Level() : this(new LevelInstance())
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
				return (long)_domInstance.LevelInfo.Number;
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

		public ApiObjectReference<TransportType>? TransportType
		{
			get
			{
				return _domInstance.LevelInfo.TransportType;
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

			if (TransportType == null)
			{
				result.AddError($"{nameof(TransportType)} cannot be null.", this, x => x.TransportType);
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
