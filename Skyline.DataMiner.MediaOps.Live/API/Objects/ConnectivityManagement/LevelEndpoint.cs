namespace Skyline.DataMiner.MediaOps.Live.API.Objects.ConnectivityManagement
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;

	public sealed class LevelEndpoint : IEquatable<LevelEndpoint>
	{
		public LevelEndpoint()
		{
			DomSection = new VirtualSignalGroupLevelSection();
		}

		public LevelEndpoint(Level level, Endpoint endpoint) : this()
		{
			Level = level ?? throw new ArgumentNullException(nameof(level));
			Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
		}

		public LevelEndpoint(ApiObjectReference<Level> level, ApiObjectReference<Endpoint> endpoint) : this()
		{
			Level = level;
			Endpoint = endpoint;
		}

		internal LevelEndpoint(VirtualSignalGroupLevelSection domSection)
		{
			DomSection = domSection ?? throw new ArgumentNullException(nameof(domSection));
		}

		internal VirtualSignalGroupLevelSection DomSection { get; }

		public ApiObjectReference<Level> Level
		{
			get
			{
				return DomSection.Level ?? Guid.Empty;
			}

			set
			{
				DomSection.Level = value;
			}
		}

		public ApiObjectReference<Endpoint> Endpoint
		{
			get
			{
				return DomSection.Endpoint ?? Guid.Empty;
			}

			set
			{
				DomSection.Endpoint = value;
			}
		}

		public void Validate()
		{
			if (Level == null || Level == ApiObjectReference<Level>.Empty)
			{
				throw new InvalidOperationException($"{nameof(Level)} cannot be null.");
			}

			if (Endpoint == null || Endpoint == ApiObjectReference<Endpoint>.Empty)
			{
				throw new InvalidOperationException($"{nameof(Endpoint)} cannot be null.");
			}
		}

		public override string ToString()
		{
			return $"{Level} - {Endpoint}";
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as LevelEndpoint);
		}

		public bool Equals(LevelEndpoint other)
		{
			return other is not null &&
				   EqualityComparer<VirtualSignalGroupLevelSection>.Default.Equals(DomSection, other.DomSection);
		}

		public override int GetHashCode()
		{
			return EqualityComparer<VirtualSignalGroupLevelSection>.Default.GetHashCode(DomSection);
		}

		public static bool operator ==(LevelEndpoint left, LevelEndpoint right)
		{
			return EqualityComparer<LevelEndpoint>.Default.Equals(left, right);
		}

		public static bool operator !=(LevelEndpoint left, LevelEndpoint right)
		{
			return !(left == right);
		}
	}
}
