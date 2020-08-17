using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Utils.Messaging;

namespace Worker {
    public class Worker : BackgroundService {
        private readonly ILogger<Worker> logger;

        private readonly MessageConsumer consumer;

        public Worker (ILogger<Worker> _logger, MessageConsumer _consumer) {
            logger = _logger;
            consumer = _consumer;
        }

        protected override async Task ExecuteAsync (CancellationToken stoppingToken) {
            stoppingToken.ThrowIfCancellationRequested ();

            await consumer.StartConsume ();

            await Task.CompletedTask;
        }
    }
}