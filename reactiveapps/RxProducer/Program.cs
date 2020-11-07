using System;
using System.Text.Json;
using System.Threading;
using Confluent.Kafka;
using DomainEvents;

namespace RxProducer
{
	class Program
	{
		private const string topic = "new-rx";
		private const string kafkaBrokers = "kafka.kafka-ca1:9092";
		private static int rxId = 1;
		private static string instanceId = Guid.NewGuid().ToString();

		static void Main(string[] args)
		{
			Console.WriteLine($"RxProducer({instanceId}) -  starting");

			var config = new ProducerConfig
			{
				BootstrapServers = kafkaBrokers
			};

			var jsonOptions = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			};

			try
			{
				using (var producer = new ProducerBuilder<Null, string>(config).Build())
				{
					while (true)
					{
						var rxEvent = GenerateRxPrescribedEvent();

						try
						{
							var deliveryResult = producer.ProduceAsync(topic, new Message<Null, string> { Value = JsonSerializer.Serialize(rxEvent, jsonOptions) })
														 .Result;

							Console.WriteLine($"RxProducer({instanceId}) - emitted RxPrescribedEvent '{ deliveryResult.Value }' to '{ deliveryResult.TopicPartitionOffset }'\n");
						}
						catch (ProduceException<Null, string> e)
						{
							Console.WriteLine($"RxProducer({instanceId}) - emitting RxPrescribedEvent failed: { e.Error.Reason }");
						}

						Thread.Sleep(5000);
					}
				}

			}
			catch (Exception ex)
			{
				Console.WriteLine($"RxProducer({instanceId}) - startup failed: " + ex);
			}

			Console.Write($"RxProducer({instanceId}) - exiting");
		}

		private static RxPrescribedEvent GenerateRxPrescribedEvent()
		{
			var rxPrescribedEvent = new RxPrescribedEvent
			{
				Timestamp = DateTime.Now,
				Patient = new Patient
				{
					FirstName = $"PatientFirstName{rxId}",
					LastName = $"PatientLastName{rxId}"
				},
				Medication = new Medication
				{
					DrugName = $"Drug{rxId}"
				}
			};

			rxId++;

			return rxPrescribedEvent;
		}
	}
}
