using System.Globalization;

namespace HumbleChoiceScrapper.Helpers
{
    public static class DateHelper
    {
        public static List<string> GetDatesBetween(string startDate, string endDate)
        {
            var dates = new List<string>();

            // Parsear las fechas
            var start = ParseMonthYear(startDate);
            var end = ParseMonthYear(endDate);

            var current = start;

            while (current <= end)
            {
                dates.Add(FormatMonthYear(current));
                current = current.AddMonths(1);
            }

            return dates;
        }

        private static DateTime ParseMonthYear(string monthYear)
        {
            var parts = monthYear.Split('-');
            var monthName = parts[0];
            var year = int.Parse(parts[1]);

            var month = DateTime.ParseExact(monthName, "MMMM", CultureInfo.InvariantCulture).Month;
            return new DateTime(year, month, 1);
        }

        private static string FormatMonthYear(DateTime date)
        {
            var monthName = date.ToString("MMMM", CultureInfo.InvariantCulture).ToLower();
            var year = date.ToString("yyyy");
            return $"{monthName}-{year}";
        }
    }

}
