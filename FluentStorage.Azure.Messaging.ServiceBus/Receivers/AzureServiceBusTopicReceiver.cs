﻿using Azure.Messaging.ServiceBus;

namespace FluentStorage.Azure.Messaging.ServiceBus.Receivers {
	/// <summary>
	/// Implements message receiver on Azure Service Bus Queues
	/// </summary>
	class AzureServiceBusTopicReceiver : AzureServiceBusReceiver {
		public AzureServiceBusTopicReceiver(string connectionString, string topicName, string subscriptionName,
		                                       ServiceBusClientOptions clientOptions = null,
		                                       ServiceBusProcessorOptions processorOptions = null)
			: base(connectionString, "", topicName, subscriptionName, clientOptions, processorOptions) {
		}
	}
}
