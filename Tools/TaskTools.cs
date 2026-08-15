using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using TaskMcpServer.Models;
using TaskMcpServer.Services;

namespace TaskMcpServer.Tools;

[McpServerToolType]
public class TaskTools(TaskService taskService)
{
    [McpServerTool, Description("Add a new task. Priority: Low, Medium, or High. DueDate format: YYYY-MM-DD (optional). Category is optional.")]
    public async Task<string> AddTask(
        [Description("Title of the task")] string title,
        [Description("What the task involves")] string description,
        [Description("Priority level: Low, Medium, or High")] string priority = "Medium",
        [Description("Due date in YYYY-MM-DD format, e.g. 2025-12-31")] string? dueDate = null,
        [Description("Category label e.g. Work, Personal, Shopping")] string category = "")
    {
        if (!Enum.TryParse<Priority>(priority, ignoreCase: true, out var p))
            return $"Invalid priority '{priority}'. Use Low, Medium, or High.";

        DateTime? due = null;
        if (dueDate != null && DateTime.TryParse(dueDate, out var d))
            due = d.ToUniversalTime();

        var task = await taskService.AddAsync(title, description, p, due, category);
        return $"Task created!\nID: {task.Id}\nTitle: {task.Title}\nPriority: {task.Priority}\nDue: {(task.DueDate.HasValue ? task.DueDate.Value.ToString("yyyy-MM-dd") : "None")}\nCategory: {(string.IsNullOrEmpty(task.Category) ? "None" : task.Category)}";
    }

    [McpServerTool, Description("List all tasks with their IDs, priority, due dates, and status")]
    public async Task<string> ListTasks()
    {
        var tasks = await taskService.GetAllAsync();
        if (tasks.Count == 0)
            return "No tasks found.";

        var sb = new StringBuilder();
        foreach (var t in tasks.OrderBy(t => t.IsCompleted).ThenByDescending(t => t.Priority))
        {
            var status = t.IsCompleted ? "DONE" : "TODO";
            var due = t.DueDate.HasValue ? t.DueDate.Value.ToString("yyyy-MM-dd") : "No due date";
            var overdue = !t.IsCompleted && t.DueDate.HasValue && t.DueDate.Value < DateTime.UtcNow ? " ⚠ OVERDUE" : "";
            sb.AppendLine($"[{status}] [{t.Priority}] {t.Title}");
            sb.AppendLine($"  ID: {t.Id}");
            sb.AppendLine($"  {t.Description}");
            sb.AppendLine($"  Due: {due}{overdue} | Category: {(string.IsNullOrEmpty(t.Category) ? "None" : t.Category)}");
            if (!string.IsNullOrEmpty(t.Notes))
                sb.AppendLine($"  Notes: {t.Notes}");
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    [McpServerTool, Description("Update an existing task's title, description, priority, due date, category, or notes. Only provide the fields you want to change.")]
    public async Task<string> UpdateTask(
        [Description("Full ID of the task to update")] string id,
        [Description("New title (omit to keep current)")] string? title = null,
        [Description("New description (omit to keep current)")] string? description = null,
        [Description("New priority: Low, Medium, or High (omit to keep current)")] string? priority = null,
        [Description("New due date YYYY-MM-DD (omit to keep current)")] string? dueDate = null,
        [Description("New category (omit to keep current)")] string? category = null,
        [Description("Additional notes to store on the task")] string? notes = null)
    {
        Priority? p = null;
        if (priority != null)
        {
            if (!Enum.TryParse<Priority>(priority, ignoreCase: true, out var parsed))
                return $"Invalid priority '{priority}'. Use Low, Medium, or High.";
            p = parsed;
        }

        DateTime? due = null;
        if (dueDate != null && DateTime.TryParse(dueDate, out var d))
            due = d.ToUniversalTime();

        var task = await taskService.UpdateAsync(id, title, description, p, due, category, notes);
        return task == null
            ? $"No task found with ID: {id}"
            : $"Task updated!\nTitle: {task.Title}\nPriority: {task.Priority}\nDue: {(task.DueDate.HasValue ? task.DueDate.Value.ToString("yyyy-MM-dd") : "None")}\nCategory: {task.Category}";
    }

    [McpServerTool, Description("Mark a task as completed")]
    public async Task<string> CompleteTask(
        [Description("Full ID of the task to complete")] string id)
    {
        var success = await taskService.CompleteAsync(id);
        return success ? $"Task {id} marked as completed." : $"No task found with ID: {id}";
    }

    [McpServerTool, Description("Delete a task permanently")]
    public async Task<string> DeleteTask(
        [Description("Full ID of the task to delete")] string id)
    {
        var success = await taskService.DeleteAsync(id);
        return success ? $"Task {id} deleted." : $"No task found with ID: {id}";
    }

    [McpServerTool, Description("Search tasks by keyword across title, description, category, and notes")]
    public async Task<string> SearchTasks(
        [Description("Keyword to search for")] string query)
    {
        var results = await taskService.SearchAsync(query);
        if (results.Count == 0)
            return $"No tasks found matching '{query}'.";

        var sb = new StringBuilder($"Found {results.Count} task(s) matching '{query}':\n\n");
        foreach (var t in results)
        {
            sb.AppendLine($"[{(t.IsCompleted ? "DONE" : "TODO")}] [{t.Priority}] {t.Title} (ID: {t.Id})");
            sb.AppendLine($"  {t.Description}");
        }
        return sb.ToString().TrimEnd();
    }

    [McpServerTool, Description("Filter tasks by completion status, priority, or category")]
    public async Task<string> FilterTasks(
        [Description("Filter by status: 'pending', 'completed', or 'all'")] string status = "all",
        [Description("Filter by priority: Low, Medium, High, or empty for all")] string priority = "",
        [Description("Filter by category name, or empty for all")] string category = "")
    {
        bool? completed = status.ToLower() switch
        {
            "completed" => true,
            "pending" => false,
            _ => null
        };

        Priority? p = null;
        if (!string.IsNullOrWhiteSpace(priority) && Enum.TryParse<Priority>(priority, ignoreCase: true, out var parsed))
            p = parsed;

        var results = await taskService.FilterAsync(completed, p, string.IsNullOrWhiteSpace(category) ? null : category);
        if (results.Count == 0)
            return "No tasks match the given filters.";

        var sb = new StringBuilder($"{results.Count} task(s) found:\n\n");
        foreach (var t in results)
            sb.AppendLine($"[{(t.IsCompleted ? "DONE" : "TODO")}] [{t.Priority}] {t.Title} | Due: {(t.DueDate.HasValue ? t.DueDate.Value.ToString("yyyy-MM-dd") : "None")} | Category: {t.Category} | ID: {t.Id}");

        return sb.ToString().TrimEnd();
    }

    [McpServerTool, Description("Get a summary of all tasks: totals, completion rate, overdue count, and breakdown by priority")]
    public async Task<string> GetStats()
    {
        var s = await taskService.GetStatsAsync();
        var rate = s.Total > 0 ? (s.Completed * 100 / s.Total) : 0;
        return $"""
            Task Summary
            ────────────────────
            Total tasks   : {s.Total}
            Completed     : {s.Completed} ({rate}%)
            Pending       : {s.Pending}
            Overdue       : {s.Overdue}

            Pending by Priority
            ────────────────────
            High   : {s.High}
            Medium : {s.Medium}
            Low    : {s.Low}
            """;
    }

    [McpServerTool, Description("Delete all completed tasks at once to clean up the list")]
    public async Task<string> ClearCompleted()
    {
        var count = await taskService.ClearCompletedAsync();
        return count == 0 ? "No completed tasks to clear." : $"Cleared {count} completed task(s).";
    }
}
