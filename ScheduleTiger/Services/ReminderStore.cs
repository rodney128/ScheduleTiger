using System.Text.Json;
using ScheduleTiger.Models;

namespace ScheduleTiger.Services;

public sealed class ReminderStore
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    readonly SemaphoreSlim gate = new(1, 1);
    readonly string filePath = Path.Combine(FileSystem.AppDataDirectory, "scheduletiger-reminders.json");

    public async Task<IReadOnlyList<ReminderItem>> LoadAsync()
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        await using FileStream stream = File.OpenRead(filePath);
        var reminders = await JsonSerializer.DeserializeAsync<List<ReminderItem>>(stream, JsonOptions);
        return reminders ?? [];
    }

    public async Task SaveAsync(IEnumerable<ReminderItem> reminders)
    {
        ArgumentNullException.ThrowIfNull(reminders);

        await gate.WaitAsync();
        try
        {
            Directory.CreateDirectory(FileSystem.AppDataDirectory);
            await using FileStream stream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(stream, reminders, JsonOptions);
        }
        finally
        {
            gate.Release();
        }
    }
}