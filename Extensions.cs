namespace GeneralTransitFeed
{
	public static class Extensions
	{
		public static string NullIfWhitespace(this string input)
		{
			if (string.IsNullOrWhiteSpace(input))
			{
				return null;
			}
			else
			{
				return input;
			}
		}
	}
}