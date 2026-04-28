namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Exceptions
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;

	[Serializable]
	public class DuplicateLevelNumbersException : Exception
	{
		protected DuplicateLevelNumbersException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}

		public DuplicateLevelNumbersException()
		{
		}

		public DuplicateLevelNumbersException(string message) : base(message)
		{
		}

		public DuplicateLevelNumbersException(string message, Exception innerException) : base(message, innerException)
		{
		}

		public DuplicateLevelNumbersException(string message, IReadOnlyCollection<long> duplicateNumbers)
		: base(message)
		{
			DuplicateNumbers = duplicateNumbers ?? throw new ArgumentNullException(nameof(duplicateNumbers));
		}

		public IReadOnlyCollection<long> DuplicateNumbers { get; }
	}
}