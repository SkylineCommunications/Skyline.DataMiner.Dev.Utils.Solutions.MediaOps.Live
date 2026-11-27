namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases
{
	using System;

	/// <summary>
	/// The wait phase a resumed execution continues at, and the moment that wait started.
	/// </summary>
	internal sealed class ResumePoint
	{
		internal ResumePoint(int phaseIndex, DateTimeOffset waitStarted)
		{
			PhaseIndex = phaseIndex;
			WaitStarted = waitStarted;
		}

		internal int PhaseIndex { get; }

		internal DateTimeOffset WaitStarted { get; }
	}
}
