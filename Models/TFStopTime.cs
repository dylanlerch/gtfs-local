namespace GeneralTransitFeed.Models
{
	public enum TFPickupDropOffType
	{
		RegularlyScheduled = 0,
		NotAvailable = 1,
		PhoneAgency = 2,
		CoordinateWithDriver = 3
	}
	
	public class TFStopTime
	{
		public TFTrip Trip { get; set; }
		public TFStop Stop { get; set; }
		public string ArrivalTime { get; set; }
		public string DepartureTime { get; set; }
		public int Sequence { get; set; }
		public TFPickupDropOffType PickupType { get; set; }
		public TFPickupDropOffType DropOffType { get; set; } 
	}
}