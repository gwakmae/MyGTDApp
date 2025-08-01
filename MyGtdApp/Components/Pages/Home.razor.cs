using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;                 // ⬅ JSRuntime
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
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;   // ⬅ 추가
        [Inject] private IGtdBoardJsService BoardJs { get; set; } = default!; // ⬅ 추가

        /* ──────────────── 라우트 파라미터 ──────────────── */
        [Parameter] public string? Context { get; set; }

        /* ──────────────── 데이터 ------------------------- */
        private List<TaskItem> allTopLevelTasks = new();
        private List<TaskItem> todayTasks = new();
        private List<TaskItem> contextTasks = new();

        /* ──────────────── UI 상태 ----------------------- */
        private bool hideCompleted = false;          // 완료 항목 숨김 여부
        private TaskStatus? addingTaskStatus = null;          // 현재 “빠른 추가” 컬럼
        private string newTaskTitle = "";             // 빠른 추가 입력값
        private ElementReference quickAddInputRef;            // 빠른 추가 입력 박스
        private int draggedTaskId = 0;              // D&D : 드래그 중 ID
        private TaskStatus? dragOverStatus = null;           // D&D : 마우스 오버 컬럼
        private TaskItem? taskToEdit = null;           // 편집 모달 대상

        /* ──────────────── 생명주기 ---------------------- */
        protected override async Task OnInitializedAsync()
        {
            await LoadHideCompletedState();    // ← 추가

            await RefreshTasks();              // 기존 코드
            TaskService.OnChange += HandleTaskServiceChange;
        }

        protected override async Task OnParametersSetAsync() => await RefreshTasks();

        protected override async Task OnAfterRenderAsync(bool first)
        {
            if (first)
            {
                var helper = DotNetObjectReference.Create<object>(this);
                await BoardJs.SetupAsync(helper);
            }
        }

        /* ──────────────── 서비스 이벤트 ------------------ */
        private async void HandleTaskServiceChange()
        {
            await InvokeAsync(RefreshTasks);   // 깔끔하게 한 줄
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
        public async ValueTask DisposeAsync()
        {
            TaskService.OnChange -= HandleTaskServiceChange;
            await BoardJs.DisposeAsync();
        }
    }
}
