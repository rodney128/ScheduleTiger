using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ScheduleTiger.Models;

public sealed class ReminderItem : INotifyPropertyChanged
{
    string title = string.Empty;
    string notes = string.Empty;
    string tag = "Other";
    DateTime dueDateTime = DateTime.Now;
    bool isDone;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Title
    {
        get => title;
        set
        {
            if (title == value)
            {
                return;
            }

            title = value;
            OnPropertyChanged();
        }
    }

    public string Notes
    {
        get => notes;
        set
        {
            if (notes == value)
            {
                return;
            }

            notes = value;
            OnPropertyChanged();
        }
    }

    public string Tag
    {
        get => tag;
        set
        {
            if (tag == value)
            {
                return;
            }

            tag = value;
            OnPropertyChanged();
        }
    }

    public DateTime DueDateTime
    {
        get => dueDateTime;
        set
        {
            if (dueDateTime == value)
            {
                return;
            }

            dueDateTime = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DueDateDisplay));
            OnPropertyChanged(nameof(IsOverdue));
        }
    }

    public bool IsDone
    {
        get => isDone;
        set
        {
            if (isDone == value)
            {
                return;
            }

            isDone = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsOverdue));
        }
    }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public string DueDateDisplay => DueDateTime.ToString("ddd, MMM d • h:mm tt");

    public bool IsOverdue => !IsDone && DueDateTime < DateTime.Now;

    public event PropertyChangedEventHandler? PropertyChanged;

    void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}