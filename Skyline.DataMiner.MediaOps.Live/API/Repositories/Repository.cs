namespace Skyline.DataMiner.MediaOps.Live.API.Repositories
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.MediaOps.Live.API.Objects;
	using Skyline.DataMiner.MediaOps.Live.API.Querying;
	using Skyline.DataMiner.MediaOps.Live.API.Tools;
	using Skyline.DataMiner.MediaOps.Live.DOM.Tools;
	using Skyline.DataMiner.MediaOps.Live.Extensions;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Utils.DOM.Extensions;

	using SLDataGateway.API.Querying;
	using SLDataGateway.API.Types.Querying;

	public abstract class Repository<T> where T : ApiObject<T>
	{
		private const int _defaultPageSize = 500;

		private readonly ApiRepositoryQueryProvider<T> _queryProvider;

		protected Repository(DomHelper helper)
		{
			Helper = helper ?? throw new ArgumentNullException(nameof(helper));

			_queryProvider = new ApiRepositoryQueryProvider<T>(this);
		}

		protected DomHelper Helper { get; }

		protected internal abstract DomDefinitionId DomDefinition { get; }

		protected abstract T CreateInstance(DomInstance domInstance);

		protected abstract void ValidateBeforeSave(ICollection<T> instances);

		protected abstract void ValidateBeforeDelete(ICollection<T> instances);

		public virtual void Create(T instance)
		{
			if (instance == null)
			{
				throw new ArgumentNullException(nameof(instance));
			}

			ValidateBeforeSave(new[] { instance });

			Helper.DomInstances.Create(instance.DomInstance);
		}

		public virtual void Update(T instance)
		{
			if (instance == null)
			{
				throw new ArgumentNullException(nameof(instance));
			}

			ValidateBeforeSave(new[] { instance });

			Helper.DomInstances.Update(instance.DomInstance);
		}

		public virtual void CreateOrUpdate(IEnumerable<T> instances)
		{
			if (instances == null)
			{
				throw new ArgumentNullException(nameof(instances));
			}

			var instanceCollection = instances as ICollection<T> ?? instances.ToList();

			ValidateBeforeSave(instanceCollection);

			var domInstances = instanceCollection.Select(x => x.DomInstance.ToInstance());
			Helper.DomInstances.CreateOrUpdateInBatches(domInstances).ThrowOnFailure();
		}

		public virtual void CreateOrUpdate(T instance)
		{
			if (instance == null)
			{
				throw new ArgumentNullException(nameof(instance));
			}

			CreateOrUpdate(new[] { instance });
		}

		public virtual void Delete(T instance)
		{
			if (instance == null)
			{
				throw new ArgumentNullException(nameof(instance));
			}

			ValidateBeforeDelete(new[] { instance });

			Helper.DomInstances.Delete(instance.DomInstance);
		}

		public virtual void Delete(IEnumerable<T> instances)
		{
			if (instances == null)
			{
				throw new ArgumentNullException(nameof(instances));
			}

			var instanceCollection = instances as ICollection<T> ?? instances.ToList();

			ValidateBeforeDelete(instanceCollection);

			var domInstances = instanceCollection.Select(x => x.DomInstance.ToInstance());
			Helper.DomInstances.DeleteInBatches(domInstances).ThrowOnFailure();
		}

		public virtual long CountAll()
		{
			var filter = DomInstanceExposers.DomDefinitionId.Equal(DomDefinition.Id);
			return Helper.DomInstances.Count(filter);
		}

		public virtual long Count(FilterElement<T> filter)
		{
			if (filter is null)
			{
				throw new ArgumentNullException(nameof(filter));
			}

			var domFilter = TranslateFullFilter(filter);
			domFilter = AddDomDefinitionFilter(domFilter);

			return Helper.DomInstances.Count(domFilter);
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
			var filter = DomInstanceExposers.DomDefinitionId.Equal(DomDefinition.Id);
			return Helper.DomInstances.Read(filter).Select(CreateInstance);
		}

		public virtual IEnumerable<IEnumerable<T>> ReadAllPaged(long pageSize = _defaultPageSize)
		{
			var filter = DomInstanceExposers.DomDefinitionId.Equal(DomDefinition.Id);
			return Helper.DomInstances.ReadPaged(filter, pageSize).Select(x => x.Select(CreateInstance));
		}

		public virtual T Read(Guid id)
		{
			var filter = DomInstanceExposers.DomDefinitionId.Equal(DomDefinition.Id)
				.AND(DomInstanceExposers.Id.Equal(id));

			var domInstance = Helper.DomInstances.Read(filter).SingleOrDefault();

			return domInstance != null ? CreateInstance(domInstance) : null;
		}

		public virtual IDictionary<Guid, T> Read(IEnumerable<Guid> ids)
		{
			if (ids == null)
			{
				throw new ArgumentNullException(nameof(ids));
			}

			FilterElement<DomInstance> CreateFilter(Guid id) =>
				DomInstanceExposers.DomDefinitionId.Equal(DomDefinition.Id)
				.AND(DomInstanceExposers.Id.Equal(id));

			return FilterQueryExecutor.RetrieveFilteredItems(
					ids,
					x => CreateFilter(x),
					x => Helper.DomInstances.Read(x))
				.Select(CreateInstance)
				.SafeToDictionary(x => x.ID);
		}

		public virtual T Read(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException(nameof(name));
			}

			var filter = DomInstanceExposers.DomDefinitionId.Equal(DomDefinition.Id)
				.AND(DomInstanceExposers.Name.Equal(name));

			var domInstance = Helper.DomInstances.Read(filter).SingleOrDefault();

			return domInstance != null ? CreateInstance(domInstance) : null;
		}

		public virtual IDictionary<string, T> Read(IEnumerable<string> names)
		{
			if (names == null)
			{
				throw new ArgumentNullException(nameof(names));
			}

			FilterElement<DomInstance> CreateFilter(string name) =>
				DomInstanceExposers.DomDefinitionId.Equal(DomDefinition.Id)
				.AND(DomInstanceExposers.Name.Equal(name));

			return FilterQueryExecutor.RetrieveFilteredItems(
					names,
					x => CreateFilter(x),
					x => Helper.DomInstances.Read(x))
				.SafeToDictionary(x => x.Name, CreateInstance);
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

			if (!(domFilter is ANDFilterElement<DomInstance> andFilter) ||
				!andFilter.subFilters.Contains(DomInstanceExposers.DomDefinitionId.Equal(DomDefinition.Id)))
			{
				domFilter = new ANDFilterElement<DomInstance>(
					DomInstanceExposers.DomDefinitionId.Equal(DomDefinition.Id),
					domFilter);
			}

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
					return FilterElementFactory.Create(DomInstanceExposers.Id, comparer, (Guid)value);
				default:
					throw new NotImplementedException();
			}
		}

		protected internal virtual IOrderByElement CreateOrderBy(string fieldName, SortOrder sortOrder, bool naturalSort = false)
		{
			switch (fieldName)
			{
				case nameof(ApiObject<T>.ID):
					return OrderByElementFactory.Create(DomInstanceExposers.Id, sortOrder, naturalSort);
				default:
					throw new NotImplementedException();
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
			var domDefFilter = DomInstanceExposers.DomDefinitionId.Equal(DomDefinition.Id);

			if (!(domFilter is ANDFilterElement<DomInstance> andFilter) ||
				!andFilter.subFilters.Contains(domDefFilter))
			{
				domFilter = new ANDFilterElement<DomInstance>(
					domDefFilter,
					domFilter);
			}

			return domFilter;
		}
	}
}
