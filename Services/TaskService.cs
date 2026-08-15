using System.Text.Json;
using TaskMcpServer.Models;

namespace TaskMcpServer.Services;

public class TaskService
{
    private readonly string _filePath = "tasks.json";
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public async Task<List<TaskItem>> GetAllAsync()
    {
        if (!File.Exists(_filePath))
            return new List<TaskItem>();

        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<List<TaskItem>>(json, _jsonOptions) ?? new List<TaskItem>();
    }

    public async Task SaveAllAsync(List<TaskItem> tasks)
    {
        var json = JsonSerializer.Serialize(tasks, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }

    public async Task<TaskItem> AddAsync(string title, string description, Priority priority, DateTime? dueDate, string category)
    {
        var tasks = await GetAllAsync();
        var task = new TaskItem
        {
            Title = title,
            Description = description,
            Priority = priority,
            DueDate = dueDate,
            Category = category
        };
        tasks.Add(task);
        await SaveAllAsync(tasks);
        return task;
    }

    public async Task<TaskItem?> UpdateAsync(string id, string? title, string? description, Priority? priority, DateTime? dueDate, string? category, string? notes)
    {
        var tasks = await GetAllAsync();
        var task = tasks.FirstOrDefault(t => t.Id == id);
        if (task == null) return null;

        if (title != null) task.Title = title;
        if (description != null) task.Description = description;
        if (priority != null) task.Priority = priority.Value;
        if (dueDate != null) task.DueDate = dueDate;
        if (category != null) task.Category = category;
        if (notes != null) task.Notes = notes;

        await SaveAllAsync(tasks);
        return task;
    }

    public async Task<bool> CompleteAsync(string id)
    {
        var tasks = await GetAllAsync();
        var task = tasks.FirstOrDefault(t => t.Id == id);
        if (task == null) return false;
        task.IsCompleted = true;
        await SaveAllAsync(tasks);
        return true;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var tasks = await GetAllAsync();
        var task = tasks.FirstOrDefault(t => t.Id == id);
        if (task == null) return false;
        tasks.Remove(task);
        await SaveAllAsync(tasks);
        return true;
    }

    public async Task<List<TaskItem>> SearchAsync(string query)
    {
        var tasks = await GetAllAsync();
        var q = query.ToLower();
        return tasks.Where(t =>
            t.Title.ToLower().Contains(q) ||
            t.Description.ToLower().Contains(q) ||
            t.Category.ToLower().Contains(q) ||
            t.Notes.ToLower().Contains(q)).ToList();
    }

    public async Task<List<TaskItem>> FilterAsync(bool? completed, Priority? priority, string? category)
    {
        var tasks = await GetAllAsync();

        if (completed.HasValue)
            tasks = tasks.Where(t => t.IsCompleted == completed.Value).ToList();
        if (priority.HasValue)
            tasks = tasks.Where(t => t.Priority == priority.Value).ToList();
        if (!string.IsNullOrWhiteSpace(category))
            tasks = tasks.Where(t => t.Category.ToLower() == category.ToLower()).ToList();

        return tasks;
    }

    public async Task<(int Total, int Completed, int Pending, int Overdue, int High, int Medium, int Low)> GetStatsAsync()
    {
        var tasks = await GetAllAsync();
        var now = DateTime.UtcNow;
        var pending = tasks.Where(t => !t.IsCompleted).ToList();

        return (
            Total: tasks.Count,
            Completed: tasks.Count(t => t.IsCompleted),
            Pending: pending.Count,
            Overdue: pending.Count(t => t.DueDate.HasValue && t.DueDate.Value < now),
            High: pending.Count(t => t.Priority == Priority.High),
            Medium: pending.Count(t => t.Priority == Priority.Medium),
            Low: pending.Count(t => t.Priority == Priority.Low)
        );
    }

    public async Task<int> ClearCompletedAsync()
    {
        var tasks = await GetAllAsync();
        var count = tasks.Count(t => t.IsCompleted);
        await SaveAllAsync(tasks.Where(t => !t.IsCompleted).ToList());
        return count;
    }
}
