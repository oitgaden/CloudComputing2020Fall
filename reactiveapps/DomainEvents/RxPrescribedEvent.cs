using System;

namespace DomainEvents
{
	public class RxPrescribedEvent
	{
		public DateTime Timestamp { get; set; }

		public Patient Patient { get; set; }
		public Medication Medication { get; set; }

		public override string ToString()
		{
			return $"Drug { Medication.DrugName } prescribed for patient: { Patient.LastName }, { Patient.FirstName }";
		}
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
