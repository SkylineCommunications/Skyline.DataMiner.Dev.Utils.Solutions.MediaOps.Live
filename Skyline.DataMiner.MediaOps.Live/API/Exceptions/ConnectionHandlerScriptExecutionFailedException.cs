namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Exceptions
{
	using System;
	using System.Runtime.Serialization;

	[Serializable]
	public class ConnectionHandlerScriptExecutionFailedException : Exception
	{
		protected ConnectionHandlerScriptExecutionFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}

		public ConnectionHandlerScriptExecutionFailedException()
		{
		}

		public ConnectionHandlerScriptExecutionFailedException(string message) : base(message)
		{
		}

		public ConnectionHandlerScriptExecutionFailedException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
