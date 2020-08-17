using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Utils.Messaging;

namespace WebApi.Controllers {
    [ApiController]
    [Route ("[controller]")]
    public class SendMessageController : ControllerBase {
        private readonly ILogger<SendMessageController> logger;
        private readonly MessageProducer messageProducer;

        public SendMessageController (ILogger<SendMessageController> logger, MessageProducer messageProducer) {
            this.logger = logger;
            this.messageProducer = messageProducer;
        }

        [HttpGet]
        public async Task<string> Get () {
            await this.messageProducer.SendMessage<string> ($"teste-message {Guid.NewGuid().ToString()}");
            return Task.FromResult<string> ("OK").Result;
        }
    }
}