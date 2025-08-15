// 파일명: Components/Pages/Home.UIState.cs
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
        private bool hideCompleted = false;
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
            // ✨ 수정: 데이터를 다시 로드하여 변경사항을 완벽하게 반영
            await RefreshDataBasedOnRoute();
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
            // ✨ 수정: 데이터를 다시 로드하여 변경사항을 완벽하게 반영
            await RefreshDataBasedOnRoute();
        }

        private IEnumerable<TaskItem> FilterTasks(IEnumerable<TaskItem> src)
        {
            if (hideCompleted)
            {
                src = src.Where(t => !t.IsCompleted);
            }

            // ✨ 참고: ActiveTasksView는 이미 서비스단에서 hidden 필터링이 완료되었으므로,
            // 이 메서드는 Board View 등 다른 뷰에만 영향을 미칩니다.
            if (!showHidden)
            {
                src = src.Where(t => !t.IsHidden);
            }

            return src;
        }
    }
}