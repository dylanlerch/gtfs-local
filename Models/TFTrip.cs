using System.Collections.Generic;

namespace GeneralTransitFeed.Models
{
	public class TFTrip
	{
		public TFRoute Route { get; set; }
		public HashSet<TFStop> Stops { get; set; } = new HashSet<TFStop>();
		public HashSet<TFStopTime> StopTimes { get; set; } = new HashSet<TFStopTime>();
		public TFServiceCalendar Calendar { get; set; }
		public TFServiceCalendarExceptions CalendarExceptions { get; set; }
		public string Headsign { get; set; }
		public string Direction { get; set; }
	}
}