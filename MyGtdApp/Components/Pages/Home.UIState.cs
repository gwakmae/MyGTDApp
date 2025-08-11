using Microsoft.JSInterop;
using MyGtdApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace MyGtdApp.Components.Pages
{
    public partial class Home
    {
        // ✨ hideCompleted를 Home.razor.cs에서 여기로 이동
        private bool hideCompleted = false;

        // ✨ showHidden 정의
        private bool showHidden = false;

        private async Task LoadHideCompletedState()
        {
            try
            {
                var savedState = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "gtd-hideCompleted");
                if (!string.IsNullOrEmpty(savedState) && bool.TryParse(savedState, out bool parsedState))
                {
                    hideCompleted = parsedState;
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

        private async Task ToggleHideCompleted()
        {
            hideCompleted = !hideCompleted;
            await SaveHideCompletedState();
            StateHasChanged();
        }

        private async Task LoadShowHiddenState()
        {
            try
            {
                var savedState = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "gtd-showHidden");
                if (!string.IsNullOrEmpty(savedState) && bool.TryParse(savedState, out bool parsedState))
                {
                    showHidden = parsedState;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load showHidden state: {ex.Message}");
            }
        }

        private async Task SaveShowHiddenState()
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("localStorage.setItem", "gtd-showHidden", showHidden.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save showHidden state: {ex.Message}");
            }
        }

        private async Task ToggleShowHidden()
        {
            showHidden = !showHidden;
            await SaveShowHiddenState();
            StateHasChanged();
        }

        // ✨ 두 필터를 모두 처리하는 통합 필터 메서드
        private IEnumerable<TaskItem> FilterTasks(IEnumerable<TaskItem> src)
        {
            if (hideCompleted)
            {
                src = src.Where(t => !t.IsCompleted);
            }

            if (!showHidden)
            {
                src = src.Where(t => !t.IsHidden);
            }

            return src;
        }

        // ✨ 기존 FilterCompleted 메서드는 삭제하고 위의 FilterTasks로 통합합니다.
    }
}