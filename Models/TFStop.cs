using System.Collections.Generic;

namespace GeneralTransitFeed.Models
{
	public class TFStop
	{	
		public string Id { get; set; }
		public HashSet<TFTrip> Trips { get; set; } = new HashSet<TFTrip>();
		public HashSet<TFStopTime> StopTimes { get; set; } = new HashSet<TFStopTime>();
		public string Code { get; set; }
		public string Name { get; set; }
		public float Latitude { get; set; }
		public float Longitude { get; set; }
		public string Type { get; set; }
		public string ParentId { get; set; }
		public TFStop Parent { get; set; }
		public HashSet<TFStop> Children { get; set; } = new HashSet<TFStop>();
		public string Platform { get; set; }
	}
}