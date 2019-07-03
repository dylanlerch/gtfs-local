using System;
using System.Collections.Generic;

namespace GeneralTransitFeed.Models
{
	public enum TFCalendarExceptionType
	{
		Added = 1,
		Removed = 2
	}

	public class TFServiceCalendarExceptions
	{
		public Dictionary<DateTimeOffset, TFCalendarExceptionType> Exceptions { get; set; } = new Dictionary<DateTimeOffset, TFCalendarExceptionType>();
	}
}