using Microsoft.JSInterop;
using MyGtdApp.Models;

namespace MyGtdApp.Components.Pages
{
    public partial class Home
    {
        private async Task LoadHideCompletedState()
        {
            try
            {
                var savedState = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "gtd-hideCompleted");
                if (!string.IsNullOrEmpty(savedState) && bool.TryParse(savedState, out bool parsedState))
                {
                    hideCompleted = parsedState;
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load hideCompleted state: {ex.Message}");
            }
        }

        private async Task SaveHideCompletedState()
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("localStorage.setItem", "gtd-hideCompleted", hideCompleted.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save hideCompleted state: {ex.Message}");
            }
        }

        // ✅ 누락된 메서드들 추가
        private async Task ToggleHideCompleted()
        {
            hideCompleted = !hideCompleted;
            await SaveHideCompletedState();
            StateHasChanged();
        }

        private IEnumerable<TaskItem> FilterCompleted(IEnumerable<TaskItem> src)
            => hideCompleted ? src.Where(t => !t.IsCompleted) : src;
    }
}
