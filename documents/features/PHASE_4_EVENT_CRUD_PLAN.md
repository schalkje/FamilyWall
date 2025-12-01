# Phase 4: Event Creation & Editing - Implementation Plan

**Date:** November 30, 2025
**Priority:** High - User Requested
**Status:** Planning

---

## 🎯 Goals

Enable users to create, edit, and manage calendar events with an intuitive UI following industry best practices from Outlook and Google Calendar.

---

## 📋 Requirements

### 1. Event Details Navigation
- ✅ Click event from any view → Navigate to full-screen event details
- ✅ Event details show all information
- 🔲 Event details allow editing (inline or edit mode)
- 🔲 Save/Cancel buttons for changes
- 🔲 Delete event option

### 2. Event Editing Capabilities
**Editable Fields:**
- 🔲 **Title** - Text input
- 🔲 **Start Date/Time** - Date & time picker
- 🔲 **End Date/Time** - Date & time picker
- 🔲 **Duration** - Auto-calculated or manual override
- 🔲 **Description** - Rich text area
- 🔲 **Who it's for** - Family member selector (multi-select)
- 🔲 **Location** - Text input with suggestions
- 🔲 **All-Day toggle** - Checkbox
- 🔲 **Calendar selection** - Dropdown (which calendar to save to)
- 🔲 **Color override** - Optional event-specific color

### 3. Day View Navigation
- 🔲 Click day label/number in Month view → Navigate to Day view
- 🔲 Day view shows single day with hourly slots (24 hours or business hours)
- 🔲 Current day highlighted
- 🔲 Previous/Next day navigation
- 🔲 "Today" button to jump back

### 4. Event Creation Methods

#### Method 1: Add Event Button
- 🔲 "+" or "New Event" button in header
- 🔲 Opens event creation form
- 🔲 Defaults to current date/time or selected date
- 🔲 All fields editable

#### Method 2: Click Time Slot (Day View)
- 🔲 Click empty time slot → Create event at that time
- 🔲 Default duration: 1 hour
- 🔲 Quick inline title entry
- 🔲 Click to expand to full form

#### Method 3: Click Day (Month/Week View)
- 🔲 Click day number → Navigate to day view
- 🔲 Option to quick-add all-day event

---

## 🏗️ Architecture Design

### Navigation Flow

```
Month/Week/Agenda View
    │
    ├─ Click Event → Event Details Page (Read/Edit Mode)
    │                     │
    │                     ├─ Edit Mode
    │                     ├─ Save Changes
    │                     └─ Delete Event
    │
    └─ Click Day Label → Day View
                             │
                             ├─ View hourly schedule
                             ├─ Add Event Button → Event Creation Form
                             └─ Click Time Slot → Quick Event Creation
```

### Component Structure

```
Pages/
  ├─ Calendar.razor (Month/Week/Agenda views)
  ├─ EventDetails.razor (NEW - Full page event view)
  └─ DayView.razor (NEW - Single day hourly view)

Components/Calendar/
  ├─ MonthViewSimple.razor (existing)
  ├─ WeekView.razor (existing)
  ├─ AgendaView.razor (existing)
  ├─ DayViewDetailed.razor (NEW - Hourly day view)
  ├─ EventEditForm.razor (NEW - Event editing form)
  ├─ EventQuickCreate.razor (NEW - Quick event creation)
  └─ FamilyMemberSelector.razor (NEW - Who is it for)
```

---

## 🎨 UI Design Specifications

### Event Details Page

**Layout: Full Page**

```
┌────────────────────────────────────────────────────────────┐
│ ← Back    Event Details                      Edit  Delete  │
├────────────────────────────────────────────────────────────┤
│                                                            │
│  [Title Input Field]                                       │
│  ───────────────────────────────────────────────────       │
│                                                            │
│  📅 Start                                                  │
│  [Date Picker] [Time Picker]                               │
│                                                            │
│  📅 End                                                    │
│  [Date Picker] [Time Picker]    Duration: 1 hour          │
│                                                            │
│  ☐ All-day event                                           │
│                                                            │
│  📍 Location                                               │
│  [Text Input with autocomplete]                            │
│                                                            │
│  👥 Who is this for?                                       │
│  [☑ Dad  ☑ Mom  ☑ Child1  ☐ Child2]                       │
│                                                            │
│  📆 Calendar                                               │
│  [Dropdown: Personal ▼]  🔵                                │
│                                                            │
│  📝 Description                                            │
│  ┌────────────────────────────────────────────────────┐   │
│  │ [Rich text area]                                   │   │
│  │                                                    │   │
│  └────────────────────────────────────────────────────┘   │
│                                                            │
│  🎨 Event Color (Optional)                                 │
│  🔵 🔴 🟢 🟡 🟣 🟠                                          │
│                                                            │
├────────────────────────────────────────────────────────────┤
│                    [Cancel]  [Save Event]                  │
└────────────────────────────────────────────────────────────┘
```

### Day View

**Layout: Full Page with Hourly Slots**

```
┌────────────────────────────────────────────────────────────┐
│ ◀  Friday, December 1, 2025  ▶         Today  [+ New Event]│
├────────────────────────────────────────────────────────────┤
│ All Day │ Family Dinner 🔴                                 │
├─────────┼──────────────────────────────────────────────────┤
│ 12 AM   │                                                  │
├─────────┤                                                  │
│  1 AM   │                                                  │
├─────────┤                                                  │
│  2 AM   │                                                  │
├─────────┤  (Collapsed - Click to expand)                   │
│  ...    │                                                  │
├─────────┤                                                  │
│  6 AM   │                                                  │
├─────────┼──────────────────────────────────────────────────┤
│  7 AM   │                                                  │
├─────────┤                                                  │
│  8 AM   │ ┌──────────────────────────────────────────┐    │
│         │ │ Morning Standup 🔵                       │    │
├─────────┤ │ 8:00 AM - 8:30 AM                        │    │
│  9 AM   │ └──────────────────────────────────────────┘    │
│         │ [+ Click to add event]                          │
├─────────┼──────────────────────────────────────────────────┤
│ 10 AM   │ ┌──────────────────────────────────────────┐    │
│         │ │ Doctor Appointment 🟢                    │    │
│         │ │ 10:00 AM - 11:30 AM                      │    │
├─────────┤ │ Dr. Smith's Office                       │    │
│ 11 AM   │ │ Mom, Child1                              │    │
│         │ └──────────────────────────────────────────┘    │
├─────────┼──────────────────────────────────────────────────┤
│ 12 PM   │ [+ Click to add event]                          │
├─────────┤                                                  │
│  ...    │                                                  │
└────────────────────────────────────────────────────────────┘
```

### Quick Event Creation (Click Time Slot)

```
┌────────────────────────────────────────┐
│ New Event - 9:00 AM              [×]  │
├────────────────────────────────────────┤
│ [Enter event title...]                 │
│                                        │
│ ⏰ 9:00 AM - 10:00 AM (1 hour)         │
│                                        │
│ [Quick Add]  [More Options...]         │
└────────────────────────────────────────┘
```

---

## 🔧 Technical Implementation

### 1. Navigation Setup

**Add Routing:**
```csharp
// App.razor or Routes.razor
<Route @path="/calendar/event/{eventId:int}" Component="@typeof(EventDetails)" />
<Route @path="/calendar/day/{date:datetime}" Component="@typeof(DayView)" />
<Route @path="/calendar/event/new" Component="@typeof(EventDetails)" />
```

### 2. Event Service Extensions

**Add to ICalendarEventService:**
```csharp
// Event CRUD operations
Task<CalendarEvent> CreateEventAsync(
    CalendarEvent calendarEvent,
    CancellationToken cancellationToken = default);

Task<CalendarEvent> UpdateEventAsync(
    CalendarEvent calendarEvent,
    CancellationToken cancellationToken = default);

Task DeleteEventAsync(
    int id,
    CancellationToken cancellationToken = default);

// Validation
Task<ValidationResult> ValidateEventAsync(CalendarEvent calendarEvent);
```

### 3. New Models/DTOs

**EventEditDto:**
```csharp
public class EventEditDto
{
    public int? Id { get; set; } // Null for new events
    public required string Title { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public bool IsAllDay { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public int CalendarId { get; set; }
    public List<string> FamilyMembers { get; set; } = new(); // "Dad", "Mom", etc.
    public string? ColorOverride { get; set; }
}
```

**FamilyMember:**
```csharp
public class FamilyMember
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string? AvatarUrl { get; set; }
    public string Color { get; set; } = "#3788D8";
}
```

### 4. State Management

**EventEditViewModel:**
```csharp
public class EventEditViewModel
{
    public EventEditDto Event { get; set; } = new();
    public List<CalendarConfiguration> AvailableCalendars { get; set; } = new();
    public List<FamilyMember> FamilyMembers { get; set; } = new();
    public bool IsEditing { get; set; }
    public bool IsSaving { get; set; }
    public string? ErrorMessage { get; set; }

    // Validation
    public Dictionary<string, string> ValidationErrors { get; set; } = new();

    // Computed properties
    public TimeSpan Duration => Event.EndUtc - Event.StartUtc;
    public bool IsValid => !ValidationErrors.Any();
}
```

---

## 📝 Implementation Tasks

### Phase 4A: Event Details & Editing (Priority 1)

1. **Create EventDetails.razor Page**
   - Route: `/calendar/event/{eventId}`
   - Full-page layout with back button
   - Display all event information
   - Edit mode toggle

2. **Create EventEditForm.razor Component**
   - Reusable form for create/edit
   - All field inputs with validation
   - Date/time pickers
   - Duration calculation
   - Family member selector

3. **Implement Navigation from Calendar Views**
   - Update MonthView: Click event → Navigate to details
   - Update WeekView: Click event → Navigate to details
   - Update AgendaView: Click event → Navigate to details

4. **Backend: Event Update Service**
   - Implement UpdateEventAsync in CalendarEventService
   - Add validation logic
   - Handle optimistic concurrency

### Phase 4B: Day View (Priority 2)

5. **Create DayView.razor Page**
   - Route: `/calendar/day/{date}`
   - Hourly time slots (configurable: 24h or business hours)
   - All-day events section
   - Current time indicator
   - Navigation: Previous/Next/Today

6. **Create DayViewDetailed.razor Component**
   - Reusable day view component
   - Scrollable time slots
   - Event positioning by time
   - Empty slot click handlers

7. **Implement Day Navigation**
   - Update MonthView: Click day number → Navigate to day view
   - Update WeekView: Click day header → Navigate to day view

### Phase 4C: Event Creation (Priority 3)

8. **Create EventQuickCreate.razor Component**
   - Modal/popup for quick event creation
   - Minimal fields: title, time, duration
   - "More options" button → Full form

9. **Implement "New Event" Button**
   - Add to header in all views
   - Opens event creation form
   - Pre-fill with selected date/time

10. **Implement Click-to-Create in Day View**
    - Click empty time slot → Quick create popup
    - Default 1-hour duration
    - Quick save or expand to full form

11. **Backend: Event Creation Service**
    - Implement CreateEventAsync in CalendarEventService
    - Validate event doesn't conflict
    - Support both local and Graph API calendars

### Phase 4D: Family Member Support (Priority 4)

12. **Create FamilyMemberSelector.razor Component**
    - Multi-select checkboxes
    - Avatar display
    - Color coding

13. **Add FamilyMember Model**
    - Database schema update
    - CRUD operations
    - Link to events

14. **Family Member Management UI**
    - Settings panel to add/edit family members
    - Color and avatar customization

### Phase 4E: Event Deletion (Priority 5)

15. **Implement Delete Functionality**
    - Delete button on event details page
    - Confirmation dialog
    - Backend DeleteEventAsync service
    - Cascade delete or soft delete

---

## 🎯 Success Criteria

✅ Users can click any event and navigate to a full event details page
✅ Event details page allows editing all key fields
✅ Users can save changes to existing events
✅ Users can click a day and see a detailed hourly day view
✅ Users can create new events via "New Event" button
✅ Users can create new events by clicking time slots in day view
✅ Users can select which family members an event applies to
✅ All changes sync back to Microsoft Graph (if applicable)
✅ UI follows industry best practices (Outlook/Google Calendar)

---

## 🚀 Estimated Timeline

- **Phase 4A (Event Details & Editing)**: 2-3 hours
- **Phase 4B (Day View)**: 2-3 hours
- **Phase 4C (Event Creation)**: 2-3 hours
- **Phase 4D (Family Member Support)**: 1-2 hours
- **Phase 4E (Event Deletion)**: 30 minutes

**Total: 8-12 hours of development**

---

## 📚 Best Practices to Follow

### Outlook Calendar
- **Inline editing** - Click field to edit directly
- **Auto-save** - Save as you type (with debounce)
- **Quick actions** - Right-click context menus
- **Smart defaults** - Intelligent time suggestions

### Google Calendar
- **Clean UI** - Minimal, focused design
- **Drag & drop** - Move events by dragging (Phase 5)
- **Color coding** - Visual calendar organization
- **Quick add** - Natural language event creation (Phase 6)

### General Best Practices
- **Keyboard shortcuts** - Tab navigation, Enter to save
- **Validation feedback** - Real-time error messages
- **Loading states** - Show saving/loading indicators
- **Undo support** - Option to undo recent changes
- **Offline support** - Queue changes when offline (Phase 5)

---

## 🔄 Next Steps

1. **Confirm requirements** with user
2. **Start with Phase 4A** - Event details and editing
3. **Implement navigation** from existing views
4. **Add Day View** for better single-day management
5. **Enable event creation** with multiple entry points
6. **Add family member support** for event attribution

---

**This plan provides a complete event management system with intuitive UI for creating, editing, and organizing family calendar events!** 🎉
