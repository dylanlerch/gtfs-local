using System.Collections.Generic;
using GeneralTransitFeed.Models;

namespace GeneralTransitFeed.Abstractions
{
	public interface ITransitFeedData
	{
		IReadOnlyDictionary<string, TFRoute> Routes { get; }
		IReadOnlyDictionary<string, TFTrip> Trips { get; }
		IReadOnlyDictionary<string, TFStop> Stops { get; }
		IReadOnlyDictionary<string, TFServiceCalendar> Calendar { get; }
		IReadOnlyDictionary<string, TFServiceCalendarExceptions> CalendarExceptions { get; }

		void Read(string gtfsFilePath);
	}
}