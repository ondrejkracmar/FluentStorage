﻿using System;
using System.Collections.Generic;
using System.Text;
using Amazon.SQS.Model;
using FluentStorage.Messaging;

namespace FluentStorage.AWS.Messaging {
	static class Converter {
		public const string ReceiptHandlePropertyName = "ReceiptHandle";

		public static SendMessageBatchRequestEntry ToSQSMessage(IQueueMessage message) {
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var r = new SendMessageBatchRequestEntry(Guid.NewGuid().ToString(), message.StringContent);

			if (message.Properties != null) {
				foreach (KeyValuePair<string, string> prop in message.Properties) {
					r.MessageAttributes[prop.Key] = new MessageAttributeValue {
						DataType = "String",
						StringValue = prop.Value
					};
				}
			}

			return r;
		}

		public static IQueueMessage ToQueueMessage(Message sqsMessage) {
			var r = new QueueMessage(sqsMessage.Body);
			r.Id = sqsMessage.MessageId;

			foreach (KeyValuePair<string, MessageAttributeValue> attr in sqsMessage.MessageAttributes) {
				r.Properties[attr.Key] = attr.Value.StringValue;
			}
			r.Properties[ReceiptHandlePropertyName] = sqsMessage.ReceiptHandle;

			return r;
		}
	}
}
