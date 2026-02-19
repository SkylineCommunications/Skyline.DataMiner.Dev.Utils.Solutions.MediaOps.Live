namespace Skyline.DataMiner.Solutions.MediaOps.Live.API.Tools
{
	using System;
	using System.Linq;
	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects;

	public static class FilterElementFactory
	{
		public static FilterElement<DomInstance> Create<T>(DynamicListExposer<DomInstance, T> exposer, Comparer comparer, object value)
		{
			if (exposer == null)
			{
				throw new ArgumentNullException(nameof(exposer));
			}

			var filterValue = ConvertFilterValue<T>(value);

			return CreateFilter(exposer, comparer, filterValue);
		}

		public static FilterElement<DomInstance> Create<T>(DynamicListExposer<DomInstance, object> exposer, Comparer comparer, object value)
		{
			if (exposer == null)
			{
				throw new ArgumentNullException(nameof(exposer));
			}

			var filterValue = ConvertFilterValue<T>(value);

			return CreateFilter(exposer, comparer, filterValue);
		}

		public static FilterElement<DomInstance> Create<T>(Exposer<DomInstance, T> exposer, Comparer comparer, object value)
		{
			if (exposer == null)
			{
				throw new ArgumentNullException(nameof(exposer));
			}

			var filterValue = ConvertFilterValue<T>(value);

			return CreateFilter(exposer, comparer, filterValue);
		}

		public static FilterElement<DomInstance> Create<T>(Exposer<DomInstance, object> exposer, Comparer comparer, object value)
		{
			if (exposer == null)
			{
				throw new ArgumentNullException(nameof(exposer));
			}

			var filterValue = ConvertFilterValue<T>(value);

			return CreateFilter(exposer, comparer, filterValue);
		}

		private static FilterElement<DomInstance> CreateFilter<T>(DynamicListExposer<DomInstance, T> exposer, Comparer comparer, T filterValue)
		{
			switch (comparer)
			{
				case Comparer.Equals:
					return exposer.Equal(filterValue);
				case Comparer.NotEquals:
					return exposer.NotEqual(filterValue);
				case Comparer.GT:
					return exposer.GreaterThan(filterValue);
				case Comparer.GTE:
					return exposer.GreaterThanOrEqual(filterValue);
				case Comparer.LT:
					return exposer.LessThan(filterValue);
				case Comparer.LTE:
					return exposer.LessThanOrEqual(filterValue);
				case Comparer.Contains:
					return exposer.Contains(filterValue);
				case Comparer.NotContains:
					return exposer.NotContains(filterValue);
				default:
					throw new NotSupportedException("This comparer option is not supported");
			}
		}

		private static FilterElement<DomInstance> CreateFilter<T>(Exposer<DomInstance, T> exposer, Comparer comparer, T filterValue)
		{
			switch (comparer)
			{
				case Comparer.Equals:
					return exposer.UncheckedEqual(filterValue);
				case Comparer.NotEquals:
					return exposer.UncheckedNotEqual(filterValue);
				case Comparer.GT:
					return exposer.UncheckedGreaterThan(filterValue);
				case Comparer.GTE:
					return exposer.UncheckedGreaterThanOrEqual(filterValue);
				case Comparer.LT:
					return exposer.UncheckedLessThan(filterValue);
				case Comparer.LTE:
					return exposer.UncheckedLessThanOrEqual(filterValue);
				default:
					throw new NotSupportedException("This comparer option is not supported");
			}
		}

		private static T ConvertFilterValue<T>(object value)
		{
			if (value == null)
				return default;

			if (value is T typedValue)
				return typedValue;

			var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

			// Handle special type conversions
			if (TryConvertSpecialTypes(value, targetType, out object result))
				return (T)result;

			// Handle implicit operator to target type
			if (TryConvertViaImplicitOperator(value, targetType, out result))
				return (T)result;

			// Fallback for IConvertible
			if (value is IConvertible)
				return (T)Convert.ChangeType(value, targetType);

			throw new InvalidCastException($"Cannot convert value of type {value.GetType()} to {typeof(T)}.");
		}

		private static bool TryConvertSpecialTypes(object value, Type targetType, out object result)
		{
			result = null;

			// DmsElementId -> string
			if (targetType == typeof(string) && value is DmsElementId id)
			{
				result = id.Value;
				return true;
			}

			// IApiObjectReference -> Guid
			else if (targetType == typeof(Guid) && value is IApiObjectReference apiRef)
			{
				result = apiRef.ID;
				return true;
			}

			return false;
		}

		private static bool TryConvertViaImplicitOperator(object value, Type targetType, out object result)
		{
			result = null;

			var implicitOp = value.GetType().GetMethod(
				"op_Implicit",
				System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
				null,
				[value.GetType()],
				null);

			if (implicitOp != null && implicitOp.ReturnType == targetType)
			{
				result = implicitOp.Invoke(null, new object[] { value });
				return true;
			}

			return false;
		}
	}
}
