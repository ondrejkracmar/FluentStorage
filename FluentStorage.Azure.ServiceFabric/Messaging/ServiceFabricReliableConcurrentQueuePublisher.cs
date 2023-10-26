﻿using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Data.Collections;
using FluentStorage.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FluentStorage.Microsoft.ServiceFabric.Messaging {
	class ServiceFabricReliableConcurrentQueuePublisher : IMessenger {
		private readonly IReliableStateManager _stateManager;
		private readonly string _queueName;
		private readonly TimeSpan? _timeout;

		public ServiceFabricReliableConcurrentQueuePublisher(IReliableStateManager stateManager, string queueName, TimeSpan? timeout = null) {
			_stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
			_queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
			_timeout = timeout;
		}

		public async Task PutMessagesAsync(IReadOnlyCollection<IQueueMessage> messages, CancellationToken cancellationToken) {
			IReliableConcurrentQueue<byte[]> collection = await _stateManager.GetOrAddAsync<IReliableConcurrentQueue<byte[]>>(_queueName).ConfigureAwait(false);

			using (var tx = new ServiceFabricTransaction(_stateManager, null)) {
				foreach (QueueMessage message in messages) {
					byte[] data = message.ToByteArray();
					await collection.EnqueueAsync(tx.Tx, data, cancellationToken, _timeout).ConfigureAwait(false);
				}

				await tx.CommitAsync().ConfigureAwait(false);
			}
		}

		public void Dispose() {

		}

		public Task CreateChannelsAsync(IEnumerable<string> channelNames, CancellationToken cancellationToken = default) => throw new NotImplementedException();
		public Task<IReadOnlyCollection<string>> ListChannelsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
		public Task DeleteChannelsAsync(IEnumerable<string> channelNames, CancellationToken cancellationToken = default) => throw new NotImplementedException();
		public Task<long> GetMessageCountAsync(string channelName, CancellationToken cancellationToken = default) => throw new NotImplementedException();
		public Task SendAsync(string channelName, IEnumerable<IQueueMessage> messages, CancellationToken cancellationToken = default) => throw new NotImplementedException();
		public Task<IReadOnlyCollection<IQueueMessage>> ReceiveAsync(string channelName, int count = 100, TimeSpan? visibility = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
		public Task<IReadOnlyCollection<IQueueMessage>> PeekAsync(string channelName, int count = 100, CancellationToken cancellationToken = default) => throw new NotImplementedException();
		public Task DeleteAsync(string channelName, IEnumerable<IQueueMessage> messages, CancellationToken cancellationToken = default) => throw new NotImplementedException();
		public Task StartMessageProcessorAsync(string channelName, IMessageProcessor messageProcessor) => throw new NotImplementedException();
	}
}
