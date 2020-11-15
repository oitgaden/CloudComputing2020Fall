using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MedHistoryService.Models
{
	public class RxPrescribed
	{
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

		public Patient Patient { get; set; }

		public Medication Medication { get; set; }
	}

	public class Patient
	{
		public string FirstName { get; set; }
		public string LastName { get; set; }
	}

	public class Medication
	{
		public string DrugName { get; set; }
	}
}
