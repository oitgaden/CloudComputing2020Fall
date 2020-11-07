using System;
using System.Text.Json;
using Confluent.Kafka;
using DomainEvents;

namespace PatientService
{
	class Program
	{
		private const string topic = "new-rx";
		private const string consumer_group = "patient-service";
		private const string kafkaBrokers = "kafka.kafka-ca1:9092";
		private static string instanceId = Guid.NewGuid().ToString();

		static void Main(string[] args)
		{
			Console.WriteLine($"PatientService({instanceId}) -  starting");

			var conf = new ConsumerConfig
			{
				GroupId = consumer_group,
				BootstrapServers = kafkaBrokers,
				// Note: The AutoOffsetReset property determines the start offset in the event
				// there are not yet any committed offsets for the consumer group for the
				// topic/partitions of interest. By default, offsets are committed
				// automatically, so in this example, consumption will only start from the
				// earliest message in the topic 'my-topic' the first time you run the program.
				AutoOffsetReset = AutoOffsetReset.Earliest
			};

			var jsonOptions = new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			};

			try
			{
				using (var consumer = new ConsumerBuilder<Ignore, string>(conf).Build())
				{
					consumer.Subscribe(topic);

					try
					{
						while (true)
						{
							try
							{
								var response = consumer.Consume();

								var rxEvent = JsonSerializer.Deserialize(response.Value, typeof(RxPrescribedEvent), jsonOptions) as RxPrescribedEvent;
								
								if (rxEvent.Medication == null)
								{
									Console.WriteLine("***Throwing away junk ***");
									continue;
								}

								Console.WriteLine($"PatientService({instanceId}) - Medication {rxEvent.Medication.DrugName} prescribed notification sent to patient {rxEvent.Patient.LastName}, {rxEvent.Patient.FirstName}\n");
							}
							catch (ConsumeException e)
							{
								Console.WriteLine($"PatientService({instanceId}) - Event consuming error occured: {e.Error.Reason}");
							}
							catch (JsonException e)
							{
								Console.WriteLine($"PatientService({instanceId}) - JSON error occured: {e.Message}");
							}
						}
					}
					catch (OperationCanceledException)
					{
						// Ensure the consumer leaves the group cleanly and final offsets are committed.
						consumer.Close();
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"PatientService({instanceId}) - unhandled exception: {ex}");
			}
		}
	}
}
