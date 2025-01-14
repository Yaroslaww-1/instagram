﻿using Confluent.Kafka;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaLakeCore.Infrastructure.EventBus.Integration.Kafka
{
    public class KafkaIntegrationEventBus : IIntegrationEventBus, IDisposable
    {
        private readonly KafkaConnectionFactory _connectionFactory;
        private readonly IConsumer<Ignore, string> _consumer;
        private readonly IDictionary<KeyValuePair<string, string>, Type> _subscribedEvents;
        private readonly ILogger<IIntegrationEventBus> _logger;

        public KafkaIntegrationEventBus(KafkaConnectionFactory connectionFactory, ILogger<IIntegrationEventBus> logger)
        {
            _connectionFactory = connectionFactory;
            _subscribedEvents = new Dictionary<KeyValuePair<string, string>, Type>();
            _logger = logger;
            _consumer = new ConsumerBuilder<Ignore, string>(_connectionFactory.GetConsumerConfig()).Build();
        }
      
        public void Subscribe<T>() where T : IntegrationEvent, new()
        {
            var eventType = typeof(T).Name;
            if (eventType != null)
            {
                var eventKey = new KeyValuePair<string, string>(new T().AggregateEntityName, eventType);
                _subscribedEvents.TryAdd(eventKey, new T().GetType());
            }
        }

        public void StartConsumer(IApplicationBuilder app)
        {
            new Thread(() => StartConsumerLoop(app)).Start();
        }

        private async Task StartConsumerLoop(IApplicationBuilder app)
        {
            _subscribedEvents.Keys
                .Select(p => p.Key)
                .Distinct()
                .ToList()
                .ForEach(aggregateName => {
                    _logger.LogInformation($"Subscribed to topic {aggregateName}");
                    _consumer.Subscribe(aggregateName);
                });

            while (true)
            {
                try
                {
                    // We will give the process 1 second to commit the message and store its offset. TimeSpan.FromMilliseconds(_connectionFactory.GetConsumerConfig().MaxPollIntervalMs.Value - 1000.0)
                    var consumeResult = _consumer.Consume();

                    await ProcessMessage(app, consumeResult);
                }
                catch (ConsumeException e)
                {
                    _logger.LogError($"Kafka consume fatal error occured: {e.Error.Reason}");

                    if (e.Error.IsFatal)
                    {
                        // https://github.com/edenhill/librdkafka/blob/master/INTRODUCTION.md#fatal-consumer-errors
                        break;
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError($"Kafka confume error occured: {e}");
                }
            }
        }

        private async Task ProcessMessage(IApplicationBuilder app, ConsumeResult<Ignore, string> consumeResult)
        {
            Message<Ignore, string> message = consumeResult.Message;

            var eventType = Encoding.UTF8.GetString(message.Headers
                .Where(h => h.Key == "eventType")
                .First()
                .GetValueBytes());

            _subscribedEvents.TryGetValue(new KeyValuePair<string, string>(consumeResult.Topic, eventType), out var EventClassType);

            _logger.LogInformation($"Event received: {eventType} {message.Value}");

            var @event = JsonConvert.DeserializeObject(message.Value, EventClassType);

            using (var scope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Publish(@event);
            };

            _logger.LogInformation($"Event processed: {eventType} {message.Value}");
        }

        public void Dispose()
        {
            _consumer.Dispose();
        }
    }
}
