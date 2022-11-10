using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tni.Helper.Extensions
{
    /// <summary>
    /// Contains various utilities for working with Dates
    /// </summary>
    internal static class DateExtensions
    {
        /// <summary>
        /// Get start of the provided date
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static DateTime StartOfDay(this DateTime source)
        {
            return source.Date;
        }

        /// <summary>
        /// Get end of the provided date
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static DateTime EndOfDay(this DateTime source)
        {
            return source.Date.AddDays(1).AddSeconds(-1);
        }

        /// <summary>
        /// Get start of the month of the provided date
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static DateTime StartOfMonth(this DateTime source)
        {
            return new DateTime(source.Year, source.Month, 1);
        }

        /// <summary>
        /// Get end of the month of the provided date
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static DateTime EndOfMonth(this DateTime source)
        {
            return new DateTime(source.Year, source.Month, 1).AddMonths(1).Subtract(new TimeSpan(0, 0, 0, 0, 1));
        }

        /// <summary>
        /// Get start/end date of the week in which provided date is in.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Tuple<DateTime, DateTime> GetWeek(this DateTime source)
        {
            var startOfWeek = source.AddDays((int)(source.DayOfWeek) * -1).StartOfDay();
            return new Tuple<DateTime, DateTime>(startOfWeek, startOfWeek.AddDays(6).EndOfDay());
        }

        /// <summary>
        /// Gets range of dates describing weeks inside of a month on which the provided date is in.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static List<Tuple<DateTime, DateTime>> GetMonthWeeks(this DateTime source)
        {
            var calendar = CultureInfo.CurrentCulture.Calendar;
            var firstDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            var year = source.Year;
            var month = source.Month;
            var weekPeriods =
            Enumerable.Range(1, calendar.GetDaysInMonth(year, month))
            .Select(d =>
            {
                var date = new DateTime(year, month, d);
                var weekNumInYear = calendar.GetWeekOfYear(date, CalendarWeekRule.FirstDay, firstDayOfWeek);
                return new { date, weekNumInYear };
            })
            .GroupBy(gb => gb.weekNumInYear)
            .Select(x => new Tuple<DateTime, DateTime>(x.First().date, x.Last().date))
            .ToList();

            return weekPeriods;
        }

        /// <summary>
        /// Get the dates in which provided days of interest lays inside of provided datetime range.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="days"></param>
        /// <returns></returns>
        public static List<DateTime> ExtractDayOccurences(this Tuple<DateTime, DateTime> source, List<DayOfWeek> days)
        {
            var collection = new List<DateTime>();
            if (source.Item1 > source.Item2) return collection;

            var start = source.Item1;
            var end = source.Item2;
            while (true)
            {
                if (days.Any(a => a.Equals(start.DayOfWeek)))
                    collection.Add(start);

                if (start >= end)
                    break;
                else
                    start = start.AddDays(1);
            }

            return collection;
        }

        /// <summary>
        /// Adding business days only to provided date
        /// </summary>
        /// <param name="current">Start of the date</param>
        /// <param name="days">No of business days to be added.</param>
        /// <returns></returns>
        public static DateTime AddBusinessDays(this DateTime current, int days)
        {
            var sign = Math.Sign(days);
            var unsignedDays = Math.Abs(days);
            for (var i = 0; i < unsignedDays; i++)
            {
                do
                {
                    current = current.AddDays(sign);
                } while (current.DayOfWeek == DayOfWeek.Saturday ||
                         current.DayOfWeek == DayOfWeek.Sunday);
            }
            return current;
        }

        /// <summary>
        /// Will compute for a provided date, the week of the year.
        /// </summary>
        /// <param name="source">Date source</param>
        /// <returns></returns>
        public static int WeekOfYear(this DateTime source)
        {
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(source, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        /// <summary>
        /// Getting first date of the provided week of specified year.
        /// </summary>
        /// <param name="weekOfYear">Week of the year</param>
        /// <param name="year">Year</param>
        /// <returns></returns>
        public static DateTime FirstDateOfWeek(this int weekOfYear, int year)
        {
            DateTime jan1 = new DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;

            // Use first Thursday in January to get first week of the year as
            // it will never be in Week 52/53
            DateTime firstThursday = jan1.AddDays(daysOffset);
            var cal = CultureInfo.CurrentCulture.Calendar;
            int firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            var weekNum = weekOfYear;
            // As we're adding days to a date in Week 1,
            // we need to subtract 1 in order to get the right date for week #1
            if (firstWeek == 1)
            {
                weekNum -= 1;
            }

            // Using the first Thursday as starting week ensures that we are starting in the right year
            // then we add number of weeks multiplied with days
            var result = firstThursday.AddDays(weekNum * 7);

            // Subtract 3 days from Thursday to get Monday, which is the first weekday in ISO8601
            return result.AddDays(-3);
        }
    }
}
