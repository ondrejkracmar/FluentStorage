﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FluentStorage.Messaging {
	class InMemoryMessenger : IMessenger {
		private static readonly ConcurrentDictionary<string, InMemoryMessenger> _nameToMessenger =
		   new ConcurrentDictionary<string, InMemoryMessenger>();

		private readonly ConcurrentDictionary<string, ConcurrentQueue<IQueueMessage>> _queues =
		   new ConcurrentDictionary<string, ConcurrentQueue<IQueueMessage>>();

		#region [ IMessenger ]

		public Task CreateChannelsAsync(IEnumerable<string> channelNames, CancellationToken cancellationToken = default) {
			foreach (string channelName in channelNames) {
				_queues[channelName] = new ConcurrentQueue<IQueueMessage>();
			}

			return Task.CompletedTask;
		}

		public Task<long> GetMessageCountAsync(string channelName, CancellationToken cancellationToken = default) {
			if (channelName is null)
				throw new ArgumentNullException(nameof(channelName));

			ConcurrentQueue<IQueueMessage> queue = GetQueue(channelName);
			return Task.FromResult((long)queue.Count);
		}

		public Task<IReadOnlyCollection<string>> ListChannelsAsync(CancellationToken cancellationToken = default) {
			return Task.FromResult<IReadOnlyCollection<string>>(_queues.Select(q => q.Key).ToList());
		}

		public Task DeleteChannelsAsync(IEnumerable<string> channelNames, CancellationToken cancellationToken = default) {
			if (channelNames is null)
				throw new ArgumentNullException(nameof(channelNames));

			foreach (string cn in channelNames) {
				_queues.TryRemove(cn, out ConcurrentQueue<IQueueMessage> v);
			}

			return Task.CompletedTask;
		}

		public Task<IReadOnlyCollection<IQueueMessage>> PeekAsync(string channelName, int count = 100, CancellationToken cancellationToken = default) {
			if (channelName is null)
				throw new ArgumentNullException(nameof(channelName));

			return Task.FromResult<IReadOnlyCollection<IQueueMessage>>(GetMessages(channelName, count, true, null));
		}

		public Task<IReadOnlyCollection<IQueueMessage>> ReceiveAsync(
		   string channelName, int count = 100, TimeSpan? visibility = null, CancellationToken cancellationToken = default) {
			if (channelName is null)
				throw new ArgumentNullException(nameof(channelName));

			return Task.FromResult<IReadOnlyCollection<IQueueMessage>>(GetMessages(channelName, count, false, visibility));
		}

		private List<IQueueMessage> GetMessages(string channelName, int count, bool peekOnly, TimeSpan? visibility) {
			var result = new List<IQueueMessage>();
			ConcurrentQueue<IQueueMessage> queue = GetQueue(channelName);

			DateTime now = DateTime.UtcNow;
			DateTimeOffset nextVisible = now + (visibility ?? TimeSpan.FromMinutes(1));

			while (result.Count < count) {
				if (!queue.TryDequeue(out IQueueMessage msg))
					break;

				bool isVisible = msg.NextVisibleTime == null || (msg.NextVisibleTime.Value >= now);

				if (isVisible) {
					result.Add(msg);

					msg.NextVisibleTime = nextVisible;

					if (peekOnly) {
						queue.Enqueue(msg);
					}
				}
				else {
					queue.Enqueue(msg);
				}
			}

			return result;

		}

		public Task SendAsync(string channelName, IEnumerable<IQueueMessage> messages, CancellationToken cancellationToken = default) {
			if (channelName is null)
				throw new ArgumentNullException(nameof(channelName));

			if (messages is null)
				throw new ArgumentNullException(nameof(messages));

			ConcurrentQueue<IQueueMessage> queue = GetQueue(channelName);
			foreach (QueueMessage qm in messages) {
				queue.Enqueue(qm);
			}
			return Task.CompletedTask;
		}

		public void Dispose() {

		}


		#endregion

		private ConcurrentQueue<IQueueMessage> GetQueue(string channelName) {
			return _queues.GetOrAdd(channelName, new ConcurrentQueue<IQueueMessage>());
		}

		public static IMessenger CreateOrGet(string name) {
			if (_nameToMessenger.TryGetValue(name, out InMemoryMessenger messenger))
				return messenger;

			messenger = new InMemoryMessenger();
			_nameToMessenger[name] = messenger;
			return messenger;
		}

		public Task DeleteAsync(string channelName, IEnumerable<IQueueMessage> messages, CancellationToken cancellationToken = default) => throw new NotImplementedException();
		public Task StartMessageProcessorAsync(string channelName, IMessageProcessor messageProcessor) => throw new NotImplementedException();
	}
}
