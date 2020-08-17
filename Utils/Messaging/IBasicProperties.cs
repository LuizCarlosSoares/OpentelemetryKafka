using System.Collections.Generic;
using Confluent.Kafka;

public interface IBasicProperties {

    Headers Headers { get; set; }
}

public class BasicProperties : IBasicProperties {

    public BasicProperties () {
        Headers = new Headers ();
    }
    public Headers Headers { get; set; }
}