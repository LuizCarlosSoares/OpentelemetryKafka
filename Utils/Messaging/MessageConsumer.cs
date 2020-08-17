using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Context.Propagation;

namespace Utils.Messaging {
    public class MessageConsumer : IDisposable {
        private static readonly ActivitySource ActivitySource = new ActivitySource (nameof (MessageConsumer));
        private static readonly ITextFormat TextFormat = new TraceContextFormat ();
        private readonly ILogger<MessageConsumer> logger;

        private readonly IConfiguration configuration;

        public MessageConsumer (ILogger<MessageConsumer> _logger, IConfiguration _configuration) {
            logger = _logger;
            configuration = _configuration;

        }

        public async Task StartConsume () {

            await Task.Run (() => {
                var config = new ConsumerConfig {
                BootstrapServers = configuration.GetSection ("ProducerConfig:BootstrapServers").Value,
                GroupId = "Opentelemetry consumer",
                AutoOffsetReset = AutoOffsetReset.Earliest
                };

                using (var consumer = new ConsumerBuilder<Ignore, string> (config).Build ()) {

                    consumer.Subscribe (configuration.GetSection ("ProducerTopic").Value);
                    CancellationTokenSource cts = new CancellationTokenSource ();

                    Console.CancelKeyPress += (_, e) => {

                        e.Cancel = true;
                        cts.Cancel ();

                    };

                    try {

                        while (true) {

                            try {

                                var cr = consumer.Consume (cts.Token);
                                Console.WriteLine ($"Consumed message '{cr.Message.Value}' at: '{cr.TopicPartitionOffset}'.");

                                var parentContext = TextFormat.Extract (cr.Message.Headers, this.ExtractTraceContextFromBasicProperties);

                                // Start an activity with a name following the semantic convention of the OpenTelemetry messaging specification.
                                // https://github.com/open-telemetry/opentelemetry-specification/blob/master/specification/trace/semantic_conventions/messaging.md#span-name
                                var activityName = $"{cr.Topic} receive";

                                using (var activity = ActivitySource.StartActivity (activityName, ActivityKind.Consumer, parentContext)) {
                                    this.logger.LogInformation ($"Message received: [{cr.Message.Value}]");

                                    if (activity != null) {
                                        activity.AddTag("messaging.operation","receive");
                                        activity.AddTag ("message", cr.Message.Value);
                                    }

                                    // The OpenTelemetry messaging specification defines a number of attributes. These attributes are added here.
                                }
                            } catch (ConsumeException e) {
                                Console.WriteLine ($"Error occured: {e.Error.Reason}");
                            }
                        }
                    } catch (OperationCanceledException) {

                        consumer.Close ();
                    }

                }
            });

        }

        private IEnumerable<string> ExtractTraceContextFromBasicProperties (Headers props, string key) {
            try {
                for (int i = 0; i < props.Count; i++) {
                    if (props[i].Key == key) {
                        var bytes = props[i].GetValueBytes ();
                        return new [] { Encoding.UTF8.GetString (bytes) };
                    }
                }

            } catch (Exception ex) {
                this.logger.LogError (ex, "Failed to extract trace context: {ex}");
            }

            return Enumerable.Empty<string> ();
        }

        public void Dispose () {
            throw new NotImplementedException ();
        }

    }
}