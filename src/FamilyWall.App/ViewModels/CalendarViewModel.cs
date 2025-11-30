using FamilyWall.Core.Models;

namespace FamilyWall.App.ViewModels;

public class CalendarViewModel
{
    public enum ViewMode
    {
        Month,
        Week,
        Agenda
    }

    public ViewMode CurrentView { get; set; } = ViewMode.Month;
    public DateTime CurrentDate { get; set; } = DateTime.Today;
    public List<CalendarEvent> Events { get; set; } = new();
    public List<CalendarConfiguration> Calendars { get; set; } = new();
    public CalendarEvent? SelectedEvent { get; set; }
    public bool ShowEventDetails { get; set; }
    public bool ShowSettings { get; set; }
    public bool IsLoading { get; set; }
    public string? ErrorMessage { get; set; }

    // Date range helpers
    public DateTime MonthStart => new DateTime(CurrentDate.Year, CurrentDate.Month, 1);
    public DateTime MonthEnd => MonthStart.AddMonths(1).AddDays(-1);

    public DateTime WeekStart
    {
        get
        {
            var diff = CurrentDate.DayOfWeek - DayOfWeek.Sunday;
            return CurrentDate.AddDays(-diff).Date;
        }
    }

    public DateTime WeekEnd => WeekStart.AddDays(6);

    // Navigation helpers
    public void NextMonth() => CurrentDate = CurrentDate.AddMonths(1);
    public void PreviousMonth() => CurrentDate = CurrentDate.AddMonths(-1);
    public void NextWeek() => CurrentDate = CurrentDate.AddDays(7);
    public void PreviousWeek() => CurrentDate = CurrentDate.AddDays(-7);
    public void GoToToday() => CurrentDate = DateTime.Today;

    // Event helpers
    public List<CalendarEvent> GetEventsForDate(DateTime date)
    {
        return Events
            .Where(e => e.StartUtc.ToLocalTime().Date == date.Date)
            .OrderBy(e => e.StartUtc)
            .ToList();
    }

    public bool HasEventsOnDate(DateTime date)
    {
        return Events.Any(e => e.StartUtc.ToLocalTime().Date == date.Date);
    }

    public int GetEventCountForDate(DateTime date)
    {
        return Events.Count(e => e.StartUtc.ToLocalTime().Date == date.Date);
    }
}
