namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Exceptions
{
	using System;

	[Serializable]
	public class ConnectionHandlerScriptExecutionFailedException : Exception
	{
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
