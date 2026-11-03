namespace Backend.Services;

public sealed class ServiceSchedule
{
    public string TimeZoneId { get; set; } = "Europe/Budapest";
    public int OpeningHour { get; set; } = 9;
    public int ClosingHour { get; set; } = 17;
    public int SlotMinutes { get; set; } = 60;
    public int BookingHorizonDays { get; set; } = 90;

    public IEnumerable<DateTime> Slots(DateOnly date)
    {
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) yield break;
        var zone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        var start = date.ToDateTime(new TimeOnly(OpeningHour, 0));
        var end = date.ToDateTime(new TimeOnly(ClosingHour, 0));
        for (var local = start; local.AddMinutes(SlotMinutes) <= end; local = local.AddMinutes(SlotMinutes))
            if (!zone.IsInvalidTime(local) && !zone.IsAmbiguousTime(local))
                yield return TimeZoneInfo.ConvertTimeToUtc(local, zone);
    }

    public DateOnly Today(DateTime utcNow) => DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTimeFromUtc(utcNow, TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId)));

    public bool CanBook(DateTime utc, DateTime now)
    {
        if (utc <= now) return false;
        var date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc,
            TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId)));
        return date <= Today(now).AddDays(BookingHorizonDays) && Slots(date).Contains(utc);
    }
}
