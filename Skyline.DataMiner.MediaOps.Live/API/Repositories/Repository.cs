namespace Skyline.DataMiner.MediaOps.Live.API.Repositories
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Querying;
	using Skyline.DataMiner.MediaOps.Live.API.Subscriptions;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.DOM.Tools;
	using Skyline.DataMiner.MediaOps.Live.Extensions;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Utils.DOM.Extensions;

	using SLDataGateway.API.Querying;
	using SLDataGateway.API.Types.Querying;

	public abstract class Repository<T> where T : ApiObject<T>
	{
		private const int _defaultPageSize = 500;

		private readonly FilterElement<DomInstance> _domDefinitionFilter;
		private readonly ApiRepositoryQueryProvider<T> _queryProvider;

		protected Repository(MediaOpsLiveApi api, DomHelper helper)
		{
			Api = api ?? throw new ArgumentNullException(nameof(api));
			Helper = helper ?? throw new ArgumentNullException(nameof(helper));

			_domDefinitionFilter = DomInstanceExposers.DomDefinitionId.Equal(DomDefinition.Id);
			_queryProvider = new ApiRepositoryQueryProvider<T>(this);
		}

		protected internal MediaOpsLiveApi Api { get; }

		protected internal DomHelper Helper { get; }

		protected internal IConnection Connection => Api.Connection;

		protected internal abstract DomDefinitionId DomDefinition { get; }

		protected internal abstract T CreateInstance(DomInstance domInstance);

		protected abstract void ValidateBeforeSave(ICollection<T> instances);

		protected abstract void ValidateBeforeDelete(ICollection<T> instances);

		public virtual T Create(T instance)
		{
			if (instance == null)
			{
				throw new ArgumentNullException(nameof(instance));
			}

			ValidateBeforeSave(new[] { instance });

			var newInstance = Helper.DomInstances.Create(instance.DomInstance);
			return CreateInstance(newInstance);
		}

		public virtual T Update(T instance)
		{
			if (instance == null)
			{
				throw new ArgumentNullException(nameof(instance));
			}

			ValidateBeforeSave(new[] { instance });

			var newInstance = Helper.DomInstances.Update(instance.DomInstance);
			return CreateInstance(newInstance);
		}

		public virtual IEnumerable<T> CreateOrUpdate(IEnumerable<T> instances)
		{
			if (instances == null)
			{
				throw new ArgumentNullException(nameof(instances));
			}

			var instanceCollection = instances as ICollection<T> ?? instances.ToList();

			if (instanceCollection.Count == 0)
			{
				// Nothing to create or update.
				return instanceCollection;
			}

			ValidateBeforeSave(instanceCollection);

			var domInstances = instanceCollection.Select(x => x.DomInstance.ToInstance());
			var result = Helper.DomInstances.CreateOrUpdateInBatches(domInstances);

			result.ThrowOnFailure();

			return result.SuccessfulItems.Select(CreateInstance);
		}

		public virtual T CreateOrUpdate(T instance)
		{
			if (instance == null)
			{
				throw new ArgumentNullException(nameof(instance));
			}

			return CreateOrUpdate(new[] { instance }).Single();
		}

		public virtual void Delete(IEnumerable<T> instances)
		{
			if (instances == null)
			{
				throw new ArgumentNullException(nameof(instances));
			}

			var instanceCollection = instances.AsCollection();
			if (instanceCollection.Count == 0)
			{
				// Nothing to delete.
				return;
			}

			ValidateBeforeDelete(instanceCollection);

			var domInstances = instanceCollection.Select(x => x.DomInstance.ToInstance());
			Helper.DomInstances.DeleteInBatches(domInstances).ThrowOnFailure();
		}

		public virtual void Delete(T instance)
		{
			if (instance == null)
			{
				throw new ArgumentNullException(nameof(instance));
			}

			Delete(new[] { instance });
		}

		public virtual long CountAll()
		{
			return Count(_domDefinitionFilter);
		}

		public virtual long Count(FilterElement<T> filter)
		{
			if (filter is null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			var domFilter = TranslateFullFilter(filter);
			domFilter = AddDomDefinitionFilter(domFilter);

			return Count(domFilter);
		}

		internal long Count(FilterElement<DomInstance> domFilter)
		{
			if (domFilter is null)
			{
				throw new ArgumentNullException(nameof(domFilter));
			}

			domFilter = AddDomDefinitionFilter(domFilter);

			return Helper.DomInstances.Count(domFilter);
		}

		public virtual IEnumerable<T> ReadAll()
		{
			return Read(_domDefinitionFilter);
		}

		public virtual IEnumerable<IEnumerable<T>> ReadAllPaged(long pageSize = _defaultPageSize)
		{
			return ReadPaged(_domDefinitionFilter, pageSize);
		}

		public virtual T Read(Guid id)
		{
			if (id == Guid.Empty)
			{
				return null;
			}

			var filter = _domDefinitionFilter
				.AND(DomInstanceExposers.Id.Equal(id));

			return Read(filter).SingleOrDefault();
		}

		public virtual IDictionary<Guid, T> Read(IEnumerable<Guid> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			var idsList = ids
				.Where(x => x != Guid.Empty)
				.Distinct()
				.ToList();

			if (idsList.Count == 0)
			{
				return new Dictionary<Guid, T>();
			}

			FilterElement<DomInstance> CreateFilter(Guid id) =>
				_domDefinitionFilter
				.AND(DomInstanceExposers.Id.Equal(id));

			return FilterQueryExecutor.RetrieveFilteredItems(
					idsList,
					x => CreateFilter(x),
					x => Read(x))
				.SafeToDictionary(x => x.ID);
		}

		public virtual IDictionary<ApiObjectReference<T>, T> Read(IEnumerable<ApiObjectReference<T>> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			var idsList = ids
				.Where(x => x != ApiObjectReference<T>.Empty)
				.Distinct()
				.ToList();

			if (idsList.Count == 0)
			{
				return new Dictionary<ApiObjectReference<T>, T>();
			}

			FilterElement<DomInstance> CreateFilter(Guid id) =>
				_domDefinitionFilter
				.AND(DomInstanceExposers.Id.Equal(id));

			return FilterQueryExecutor.RetrieveFilteredItems(
					idsList,
					x => CreateFilter(x),
					x => Read(x))
				.SafeToDictionary(x => new ApiObjectReference<T>(x.ID));
		}

		public virtual T Read(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException(nameof(name));
			}

			var filter = _domDefinitionFilter
				.AND(DomInstanceExposers.Name.Equal(name));

			return Read(filter).SingleOrDefault();
		}

		public virtual IDictionary<string, T> Read(IEnumerable<string> names)
		{
			if (names == null)
			{
				throw new ArgumentNullException(nameof(names));
			}

			FilterElement<DomInstance> CreateFilter(string name) =>
				_domDefinitionFilter
				.AND(DomInstanceExposers.Name.Equal(name));

			return FilterQueryExecutor.RetrieveFilteredItems(
					names,
					x => CreateFilter(x),
					x => Read(x))
				.SafeToDictionary(x => x.DomInstance.Name, x => x);
		}

		public virtual IEnumerable<T> Read(FilterElement<T> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			var domFilter = TranslateFullFilter(filter);

			return Read(domFilter);
		}

		internal IEnumerable<T> Read(FilterElement<DomInstance> domFilter)
		{
			if (domFilter == null)
			{
				throw new ArgumentNullException(nameof(domFilter));
			}

			domFilter = AddDomDefinitionFilter(domFilter);

			var domInstances = Helper.DomInstances.Read(domFilter);

			return domInstances.Select(CreateInstance);
		}

		public virtual IEnumerable<T> Read(IQuery<T> query)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			var domFilter = TranslateFullFilter(query.Filter);
			var domOrder = TranslateFullOrderBy(query.Order);

			var domQuery = query
				.WithFilter(domFilter)
				.WithOrder(domOrder);

			return Read(domQuery);
		}

		internal IEnumerable<T> Read(IQuery<DomInstance> domQuery)
		{
			if (domQuery == null)
			{
				throw new ArgumentNullException(nameof(domQuery));
			}

			var domFilter = AddDomDefinitionFilter(domQuery.Filter);

			domQuery = domQuery.WithFilter(domFilter);

			var domInstances = Helper.DomInstances.Read(domQuery);

			return domInstances.Select(CreateInstance);
		}

		public virtual IEnumerable<IEnumerable<T>> ReadPaged(FilterElement<T> filter, long pageSize = _defaultPageSize)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			var domFilter = TranslateFullFilter(filter);

			return ReadPaged(domFilter, pageSize);
		}

		internal IEnumerable<IEnumerable<T>> ReadPaged(FilterElement<DomInstance> domFilter, long pageSize = _defaultPageSize)
		{
			if (domFilter == null)
			{
				throw new ArgumentNullException(nameof(domFilter));
			}

			domFilter = AddDomDefinitionFilter(domFilter);

			var domInstances = Helper.DomInstances.ReadPaged(domFilter, pageSize);

			return domInstances.Select(x => x.Select(CreateInstance));
		}

		public virtual IEnumerable<IEnumerable<T>> ReadPaged(IQuery<T> query, long pageSize = _defaultPageSize)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			var domFilter = TranslateFullFilter(query.Filter);
			var domOrder = TranslateFullOrderBy(query.Order);

			var domQuery = query
				.WithFilter(domFilter)
				.WithOrder(domOrder);

			return ReadPaged(domQuery, pageSize);
		}

		internal IEnumerable<IEnumerable<T>> ReadPaged(IQuery<DomInstance> domQuery, long pageSize = _defaultPageSize)
		{
			if (domQuery == null)
			{
				throw new ArgumentNullException(nameof(domQuery));
			}

			var domFilter = AddDomDefinitionFilter(domQuery.Filter);

			domQuery = domQuery.WithFilter(domFilter);

			var domInstances = Helper.DomInstances.ReadPaged(domQuery, pageSize);

			return domInstances.Select(x => x.Select(CreateInstance));
		}

		public virtual IQueryable<T> Query()
		{
			return new ApiRepositoryQuery<T>(_queryProvider);
		}

		/// <summary>
		/// Subscribes to all items in the repository.
		/// </summary>
		/// <returns>
		/// A <see cref="RepositorySubscription{T}"/> representing the subscription.
		/// </returns>
		/// <remarks>
		/// <b>Warning:</b> The returned <see cref="RepositorySubscription{T}"/> must be disposed when no longer needed
		/// to avoid resource leaks.
		/// </remarks>
		public RepositorySubscription<T> Subscribe()
		{
			var filter = new TRUEFilterElement<T>();

			return Subscribe(filter);
		}

		/// <summary>
		/// Subscribes to the repository using the specified filter.
		/// </summary>
		/// <param name="filter">
		/// The <see cref="FilterElement{T}"/> used to determine which items to include in the subscription.
		/// </param>
		/// <returns>
		/// A <see cref="RepositorySubscription{T}"/> representing the subscription with the specified filter.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// Thrown if the <paramref name="filter"/> is <c>null</c>.
		/// </exception>
		/// <remarks>
		/// <b>Warning:</b> The returned <see cref="RepositorySubscription{T}"/> must be disposed when no longer needed
		/// to avoid resource leaks.
		/// </remarks>
		public RepositorySubscription<T> Subscribe(FilterElement<T> filter)
		{
			if (filter is null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			var domFilter = TranslateFullFilter(filter);
			domFilter = AddDomDefinitionFilter(domFilter);

			return new RepositorySubscription<T>(this, domFilter);
		}

		protected internal virtual void CreateWithoutValidation(T instance)
		{
			if (instance == null)
			{
				throw new ArgumentNullException(nameof(instance));
			}

			Helper.DomInstances.Create(instance.DomInstance);
		}

		protected internal virtual void UpdateWithoutValidation(T instance)
		{
			if (instance == null)
			{
				throw new ArgumentNullException(nameof(instance));
			}

			Helper.DomInstances.Update(instance.DomInstance);
		}

		protected internal virtual void CreateOrUpdateWithoutValidation(IEnumerable<T> instances)
		{
			if (instances == null)
			{
				throw new ArgumentNullException(nameof(instances));
			}

			var domInstances = instances.Select(x => x.DomInstance.ToInstance());
			Helper.DomInstances.CreateOrUpdateInBatches(domInstances).ThrowOnFailure();
		}

		protected internal virtual void DeleteWithoutValidation(T instance)
		{
			if (instance == null)
			{
				throw new ArgumentNullException(nameof(instance));
			}

			Helper.DomInstances.Delete(instance.DomInstance);
		}

		protected internal virtual void DeleteWithoutValidation(IEnumerable<T> instances)
		{
			if (instances == null)
			{
				throw new ArgumentNullException(nameof(instances));
			}

			var domInstances = instances.Select(x => x.DomInstance.ToInstance());
			Helper.DomInstances.DeleteInBatches(domInstances).ThrowOnFailure();
		}

		protected internal virtual FilterElement<DomInstance> CreateFilter(string fieldName, Comparer comparer, object value)
		{
			switch (fieldName)
			{
				case nameof(ApiObject<T>.ID):
					return FilterElementFactory.Create<Guid>(DomInstanceExposers.Id, comparer, value);
				default:
					throw new NotImplementedException($"Creating a filter for field '{fieldName}' is not implemented.");
			}
		}

		protected internal virtual IOrderByElement CreateOrderBy(string fieldName, SortOrder sortOrder, bool naturalSort = false)
		{
			switch (fieldName)
			{
				case nameof(ApiObject<T>.ID):
					return OrderByElementFactory.Create(DomInstanceExposers.Id, sortOrder, naturalSort);
				default:
					throw new NotImplementedException($"Creating an order by for field '{fieldName}' is not implemented.");
			}
		}

		protected virtual FilterElement<DomInstance> TranslateFullFilter(FilterElement<T> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			FilterElement<DomInstance> translated;

			if (filter is ANDFilterElement<T> and)
			{
				translated = new ANDFilterElement<DomInstance>(and.subFilters.Select(TranslateFullFilter).ToArray());
			}
			else if (filter is ORFilterElement<T> or)
			{
				translated = new ORFilterElement<DomInstance>(or.subFilters.Select(TranslateFullFilter).ToArray());
			}
			else if (filter is NOTFilterElement<T> not)
			{
				translated = new NOTFilterElement<DomInstance>(TranslateFullFilter(not));
			}
			else if (filter is TRUEFilterElement<T>)
			{
				translated = new TRUEFilterElement<DomInstance>();
			}
			else if (filter is FALSEFilterElement<T>)
			{
				translated = new FALSEFilterElement<DomInstance>();
			}
			else if (filter is ManagedFilterIdentifier managedFilter)
			{
				translated = TranslateFilter(managedFilter);
			}
			else
			{
				throw new NotSupportedException($"Unsupported filter: {filter}");
			}

			return translated;
		}

		protected virtual IOrderBy TranslateFullOrderBy(IOrderBy order)
		{
			if (order == null)
			{
				throw new ArgumentNullException(nameof(order));
			}

			var translatedElements = new List<IOrderByElement>();

			foreach (var orderByElement in order.Elements)
			{
				var translated = TranslateOrderBy(orderByElement);
				translatedElements.Add(translated);
			}

			return new OrderBy(translatedElements);
		}

		protected virtual FilterElement<DomInstance> TranslateFilter(ManagedFilterIdentifier managedFilter)
		{
			if (managedFilter == null)
			{
				throw new ArgumentNullException(nameof(managedFilter));
			}

			var fieldName = managedFilter.getFieldName().fieldName;
			var comparer = managedFilter.getComparer();
			var value = managedFilter.getValue();

			var translated = CreateFilter(fieldName, comparer, value);

			return translated;
		}

		protected virtual IOrderByElement TranslateOrderBy(IOrderByElement orderByElement)
		{
			if (orderByElement == null)
			{
				throw new ArgumentNullException(nameof(orderByElement));
			}

			var fieldName = orderByElement.Exposer.fieldName;
			var sortOrder = orderByElement.SortOrder;
			var naturalSort = orderByElement.Options.NaturalSort;

			var translated = CreateOrderBy(fieldName, sortOrder, naturalSort);

			return translated;
		}

		private FilterElement<DomInstance> AddDomDefinitionFilter(FilterElement<DomInstance> domFilter)
		{
			if (domFilter == _domDefinitionFilter)
			{
				return domFilter;
			}

			if (domFilter is TRUEFilterElement<DomInstance>)
			{
				return _domDefinitionFilter;
			}

			if (domFilter is ANDFilterElement<DomInstance> andFilter)
			{
				return !andFilter.subFilters.Contains(_domDefinitionFilter)
					? andFilter.AND(_domDefinitionFilter)
					: domFilter;
			}

			return new ANDFilterElement<DomInstance>(_domDefinitionFilter, domFilter);
		}
	}
}
