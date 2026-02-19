namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Repositories
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Querying;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Subscriptions;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.Solutions.MediaOps.Live.DOM.Tools;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Extensions;
	using Skyline.DataMiner.Utils.DOM.Extensions;
	using SLDataGateway.API.Querying;
	using SLDataGateway.API.Types.Querying;
	using SDM = Skyline.DataMiner.SDM;

	public abstract class Repository<T> : SDM.IBulkRepository<T>, SDM.IQueryableRepository<T>
		where T : ApiObject<T>
	{
		private const int _defaultPageSize = 500;

		private readonly FilterElement<DomInstance> _domDefinitionFilter;
		private readonly ApiRepositoryQueryProvider<T> _queryProvider;

		protected Repository(IMediaOpsLiveApi api, DomHelper helper)
		{
			Api = api ?? throw new ArgumentNullException(nameof(api));
			Helper = helper ?? throw new ArgumentNullException(nameof(helper));

			_domDefinitionFilter = DomInstanceExposers.DomDefinitionId.Equal(DomDefinition.Id);
			_queryProvider = new ApiRepositoryQueryProvider<T>(this);
		}

		protected internal IMediaOpsLiveApi Api { get; }

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

		public virtual IReadOnlyCollection<T> CreateOrUpdate(IEnumerable<T> instances)
		{
			if (instances == null)
			{
				throw new ArgumentNullException(nameof(instances));
			}

			var instanceCollection = instances.AsCollection();

			if (instanceCollection.Count == 0)
			{
				// Nothing to create or update.
				return [];
			}

			ValidateBeforeSave(instanceCollection);

			var domInstances = instanceCollection.Select(x => x.DomInstance.ToInstance());
			var result = Helper.DomInstances.CreateOrUpdateInBatches(domInstances);

			result.ThrowOnFailure();

			return result.SuccessfulItems
				.Select(CreateInstance)
				.ToList();
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

			return Count(filter.ToQuery());
		}

		public virtual long Count(IQuery<T> query)
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

			return Count(domQuery);
		}

		internal long Count(FilterElement<DomInstance> domFilter)
		{
			if (domFilter is null)
			{
				throw new ArgumentNullException(nameof(domFilter));
			}

			return Count(domFilter.ToQuery());
		}

		internal long Count(IQuery<DomInstance> domQuery)
		{
			if (domQuery is null)
			{
				throw new ArgumentNullException(nameof(domQuery));
			}

			// Ensure the DomDefinition filter is applied.
			var domFilter = EnsureDomDefinitionFilter(domQuery.Filter);
			domQuery = domQuery.WithFilter(domFilter);

			return Helper.DomInstances.Count(domQuery);
		}

		public virtual IEnumerable<T> ReadAll()
		{
			return ReadDom(_domDefinitionFilter);
		}

		public virtual IEnumerable<RepositoryPage<T>> ReadAllPaged(int pageSize = _defaultPageSize)
		{
			return ReadDomPaged(_domDefinitionFilter, pageSize);
		}

		public virtual T Read(Guid id)
		{
			if (id == Guid.Empty)
			{
				return null;
			}

			var filter = _domDefinitionFilter
				.AND(DomInstanceExposers.Id.Equal(id));

			return ReadDom(filter).SingleOrDefault();
		}

		public virtual T Read(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException(nameof(name));
			}

			var filter = _domDefinitionFilter
				.AND(DomInstanceExposers.Name.Equal(name));

			return ReadDom(filter).SingleOrDefault();
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
					x => ReadDom(x))
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
					x => ReadDom(x))
				.SafeToDictionary(x => new ApiObjectReference<T>(x.ID));
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
					x => ReadDom(x))
				.SafeToDictionary(x => x.DomInstance.Name, x => x);
		}

		public virtual IEnumerable<T> Read(FilterElement<T> filter)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			var domFilter = TranslateFullFilter(filter);

			return ReadDom(domFilter);
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

			return ReadDom(domQuery);
		}

		internal IEnumerable<T> ReadDom(FilterElement<DomInstance> domFilter)
		{
			if (domFilter == null)
			{
				throw new ArgumentNullException(nameof(domFilter));
			}

			return ReadDom(domFilter.ToQuery());
		}

		internal IEnumerable<T> ReadDom(IQuery<DomInstance> domQuery)
		{
			if (domQuery == null)
			{
				throw new ArgumentNullException(nameof(domQuery));
			}

			// Ensure the DomDefinition filter is applied.
			var domFilter = EnsureDomDefinitionFilter(domQuery.Filter);
			domQuery = domQuery.WithFilter(domFilter);

			var domInstances = Helper.DomInstances.Read(domQuery);

			return domInstances.Select(CreateInstance);
		}

		public virtual IEnumerable<RepositoryPage<T>> ReadPaged(FilterElement<T> filter, int pageSize = _defaultPageSize)
		{
			if (filter == null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			var domFilter = TranslateFullFilter(filter);

			return ReadDomPaged(domFilter, pageSize);
		}

		public virtual IEnumerable<RepositoryPage<T>> ReadPaged(IQuery<T> query, int pageSize = _defaultPageSize)
		{
			if (query == null)
			{
				throw new ArgumentNullException(nameof(query));
			}

			var domFilter = TranslateFullFilter(query.Filter);
			domFilter = EnsureDomDefinitionFilter(domFilter);

			var domOrder = TranslateFullOrderBy(query.Order);

			var domQuery = query
				.WithFilter(domFilter)
				.WithOrder(domOrder);

			return ReadDomPaged(domQuery, pageSize);
		}

		internal IEnumerable<RepositoryPage<T>> ReadDomPaged(FilterElement<DomInstance> domFilter, int pageSize)
		{
			if (domFilter == null)
			{
				throw new ArgumentNullException(nameof(domFilter));
			}

			return ReadDomPaged(domFilter.ToQuery(), pageSize);
		}

		internal IEnumerable<RepositoryPage<T>> ReadDomPaged(IQuery<DomInstance> domQuery, int pageSize)
		{
			if (domQuery == null)
			{
				throw new ArgumentNullException(nameof(domQuery));
			}

			// Ensure the DomDefinition filter is applied.
			var domFilter = EnsureDomDefinitionFilter(domQuery.Filter);
			domQuery = domQuery.WithFilter(domFilter);

			var pagingHelper = Helper.DomInstances.PreparePaging(domQuery, pageSize);
			var pageNumber = 0;

			while (pagingHelper.MoveToNextPage())
			{
				var items = pagingHelper.GetCurrentPage().Select(CreateInstance).ToList();
				var hasNextPage = pagingHelper.HasNextPage();

				yield return new RepositoryPage<T>(items, pageNumber++, hasNextPage);
			}
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
			domFilter = EnsureDomDefinitionFilter(domFilter);

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

		private FilterElement<DomInstance> EnsureDomDefinitionFilter(FilterElement<DomInstance> domFilter)
		{
			if (domFilter is null)
			{
				throw new ArgumentNullException(nameof(domFilter));
			}

			if (domFilter.Equals(_domDefinitionFilter))
			{
				return domFilter;
			}

			if (domFilter is TRUEFilterElement<DomInstance>)
			{
				return _domDefinitionFilter;
			}

			if (domFilter is FALSEFilterElement<DomInstance>)
			{
				return domFilter;
			}

			if (domFilter is ANDFilterElement<DomInstance> andFilter)
			{
				if (andFilter.subFilters.Contains(_domDefinitionFilter))
				{
					return andFilter;
				}

				var subFilters = new List<FilterElement<DomInstance>>(andFilter.subFilters);
				subFilters.Insert(0, _domDefinitionFilter);

				return new ANDFilterElement<DomInstance>(subFilters.ToArray());
			}

			return new ANDFilterElement<DomInstance>(_domDefinitionFilter, domFilter);
		}

		#region SDM Interface Implementations

		IEnumerable<SDM.IPagedResult<T>> SDM.IPageableRepository<T>.ReadPaged(FilterElement<T> filter)
		{
			return ReadPaged(filter);
		}

		IEnumerable<SDM.IPagedResult<T>> SDM.IPageableRepository<T>.ReadPaged(IQuery<T> query)
		{
			return ReadPaged(query);
		}

		IEnumerable<SDM.IPagedResult<T>> SDM.IPageableRepository<T>.ReadPaged(FilterElement<T> filter, int pageSize)
		{
			return ReadPaged(filter, pageSize);
		}

		IEnumerable<SDM.IPagedResult<T>> SDM.IPageableRepository<T>.ReadPaged(IQuery<T> query, int pageSize)
		{
			return ReadPaged(query, pageSize);
		}

		T SDM.ICreatableRepository<T>.Create(T oToCreate)
		{
			return Create(oToCreate);
		}

		T SDM.IUpdatableRepository<T>.Update(T oToUpdate)
		{
			return Update(oToUpdate);
		}

		IReadOnlyCollection<T> SDM.IBulkCreatableRepository<T>.Create(IEnumerable<T> oToCreate)
		{
			return CreateOrUpdate(oToCreate);
		}

		IReadOnlyCollection<T> SDM.IBulkUpdatableRepository<T>.Update(IEnumerable<T> oToUpdate)
		{
			return CreateOrUpdate(oToUpdate);
		}

		IReadOnlyCollection<T> SDM.IBulkRepository<T>.CreateOrUpdate(IEnumerable<T> oToCreateOrUpdate)
		{
			return CreateOrUpdate(oToCreateOrUpdate);
		}

		#endregion
	}
}
