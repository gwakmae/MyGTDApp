using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MyGtdApp.Models;
using MyGtdApp.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskStatus = MyGtdApp.Models.TaskStatus;

namespace MyGtdApp.Components.Pages
{
    public partial class Home : ComponentBase, IAsyncDisposable
    {
        /* ──────────────── DI ──────────────── */
        [Inject] private ITaskService TaskService { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] private IGtdBoardJsService BoardJs { get; set; } = default!;

        /* ──────────────── 라우트 파라미터 ──────────────── */
        [Parameter] public string? Context { get; set; }

        /* ──────────────── 데이터 ------------------------- */
        private List<TaskItem> allTopLevelTasks = new();
        private List<TaskItem> todayTasks = new();
        private List<TaskItem> contextTasks = new();

        /* ──────────────── UI 상태 ----------------------- */
        private bool hideCompleted = false;
        private TaskStatus? addingTaskStatus = null;
        private string newTaskTitle = "";
        private ElementReference quickAddInputRef;
        private int draggedTaskId = 0;
        private TaskStatus? dragOverStatus = null;
        private TaskItem? taskToEdit = null;

        /* ──────────────── 생명주기 ---------------------- */
        protected override async Task OnInitializedAsync()
        {
            // ❌ 이 줄 제거 - OnAfterRenderAsync에서 처리
            // await LoadHideCompletedState();

            await RefreshTasks();
            TaskService.OnChange += HandleTaskServiceChange;
        }

        protected override async Task OnParametersSetAsync() => await RefreshTasks();

        protected override async Task OnAfterRenderAsync(bool first)
        {
            if (first)
            {
                var helper = DotNetObjectReference.Create<object>(this);
                await BoardJs.SetupAsync(helper);

                // ✅ 첫 렌더링 후 localStorage 상태 로드
                await LoadHideCompletedState();
            }
        }

        /* ──────────────── 서비스 이벤트 ------------------ */
        private async void HandleTaskServiceChange()
        {
            await InvokeAsync(RefreshTasks);
        }

        /* ──────────────── 데이터 새로고침 --------------- */
        private async Task RefreshTasks()
        {
            if (string.IsNullOrEmpty(Context))
            {
                allTopLevelTasks = await TaskService.GetAllTasksAsync();
                todayTasks = await TaskService.GetTodayTasksAsync();
            }
            else
            {
                contextTasks = await TaskService.GetTasksByContextAsync($"@{Context}");
            }
            StateHasChanged();
        }

        /* ──────────────── 예시 핸들러 ------------------- */
        private async Task AddTask(string title)
        {
            await TaskService.AddTaskAsync(title, TaskStatus.Inbox, null);
            await RefreshTasks();
        }

        private async Task DeleteTask(int id)
        {
            await TaskService.DeleteTaskAsync(id);
            await RefreshTasks();
        }

        /* ──────────────── IAsyncDisposable -------------- */
        public ValueTask DisposeAsync()
        {
            TaskService.OnChange -= HandleTaskServiceChange;
            return ValueTask.CompletedTask;
        }
    }
}
