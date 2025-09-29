namespace CheckMediaOpsLive
{
	using System;

	public class ConnectionHandlerScriptStatistics
	{
		public string ScriptName { get; set; }

		public long Executions { get; set; }

		public long FailedExecutions { get; set; }

		public DateTimeOffset LastFailedExecution { get; set; }
	}
}
