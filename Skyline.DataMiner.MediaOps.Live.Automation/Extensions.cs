namespace Skyline.DataMiner.MediaOps.Live.Automation
{
	using System;
	using System.Collections.Generic;

	using Newtonsoft.Json;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.MediaOps.Live.API;
	using Skyline.DataMiner.MediaOps.Live.API.Caching;
	using Skyline.DataMiner.MediaOps.Live.Automation.API;
	using Skyline.DataMiner.MediaOps.Live.Automation.Logging;
	using Skyline.DataMiner.MediaOps.Live.Automation.Tools;

	public static class Extensions
	{
		public static MediaOpsLiveApi GetMediaOpsLiveApi(this IEngine engine)
		{
			if (engine is null)
			{
				throw new ArgumentNullException(nameof(engine));
			}

			var api = new EngineMediaOpsLiveApi(engine);
			api.SetLogger(new EngineLogger(engine));

			return api;
		}

		public static MediaOpsLiveCache GetMediaOpsLiveCache(this IEngine engine)
		{
			if (engine is null)
			{
				throw new ArgumentNullException(nameof(engine));
			}

			return MediaOpsLiveCache.GetOrCreate(StaticEngineConnectionProvider.GetConnection);
		}

		public static IList<T> ReadScriptParamListFromApp<T>(this IEngine engine, string name)
		{
			if (String.IsNullOrWhiteSpace(name))
			{
				throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
			}

			var param = engine.GetScriptParam(name);

			if (param == null)
			{
				throw new ArgumentException($"Couldn't find script parameter with name '{name}'");
			}

			try
			{
				if (String.IsNullOrWhiteSpace(param.Value))
				{
					return Array.Empty<T>();
				}

				try
				{
					return JsonConvert.DeserializeObject<T[]>(param.Value);
				}
				catch (Exception)
				{
					// needed for when the value is not encapsulated with []
					return new[] { TryConvertSingleValue<T>(param.Value) };
				}
			}
			catch
			{
				throw new InvalidOperationException($"Unable to convert script parameter '{name}' to list of {typeof(T).Name} (value: {param.Value}).");
			}
		}

		public static T ReadScriptParamSingleFromApp<T>(this IEngine engine, string name)
		{
			var values = engine.ReadScriptParamListFromApp<T>(name);

			if (values.Count == 0)
			{
				throw new ArgumentException($"No value was provided for parameter '{name}'");
			}

			if (values.Count > 1)
			{
				throw new ArgumentException($"Multiple values were provided for parameter '{name}'");
			}

			return values[0];
		}

		private static T TryConvertSingleValue<T>(string value)
		{
			if (typeof(T) == typeof(Guid) && Guid.TryParse(value, out var guid))
			{
				return (T)(object)guid;
			}

			if (typeof(IConvertible).IsAssignableFrom(typeof(T)))
			{
				return (T)Convert.ChangeType(value, typeof(T));
			}

			return JsonConvert.DeserializeObject<T>(value);
		}
	}
}
