﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Queue;
using FluentStorage.Messaging;
using WSE = Microsoft.Azure.Storage.StorageException;

namespace FluentStorage.Azure.Queues {
	class AzureStorageQueueMessenger : IMessenger {
		private readonly CloudQueueClient _client;
		private readonly ConcurrentDictionary<string, CloudQueue> _channelNameToQueue =
		   new ConcurrentDictionary<string, CloudQueue>();

		/// <summary>
		/// Initializes a new instance of <see cref="AzureStorageQueueMessenger"/>.
		/// </summary>
		/// <param name="accountName">
		/// The account name to authenticate with the Azure Storage Queue service.
		/// Must not be <see langword="null"/> or empty.
		/// </param>
		/// <param name="storageKey">
		/// The key to authenticate with the Azure Storage Queue service.
		/// Must not be <see langword="null"/> or empty.
		/// </param>
		/// <param name="serviceUri">
		/// An <see cref="Uri"/> that points to an implementation of an Azure Storage Queue service.
		/// See <see cref="AzureStorageQueueMessenger(string,string)"/> if your not sure about this parameter.
		/// Must not be <see langword="null"/>.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown if any of the arguments is <see langword="null"/> or empty.
		/// </exception>
		public AzureStorageQueueMessenger(
		   string accountName, string storageKey, Uri serviceUri) {
			if (string.IsNullOrEmpty(accountName))
				throw new ArgumentNullException(nameof(accountName));
			if (string.IsNullOrEmpty(storageKey))
				throw new ArgumentNullException(nameof(storageKey));
			if (serviceUri is null)
				throw new ArgumentNullException(nameof(serviceUri));

			_client = new CloudQueueClient(serviceUri, new StorageCredentials(accountName, storageKey));
		}

		/// <summary>
		/// Initializes a new instance of <see cref="AzureStorageQueueMessenger"/>.
		/// </summary>
		/// <param name="accountName">
		/// The account name to authenticate with the Azure Storage Queue service.
		/// Must not be <see langword="null"/> or empty.
		/// </param>
		/// <param name="storageKey">
		/// The key to authenticate with the Azure Storage Queue service.
		/// Must not be <see langword="null"/> or empty.
		/// </param>
		/// <exception cref="ArgumentNullException">
		/// Thrown if any of the arguments is <see langword="null"/>.
		/// </exception>
		public AzureStorageQueueMessenger(
		   string accountName, string storageKey) {
			if (string.IsNullOrEmpty(accountName))
				throw new ArgumentNullException(nameof(accountName));
			if (string.IsNullOrEmpty(storageKey))
				throw new ArgumentNullException(nameof(storageKey));

			var account = new CloudStorageAccount(new StorageCredentials(accountName, storageKey), true);
			_client = account.CreateCloudQueueClient();
		}

		private async Task<CloudQueue> GetQueueAsync(string channelName, bool createIfNotExists = false) {
			if (_channelNameToQueue.TryGetValue(channelName, out CloudQueue queue))
				return queue;

			queue = _client.GetQueueReference(channelName);

			if (createIfNotExists) {
				await queue.CreateIfNotExistsAsync().ConfigureAwait(false);
			}

			_channelNameToQueue[channelName] = queue;
			return queue;
		}

		#region [ IMessenger ]

		public async Task CreateChannelsAsync(IEnumerable<string> channelNames, CancellationToken cancellationToken = default) {
			await Task.WhenAll(channelNames.Select(cn => GetQueueAsync(cn, true))).ConfigureAwait(false);
		}


		public async Task<long> GetMessageCountAsync(string channelName, CancellationToken cancellationToken = default) {
			if (channelName is null)
				throw new ArgumentNullException(nameof(channelName));

			CloudQueue queue = await GetQueueAsync(channelName, false).ConfigureAwait(false);

			if (queue == null)
				return 0;

			try {
				await queue.FetchAttributesAsync().ConfigureAwait(false);
			}
			catch (WSE ex) when (ex.RequestInformation.HttpStatusCode == 404) {
				return 0;
			}

			return queue.ApproximateMessageCount ?? 0;
		}

		public async Task<IReadOnlyCollection<string>> ListChannelsAsync(CancellationToken cancellationToken = default) {
			var queueNames = new List<string>();

			QueueContinuationToken token = null;

			do {
				QueueResultSegment page = await _client.ListQueuesSegmentedAsync(token).ConfigureAwait(false);

				queueNames.AddRange(page.Results.Select(q => q.Name));

				token = page.ContinuationToken;

			}
			while (token != null);

			return queueNames;
		}

		public async Task DeleteChannelsAsync(IEnumerable<string> channelNames, CancellationToken cancellationToken = default) {
			if (channelNames is null)
				throw new ArgumentNullException(nameof(channelNames));

			foreach (string queueName in channelNames) {
				CloudQueue queue = _client.GetQueueReference(queueName);
				await queue.DeleteIfExistsAsync(cancellationToken).ConfigureAwait(false);
			}
		}

		public async Task SendAsync(string channelName, IEnumerable<IQueueMessage> messages, CancellationToken cancellationToken = default) {
			CloudQueue queue = await GetQueueAsync(channelName).ConfigureAwait(false);

			await Task.WhenAll(messages.Select(async m => {
				CloudQueueMessage nativeMessage = Converter.ToCloudQueueMessage(m);
				await queue.AddMessageAsync(nativeMessage, cancellationToken).ConfigureAwait(false);
				m.Id = Converter.CreateId(nativeMessage);
			})).ConfigureAwait(false);
		}

		public async Task<IReadOnlyCollection<IQueueMessage>> PeekAsync(string channelName, int count = 100, CancellationToken cancellationToken = default) {
			if (channelName is null)
				throw new ArgumentNullException(nameof(channelName));

			//you can only peek a max of 32 messages
			if (count > 32)
				count = 32;

			CloudQueue queue = await GetQueueAsync(channelName).ConfigureAwait(false);

			IEnumerable<CloudQueueMessage> batch = await queue.PeekMessagesAsync(count).ConfigureAwait(false);

			return batch.Select(Converter.ToQueueMessage).ToList();

		}

		public async Task<IReadOnlyCollection<IQueueMessage>> ReceiveAsync(
		   string channelName, int count = 100, TimeSpan? visibility = null, CancellationToken cancellationToken = default) {
			if (channelName is null)
				throw new ArgumentNullException(nameof(channelName));

			//storage queue can get up to 32 messages
			if (count > 32)
				count = 32;

			CloudQueue queue = await GetQueueAsync(channelName).ConfigureAwait(false);

			IEnumerable<CloudQueueMessage> batch = await queue.GetMessagesAsync(count, visibility, null, null, cancellationToken).ConfigureAwait(false);

			if (batch == null)
				return new QueueMessage[0];

			List<IQueueMessage> result = batch.Select(Converter.ToQueueMessage).ToList();
			return result;
		}


		public void Dispose() {

		}

		public Task DeleteAsync(string channelName, IEnumerable<IQueueMessage> messages, CancellationToken cancellationToken = default) => throw new NotImplementedException();
		public Task StartMessageProcessorAsync(string channelName, IMessageProcessor messageProcessor) => throw new NotImplementedException();


		#endregion

	}
}
