namespace FamilyWall.Core.Models;

public enum CalendarVisibility
{
    Private,      // Only owner can see
    Shared,       // Shared with specific users
    Public        // Anyone with link can view
}

public enum EventStatus
{
    Confirmed,
    Tentative,
    Cancelled
}

public enum EventResponseStatus
{
    NotResponded,
    Accepted,
    Declined,
    Tentative
}
