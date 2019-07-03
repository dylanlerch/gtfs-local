using System;
using System.Collections.Generic;
using System.Linq;
using GeneralTransitFeed.Abstractions;
using GeneralTransitFeed.Models;

namespace GeneralTransitFeed
{
	public static class TransitFeed
	{
		/// <summary>
		/// Returns all of the top level stations that a given RouteType stops
		/// at.
		/// </summary>	
		public static IEnumerable<TFStop> SearchStationsWithRouteType(ITransitFeedData data, TFRouteType type)
		{
			// RouteType is stored in the route object. Trace this through to
			// the list of stops that have that RouteType.
			var routes = data.Routes.Where(m => m.Value.Type == type);
			var trips = routes.SelectMany(r => r.Value.Trips);
			var stops = trips.SelectMany(t => t.Stops);

			// Convert to HashSet to remove any duplicate stops that might
			// be added through the select statements.
			var distinctStops = stops.ToHashSet();

			// From these stops, need to follow up to the highest level parent.
			// This ensures that we're returning the actual station and not
			// individual platforms.
			var stations = new HashSet<TFStop>();
			foreach (var s in distinctStops)
			{
				stations.Add(HighestParentStop(s));
			}

			return stations;
		}

		/// <summary>
		/// Returns a stop with a given id. If there is no stop with the given
		/// id, returns null.
		/// </summary>
		public static TFStop SearchStopWithId(ITransitFeedData data, string id)
		{
			if (data.Stops.ContainsKey(id))
			{
				return data.Stops[id];
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Returns all bottom level children for a given stop. Will follow
		/// the full chain of children and return all leaf node child stops
		/// (child stops without any children). If the stop has no children,
		/// the stop will be returned in the list.
		/// </summay>
		public static IEnumerable<TFStop> AllLowestChildrenForStop(TFStop stop)
		{
			var result = new List<TFStop>();

			if (stop.Children.Count > 0)
			{
				// If the stop has children, don't add it to the list, but get
				// all of it's lowest children and return them.
				foreach (var child in stop.Children)
				{
					result.AddRange(AllLowestChildrenForStop(child));
				}
			}
			else
			{
				result.Add(stop);
			}

			return result;
		}

		/// <summary>
		/// For a given stop, will follow the chain of parents up to the 
		/// highest level parent.
		/// </summary>
		public static TFStop HighestParentStop(TFStop stop)
		{
			if (stop.Parent is null)
			{
				return stop;
			}
			else
			{
				return HighestParentStop(stop.Parent);
			}
		}

		/// <summary>
		/// Get all of the Trips that go from a given station to another.
		/// </summary>
		public static IEnumerable<TFTrip> GetAllTripsFromTo(TFStop from, TFStop to, DateTimeOffset date)
		{
			var fromStops = AllLowestChildrenForStop(from);
			var toStops = AllLowestChildrenForStop(to);

			var fromTrips = fromStops.SelectMany(s => s.Trips);
			var toTrips = toStops.SelectMany(s => s.Trips);

			// Get all of the trips that stop at A and B
			var tripsBetween = fromTrips.Intersect(toTrips);

			// tripsBetween contains a Trips going in both directions between
			// the two stops. Create a filtered list with only the trips that 
			// stop at 'from' before they stop at 'to'.
			var directionalTripsBetween = new HashSet<TFTrip>();
			foreach (var trip in tripsBetween)
			{
				var fromStopTime = trip.StopTimes.FirstOrDefault(st => fromStops.Contains(st.Stop));
				var toStopTime = trip.StopTimes.FirstOrDefault(st => toStops.Contains(st.Stop));

				if (fromStopTime.Sequence < toStopTime.Sequence)
				{
					if (TripRunsOnDate(trip, date))
					{
						directionalTripsBetween.Add(trip);
					}
				}
			}

			return directionalTripsBetween;
		}

		public static bool TripRunsOnDate(TFTrip trip, DateTimeOffset date)
		{
			// Exceptions overrule the normal calendar. Process them first.
			var exception = trip.CalendarExceptions?.Exceptions.GetValueOrDefault(date);
			if (exception.HasValue)
			{
				return exception.Value == TFCalendarExceptionType.Added;
			}

			var calendar = trip.Calendar;
			if (calendar is object)
			{
				// Ensure the calendar on this trip is for the provided date.
				// Some transit feeds will have trips for upcoming calendar
				// changes that are not active yet.
				if (calendar.StartDate <= date && date <= calendar.EndDate)
				{
					switch (date.DayOfWeek)
					{
						case DayOfWeek.Monday:
							return calendar.Monday;
						case DayOfWeek.Tuesday:
							return calendar.Tuesday;
						case DayOfWeek.Wednesday:
							return calendar.Wednesday;
						case DayOfWeek.Thursday:
							return calendar.Thursday;
						case DayOfWeek.Friday:
							return calendar.Friday;
						case DayOfWeek.Saturday:
							return calendar.Saturday;
						case DayOfWeek.Sunday:
							return calendar.Sunday;
					}
				}
				else 
				{
					return false;
				}
			}

			// Should only reach here if there is an issue with the data or
			// the processing.
			//    - Trip doesn't have a Calendar or CalendarException (it 
			//      should have at least one of each)
			//    - Provided date has a day or week outside of the expected
			//      range.
			return true;
		}
	}
}