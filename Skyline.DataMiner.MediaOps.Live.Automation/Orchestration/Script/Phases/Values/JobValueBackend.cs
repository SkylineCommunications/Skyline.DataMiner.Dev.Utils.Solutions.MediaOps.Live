namespace Skyline.DataMiner.Solutions.MediaOps.Live.Automation.Orchestration.Script.Phases.Values
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Repositories.Orchestration;

	/// <summary>
	/// The job context values of one node, shared by all events of the job.
	/// </summary>
	internal sealed class JobValueBackend : IValueBackend
	{
		private readonly NodeContextValueRepository _repository;
		private readonly Guid _jobInfoId;
		private readonly string _nodeId;

		internal JobValueBackend(NodeContextValueRepository repository, Guid jobInfoId, string nodeId)
		{
			_repository = repository ?? throw new ArgumentNullException(nameof(repository));
			_jobInfoId = jobInfoId;
			_nodeId = nodeId;
		}

		// Values are read on every access: other events of the same node can change them in the meantime.
		public IReadOnlyDictionary<string, string> ReadAll() => _repository.GetValues(_jobInfoId, _nodeId);

		public bool TryGet(string key, out string value) => _repository.TryGetValue(_jobInfoId, _nodeId, key, out value);

		public void Set(string key, string value) => _repository.SetValue(_jobInfoId, _nodeId, key, value);

		public bool Remove(string key)
		{
			if (!TryGet(key, out _))
			{
				return false;
			}

			_repository.RemoveValue(_jobInfoId, _nodeId, key);
			return true;
		}
	}
}
