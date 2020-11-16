using System;
using System.Text.Json;
using System.Threading;
using Confluent.Kafka;
using MongoDB.Driver;
using DomainEvents;
using MedHistoryService.Models;

namespace MedHistoryService
{
	class Program
	{
		private const string topic = "new-rx";
		private const string consumer_group = "medhistory-service";
		private const string kafkaBrokers = "kafka.kafka-ca1:9092";
		private static string instanceId = Guid.NewGuid().ToString();
		private static string dbName = "patients";
		private static string dbCollectionName = "prescriptions";
		private static string dbConnectionString = "mongodb://medhistory-service-db";
		// private static string dbConnectionString = "mongodb://localhost:27017";

		static void Main(string[] args)
		{
			Console.WriteLine($"MedHistoryService({instanceId}) -  starting");

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
					var dbClient = new MongoClient(dbConnectionString);
            		var database = dbClient.GetDatabase(dbName);

					consumer.Subscribe(topic);

            		var prescriptions = database.GetCollection<Prescription>(dbCollectionName);

					try
					{
						while (true)
						{
							try
							{
								var response = consumer.Consume();

								var rxEvent = JsonSerializer.Deserialize(response.Value, typeof(RxPrescribedEvent), jsonOptions) as RxPrescribedEvent;

								// var rxEvent = new RxPrescribedEvent
								// {
								// 	Patient = new DomainEvents.Patient
								// 	{
								// 		FirstName = "Tom",
								// 		LastName = "Jones"
								// 	},
								// 	Medication = new DomainEvents.Medication
								// 	{
								// 		DrugName = "A Drug"
								// 	}
								// };

								Console.WriteLine($"MedHistoryService({instanceId}) - Received {rxEvent.Medication.DrugName} for patient {rxEvent.Patient.LastName}, {rxEvent.Patient.FirstName}\n");

								var prescription = new Prescription {
									Patient = new Models.Patient {
										FirstName = rxEvent.Patient.FirstName,
										LastName = rxEvent.Patient.LastName
									},
									Medication = new Models.Medication {
										DrugName = rxEvent.Medication.DrugName
									}
								};
            
								prescriptions.InsertOne(prescription);

								Console.WriteLine($"MedHistoryService({instanceId}) - Saved prescription {prescription.Medication.DrugName} for patient {prescription.Patient.LastName}, {prescription.Patient.FirstName}\n");

								// Thread.Sleep(5000);
							}
							catch (ConsumeException e)
							{
								Console.WriteLine($"MedHistoryService({instanceId}) - Event consuming error occurred: {e.Error.Reason}");
							}
							catch (JsonException e)
							{
								Console.WriteLine($"MedHistoryService({instanceId}) - JSON error occurred: {e.Message}");
							}
							catch (Exception e)
							{
								Console.WriteLine($"MedHistoryService({instanceId}) - Exception occurred: {e.Message}");
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
				Console.WriteLine($"MedHistoryService({instanceId}) - unhandled exception: {ex}");
			}
		}
	}
}
