namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Exceptions
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement;

	[Serializable]
	public class LevelInUseException : Exception
	{
		protected LevelInUseException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}

		public LevelInUseException()
		{
		}

		public LevelInUseException(string message) : base(message)
		{
		}

		public LevelInUseException(string message, Exception innerException) : base(message, innerException)
		{
		}

		public LevelInUseException(string message, IReadOnlyCollection<Level> levels, IReadOnlyCollection<VirtualSignalGroup> referencingVirtualSignalGroups)
			: base(message)
		{
			Levels = levels ?? throw new ArgumentNullException(nameof(levels));
			ReferencingVirtualSignalGroups = referencingVirtualSignalGroups ?? throw new ArgumentNullException(nameof(referencingVirtualSignalGroups));
		}

		public IReadOnlyCollection<Level> Levels { get; }

		public IReadOnlyCollection<VirtualSignalGroup> ReferencingVirtualSignalGroups { get; }
	}
}
