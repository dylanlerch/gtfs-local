using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using GeneralTransitFeed.Abstractions;
using GeneralTransitFeed.Models;

namespace GeneralTransitFeed
{
	public class TransitFeedData : ITransitFeedData
	{
		public IReadOnlyDictionary<string, TFRoute> Routes => _routes;
		public IReadOnlyDictionary<string, TFTrip> Trips => _trips;
		public IReadOnlyDictionary<string, TFStop> Stops => _stops;
		public IReadOnlyDictionary<string, TFServiceCalendar> Calendar => _calendar;
		public IReadOnlyDictionary<string, TFServiceCalendarExceptions> CalendarExceptions => _calendarExceptions;

		private Dictionary<string, TFRoute> _routes;
		private Dictionary<string, TFTrip> _trips;
		private Dictionary<string, TFStop> _stops;
		private Dictionary<string, TFServiceCalendar> _calendar;
		private Dictionary<string, TFServiceCalendarExceptions> _calendarExceptions;

		public TransitFeedData()
		{
			_routes = new Dictionary<string, TFRoute>();
			_trips = new Dictionary<string, TFTrip>();
			_stops = new Dictionary<string, TFStop>();
			_calendar = new Dictionary<string, TFServiceCalendar>();
			_calendarExceptions = new Dictionary<string, TFServiceCalendarExceptions>();
		}

		/// <summary>
		/// Reads GTFS (General/Google Transit Feed Specification) data from
		/// a given location, loading it in to the TransitFeed object for
		/// querying and manipulation.
		/// </summary>
		/// <remarks>
		/// The feed has a lot of long string identifiers. Want to reduce
		/// the total number of string assignments and opt for references
		/// to the actual objects. The initial plan was to do this after
		/// reading all the data, but that's a whole pile of pointless
		/// string assignments.
		/// 
		/// Instead, data will be read in the right order so that instead
		/// of storing string identifiers, the reference to the actual
		/// object will be stored. The following diagram shows the 
		/// references in the files that need to be read:
		///
		///  +--------+
		///  | Routes |
		///  +--------+
		///       ^                            +------+
		///       |                            |      |
		///  +--------+   +------------+   +-------+  |
		///  | Trips  |<--| Stop Times |-->| Stops |<-+
		///  +--------+   +------------+   +-------+
		///       |
		///       +-----------------+
		///       |                 |
		///       v                 v
		/// +----------+   +----------------+
		/// | Calendar |   | Calendar Dates |
		/// +----------+   +----------------+
		///
		/// Based on this, the read order is:
		///   - Routes
		///   - Trips
		///   - Stops
		///   - Stop Times
		/// </remarks>
		public void Read(string gtfsFilePath)
		{
			// Routes
			var routesPath = Path.Combine(gtfsFilePath, Constants.Files.Routes);
			ReadFile(routesPath, (csv) =>
			{
				var id = csv.GetField(Constants.Fields.RouteId);
				var route = new TFRoute
				{
					ShortName = csv.GetField(Constants.Fields.RouteShortName).NullIfWhitespace(),
					LongName = csv.GetField(Constants.Fields.RouteLongName).NullIfWhitespace(),
					Type = csv.GetField<TFRouteType>(Constants.Fields.RouteType),
					Color = csv.GetField(Constants.Fields.RouteColor).NullIfWhitespace()
				};

				_routes.Add(id, route);
			});
			Console.WriteLine("Done: Routes");

			// Calendar
			var calendarPath = Path.Combine(gtfsFilePath, Constants.Files.Calendar);
			ReadFile(calendarPath, (csv) =>
			{
				var serviceId = csv.GetField(Constants.Fields.ServiceId);

				var startDate = DateTimeOffset.ParseExact(csv.GetField(Constants.Fields.StartDate), Constants.Formats.Date, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.AssumeUniversal);
				var endDate = DateTimeOffset.ParseExact(csv.GetField(Constants.Fields.EndDate), Constants.Formats.Date, CultureInfo.InvariantCulture.DateTimeFormat, DateTimeStyles.AssumeUniversal);

				var calendar = new TFServiceCalendar
				{
					Id = serviceId,
					Monday = csv.GetField<bool>(Constants.Fields.Monday),
					Tuesday = csv.GetField<bool>(Constants.Fields.Tuesday),
					Wednesday = csv.GetField<bool>(Constants.Fields.Wednesday),
					Thursday = csv.GetField<bool>(Constants.Fields.Thursday),
					Friday = csv.GetField<bool>(Constants.Fields.Friday),
					Saturday = csv.GetField<bool>(Constants.Fields.Saturday),
					Sunday = csv.GetField<bool>(Constants.Fields.Sunday),
					StartDate = startDate,
					EndDate = endDate
				};

				_calendar.Add(serviceId, calendar);
			});
			Console.WriteLine("Done: Calendar");

			// Calendar Date
			var calendarDatePath = Path.Combine(gtfsFilePath, Constants.Files.CalendarDates);
			ReadFile(calendarDatePath, (csv) =>
			{
				var id = csv.GetField(Constants.Fields.ServiceId);

				if (!_calendarExceptions.ContainsKey(id))
				{
					_calendarExceptions[id] = new TFServiceCalendarExceptions();
				}

				var date = DateTimeOffset.ParseExact(csv.GetField(Constants.Fields.Date), Constants.Formats.Date, CultureInfo.InvariantCulture.DateTimeFormat);
				var exceptionType = csv.GetField<TFCalendarExceptionType>(Constants.Fields.ExceptionType);

				_calendarExceptions[id].Exceptions.Add(date, exceptionType);
			});
			Console.WriteLine("Done: Calendar Dates");

			// Trips
			var tripsPath = Path.Combine(gtfsFilePath, Constants.Files.Trips);
			ReadFile(tripsPath, (csv) =>
			{
				var id = csv.GetField(Constants.Fields.TripId);
				var route = _routes[csv.GetField(Constants.Fields.RouteId)];
				var serviceId = csv.GetField(Constants.Fields.ServiceId);

				var trip = new TFTrip
				{
					Route = route,
					Calendar = _calendar.GetValueOrDefault(serviceId),
					CalendarExceptions = _calendarExceptions.GetValueOrDefault(serviceId),
					Headsign = csv.GetField(Constants.Fields.TripHeadsign).NullIfWhitespace(),
					Direction = csv.GetField(Constants.Fields.DirectionId).NullIfWhitespace()
				};

				route.Trips.Add(trip);

				_trips.Add(id, trip);
			});
			Console.WriteLine("Done: Trips");

			// Stops
			var stopsPath = Path.Combine(gtfsFilePath, Constants.Files.Stops);
			ReadFile(stopsPath, (csv) =>
			{
				var id = csv.GetField(Constants.Fields.StopId);
				var stop = new TFStop
				{
					Id = id,
					Code = csv.GetField(Constants.Fields.StopCode).NullIfWhitespace(),
					Name = csv.GetField(Constants.Fields.StopName).NullIfWhitespace(),
					Latitude = csv.GetField<float>(Constants.Fields.StopLat),
					Longitude = csv.GetField<float>(Constants.Fields.StopLon),
					Type = csv.GetField(Constants.Fields.LocationType).NullIfWhitespace(),
					ParentId = csv.GetField(Constants.Fields.ParentStation).NullIfWhitespace(),
					Platform = csv.GetField(Constants.Fields.PlatformCode).NullIfWhitespace()
				};

				_stops.Add(id, stop);
			});

			// Put a reference to the parent stop in each stop.
			foreach (var stop in _stops)
			{
				var parentId = stop.Value.ParentId;
				stop.Value.ParentId = null;

				// If this has a parent, add a reference to it.
				if (parentId is object)
				{
					var parent = _stops[parentId];
					stop.Value.Parent = parent;
					parent.Children.Add(stop.Value);
				}
			}

			Console.WriteLine("Done: Stops");

			// Stop Times
			var stopTimesPath = Path.Combine(gtfsFilePath, Constants.Files.StopTimes);
			ReadFile(stopTimesPath, (csv) =>
			{
				var trip = _trips[csv.GetField(Constants.Fields.TripId)];
				var stop = _stops[csv.GetField(Constants.Fields.StopId)];

				var stopTime = new TFStopTime
				{
					Trip = trip,
					Stop = stop,
					DepartureTime = csv.GetField(Constants.Fields.DepartureTime).NullIfWhitespace(),
					Sequence = csv.GetField<int>(Constants.Fields.StopSequence),
					PickupType = csv.GetField<TFPickupDropOffType>(Constants.Fields.PickupType),
					DropOffType = csv.GetField<TFPickupDropOffType>(Constants.Fields.DropOffType)
				};

				// StopTimes only relate to a stop and a time. Add a reference
				// for this StopTime to both of those. No list is maintained
				trip.StopTimes.Add(stopTime);
				stop.StopTimes.Add(stopTime);

				// Add two way references between trips and stops. This will
				// allow for easy lookup of the relationship between the two
				trip.Stops.Add(stop);
				stop.Trips.Add(trip);
			});

			Console.WriteLine("Done: StopTimes");
		}

		private void ReadFile(string fileName, Action<CsvReader> processLine)
		{
			using (var reader = new StreamReader(fileName))
			using (var csv = new CsvReader(reader))
			{
				csv.Configuration.PrepareHeaderForMatch = (header, index) => header.ToLower();

				csv.Read();
				csv.ReadHeader();

				while (csv.Read())
				{
					processLine(csv);
				}
			}
		}
	}
}