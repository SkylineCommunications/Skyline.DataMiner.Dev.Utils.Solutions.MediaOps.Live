namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Exceptions
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;

	[Serializable]
	public class DuplicateNamesException : Exception
	{
		protected DuplicateNamesException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}

		public DuplicateNamesException()
		{
		}

		public DuplicateNamesException(string message) : base(message)
		{
		}

		public DuplicateNamesException(string message, Exception innerException) : base(message, innerException)
		{
		}

		public DuplicateNamesException(string message, IReadOnlyCollection<string> duplicateNames)
		: base(message)
		{
			DuplicateNames = duplicateNames ?? throw new ArgumentNullException(nameof(duplicateNames));
		}

		public IReadOnlyCollection<string> DuplicateNames { get; }
	}
}