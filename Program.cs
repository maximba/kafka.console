using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace kafka.console
{
    class Program
    {
        static async Task Main (string[] args)
        {
            string kafkaEndpoint = "localhost:9092";
            string kafkaTopic = "cortomaltes";

            var producerConfig = new ProducerConfig { BootstrapServers = kafkaEndpoint };

            Action<DeliveryReport<Null, string>> handler = r => 
            Console.WriteLine(!r.Error.IsError
                ? $"Delivered message to {r.TopicPartitionOffset}"
                : $"Delivery Error: {r.Error.Reason}");

            using (var p = new ProducerBuilder<Null, string>(producerConfig).Build())
            {                        
                Console.WriteLine("Rolling ...");
                for (int i=0; i<10; i++)
                {
                    try
                    {
                        var dr = await p.ProduceAsync(kafkaTopic, new Message<Null, string> { Value = $"Event {i}"});
                        Console.WriteLine($"Delivered '{dr.Value}' to '{dr.TopicPartitionOffset}'");
                    }
                    catch (ProduceException<Null, string> e)
                    {
                        Console.WriteLine($"Delivery failed: {e.Error.Reason}");
                    }
                }
            }
/*            {
                for (int i=0; i<10; i++)
                {
                    var message = $"Event {i}";                    
                    p.Produce("kafkaTopic", new Message<Null,string>{ Value=message}, handler);
                }            
                p.Flush(TimeSpan.FromSeconds(10));
            }
*/

            var consumerConfig = new ConsumerConfig  { GroupId="cortomaltes", BootstrapServers = kafkaEndpoint, AutoOffsetReset = AutoOffsetReset.Earliest };
            
            using (var c = new ConsumerBuilder<Null, string>(consumerConfig).Build())
            {
                c.Subscribe(new List<string>() { kafkaTopic });

                CancellationTokenSource cts = new CancellationTokenSource();
                Console.CancelKeyPress += (_, e) => {
                    e.Cancel = true; // prevent the process from terminating.
                    cts.Cancel();
                };
                Console.WriteLine("Ctrl-C to exit.");

                try
                {
                    while (true)
                    {
                        try
                        {
                            var cr = c.Consume(cts.Token);
                            Console.WriteLine($"Consumed message '{cr.Value}' at: '{cr.TopicPartitionOffset}'.");
                        }
                        catch (ConsumeException e)
                        {
                            Console.WriteLine($"Error occured: {e.Error.Reason}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    c.Close();
                }
            }
        }
    }
}
