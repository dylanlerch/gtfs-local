using System.Collections.Generic;

namespace GeneralTransitFeed.Models
{	
	public enum TFRouteType
	{
		Tram = 0,
		Subway = 1,
		Rail = 2,
		Bus = 3,
		Ferry = 4,
		CableCar = 5,
		Gondola = 6,
		Funicular = 7
	}
	
	public class TFRoute
	{
		public HashSet<TFTrip> Trips { get; set; } = new HashSet<TFTrip>();
		public string ShortName { get; set; }
		public string LongName { get; set; }
		public TFRouteType Type { get; set; }
		public string Color { get; set; }
	}
}