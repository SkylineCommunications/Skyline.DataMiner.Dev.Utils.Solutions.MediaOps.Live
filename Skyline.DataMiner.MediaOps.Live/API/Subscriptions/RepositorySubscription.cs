namespace Skyline.DataMiner.MediaOps.Live.API.Subscriptions
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Repositories;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Utils.DOM;

	public sealed class RepositorySubscription<T> : IDisposable
		where T : ApiObject<T>
	{
		private readonly object _lock = new object();

		private readonly Repository<T> _repository;
		private readonly DomWatcher _domWatcher;

		internal RepositorySubscription(Repository<T> repository, FilterElement<DomInstance> domFilter)
		{
			if (domFilter is null)
			{
				throw new ArgumentNullException(nameof(domFilter));
			}

			_repository = repository ?? throw new ArgumentNullException(nameof(repository));

			_domWatcher = new DomWatcher(repository.Helper.ModuleId, domFilter, repository.Connection);
		}

		public Repository<T> Repository => _repository;

		private event EventHandler<ApiObjectsChangedEvent<T>> ChangedInternal;

		public event EventHandler<ApiObjectsChangedEvent<T>> Changed
		{
			add
			{
				lock (_lock)
				{
					var subscribeDomWatcher = ChangedInternal == null;
					ChangedInternal += value;

					if (subscribeDomWatcher)
					{
						_domWatcher.OnChanged += DomWatcher_OnChanged;
					}
				}
			}

			remove
			{
				lock (_lock)
				{
					ChangedInternal -= value;

					if (ChangedInternal == null)
					{
						_domWatcher.OnChanged -= DomWatcher_OnChanged;
					}
				}
			}
		}

		private void DomWatcher_OnChanged(object sender, DomInstancesChangedEventMessage e)
		{
			var eventArgs = new ApiObjectsChangedEvent<T>(
				e.Created?.Select(_repository.CreateInstance),
				e.Updated?.Select(_repository.CreateInstance),
				e.Deleted?.Select(_repository.CreateInstance));

			ChangedInternal?.Invoke(this, eventArgs);
		}

		public void Dispose()
		{
			lock (_lock)
			{
				_domWatcher.OnChanged -= DomWatcher_OnChanged;
				_domWatcher.Dispose();
				ChangedInternal = null;
			}
		}
	}
}
