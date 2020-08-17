using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Context.Propagation;

namespace Utils.Messaging {

    public class MessageProducer : IDisposable {

        private static readonly ActivitySource ActivitySource = new ActivitySource (nameof (MessageProducer));
        private static readonly ITextFormat TextFormat = new TraceContextFormat ();
        private readonly IConfiguration configuration;
        private readonly ILogger<MessageProducer> logger;

        private ProducerConfig producerConfig;

        public MessageProducer (ILogger<MessageProducer> _logger, IConfiguration _configuration) {

            logger = _logger;
            configuration = _configuration;
            producerConfig = new ProducerConfig () { BootstrapServers = configuration.GetSection ("ProducerConfig:BootstrapServers").Value, ClientId = "Test" };

        }
        public async Task SendMessage<T> (T messageBody) where T : class {

            var producerTopic = configuration.GetSection ("ProducerTopic").Value;
            var activityName = $"{producerTopic} send";
            var props = new BasicProperties ();

            using (var activity = ActivitySource.StartActivity (activityName, ActivityKind.Producer)) {

                if (activity != null) {

                    TextFormat.Inject (activity.Context, props, this.InjectTraceContextIntoBasicProperties);
                    AddMessagingTags (activity);

                    using (var producer = new ProducerBuilder<Null, T> (producerConfig).Build ()) {

                        try {

                            var dr = await producer.ProduceAsync (producerTopic, new Message<Null, T> { Value = messageBody, Headers = props.Headers });

                        } catch (ProduceException<Null, string> e) {

                            Console.WriteLine ($"Delivery failed: {e.Error.Reason}");

                        }

                    }

                }

            }

        }

        public void AddMessagingTags (Activity activity) {
            // These tags are added demonstrating the semantic conventions of the OpenTelemetry messaging specification
            // See:
            //   * https://github.com/open-telemetry/opentelemetry-specification/blob/master/specification/trace/semantic_conventions/messaging.md#messaging-attributes
            //   * https://github.com/open-telemetry/opentelemetry-specification/blob/master/specification/trace/semantic_conventions/messaging.md#rabbitmq       
            activity?.AddTag ("messaging.system", "kafka");
            activity?.AddTag ("messaging.destination_kind", "topic");
            activity?.AddTag ("messaging.destination", configuration.GetSection ("ProducerTopic").Value);
        }

        private void InjectTraceContextIntoBasicProperties (IBasicProperties props, string key, string value) {
            try {
                if (props.Headers == null) {
                    props.Headers = new Headers ();
                }
                var header = new Header (key, Encoding.ASCII.GetBytes (value));

                props.Headers.Add (header);

            } catch (Exception ex) {
                this.logger.LogError (ex, "Failed to inject trace context.");
            }
        }
        public void Dispose () {

        }

    }

}