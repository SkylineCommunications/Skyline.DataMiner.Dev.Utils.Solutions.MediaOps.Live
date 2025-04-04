namespace Skyline.DataMiner.MediaOps.Live.API.Objects.SlcConnectivityManagement
{
	using System;

	using Skyline.DataMiner.MediaOps.Live.DOM.Model.SlcConnectivityManagement;

	public class LevelEndpoint
	{
		public LevelEndpoint()
		{
			DomSection = new VirtualSignalGroupLevelsSection();
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

		internal LevelEndpoint(VirtualSignalGroupLevelsSection domSection)
		{
			DomSection = domSection ?? throw new ArgumentNullException(nameof(domSection));
		}

		internal VirtualSignalGroupLevelsSection DomSection { get; }

		public ApiObjectReference<Level>? Level
		{
			get
			{
				return DomSection.Level;
			}

			set
			{
				DomSection.Level = value;
			}
		}

		public ApiObjectReference<Endpoint>? Endpoint
		{
			get
			{
				return DomSection.Endpoint;
			}

			set
			{
				DomSection.Endpoint = value;
			}
		}
	}
}
