namespace MyGtdApp.Services;

public interface ITaskDataService
{
    Task<string> ExportTasksToJsonAsync();
    Task ImportTasksFromJsonAsync(string jsonData);
}
