namespace Skyline.DataMiner.MediaOps.Live.UnitTesting
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using System.Text;
	using System.Threading.Tasks;

	using Skyline.DataMiner.Net;
	using Skyline.DataMiner.Net.Async;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.ServiceManager.Objects;

	internal class AsyncMessageHandlerMock : IAsyncMessageHandler
	{
		private IConnection _connection;

		public AsyncMessageHandlerMock(IConnection connection)
		{
			_connection = connection ?? throw new ArgumentNullException(nameof(connection));
		}

		public AsyncProgress Launch(params DMSMessage[] messages)
		{
			AsyncProgress progress = AsyncProgressBuilder.CreateInstance(this, messages, 0, null, null, 250);

			AsyncResponseEvent response = new()
			{
				Messages = _connection.HandleMessages(messages),
			};

			MethodInfo dynMethod = this.GetType().GetMethod("SetResponse", BindingFlags.NonPublic | BindingFlags.Instance);
			dynMethod.Invoke(this, new object[] { response });

			return progress;
		}

		public AsyncProgress Launch(DMSMessage message, AsyncResponseEventHandler onCompleteHandler = null, AsyncProgressEventHandler onProgressHandler = null, int pageSize = 250)
		{
			AsyncProgress progress = AsyncProgressBuilder.CreateInstance(this, new[]{message}, 0, onCompleteHandler, onProgressHandler, pageSize);

			AsyncResponseEvent response = new()
			{
				Messages = _connection.HandleMessage(message),
			};

			MethodInfo dynMethod = progress.GetType().GetMethod("SetResponse", BindingFlags.NonPublic | BindingFlags.Instance);
			dynMethod.Invoke(progress, new object[] { response });

			return progress;
		}

		public AsyncProgress Launch(DMSMessage[] messages, AsyncResponseEventHandler onCompleteHandler = null, AsyncProgressEventHandler onProgressHandler = null, int pageSize = 250)
		{
			throw new NotImplementedException();
		}

		public AsyncProgress Launch(DMSMessage[] messages, AsyncResponseEventHandler onCompleteHandler, AsyncProgressEventHandler onProgressHandler, int compatClientCookie, int pageSize)
		{
			throw new NotImplementedException();
		}

		public void Launch(AsyncProgress progress)
		{
			throw new NotImplementedException();
		}

		public AsyncProgress FindRequestInfoByCompatClientCookie(int compatClientCookie)
		{
			throw new NotImplementedException();
		}

		public void HandleAsyncResponseEvent(AsyncResponseEvent responseEvent)
		{
			throw new NotImplementedException();
		}

		public void HandleAsyncProgressEvent(AsyncProgressEvent progressEvent)
		{
			throw new NotImplementedException();
		}

		public void ClearExpiredAsyncRequestResponses()
		{
			// No logic
		}

		public void Remove(AsyncProgress progress)
		{
			// No logic
		}

		public AsyncProgress CreateProgressHandle(DMSMessage[] messages, AsyncResponseEventHandler onCompleteHandler, AsyncProgressEventHandler onProgressHandler, int compatClientCookie = -1, int pageSize = 250)
		{
			throw new NotImplementedException();
		}

		public int UnclaimedAsyncResponsesCount { get; }

		public int TrackedActiveAsyncRequestCount { get; }

		public IConnection Connection { get; }
	}

	public class AsyncProgressBuilder
	{
		public static AsyncProgress CreateInstance(
			IAsyncMessageHandler parent,
			DMSMessage[] messages,
			int compatClientCookie,
			AsyncResponseEventHandler onCompleteHandler,
			AsyncProgressEventHandler onProgressHandler,
			int pageSize)
		{
			var type = typeof(AsyncProgress);

			// Find the internal constructor
			var ctor = type.GetConstructor(
				BindingFlags.Instance | BindingFlags.NonPublic,
				null,
				new Type[] {
					typeof(IAsyncMessageHandler),
					typeof(DMSMessage[]),
					typeof(int),
					typeof(AsyncResponseEventHandler),
					typeof(AsyncProgressEventHandler),
					typeof(int)
				},
				null);

			if (ctor == null)
			{
				throw new InvalidOperationException("Could not find the internal constructor for AsyncProgress.");
			}

			// Invoke the constructor
			var instance = (AsyncProgress)ctor.Invoke(new object[] {
				parent,
				messages,
				compatClientCookie,
				onCompleteHandler,
				onProgressHandler,
				pageSize
			});

			return instance;
		}
	}
}
