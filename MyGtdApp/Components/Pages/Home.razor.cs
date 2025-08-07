using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MyGtdApp.Models;
using MyGtdApp.Services;
using System.Collections.Generic;
using System.Linq;
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
        
        // 🆕 추가: 화면에 렌더링된 순서대로 모든 Task를 담는 리스트 (Shift 선택용)
        private List<TaskItem> renderedTasks = new();

        /* ──────────────── UI 상태 ----------------------- */
        private bool hideCompleted = false;
        private TaskStatus? addingTaskStatus = null;
        private string newTaskTitle = "";
        private ElementReference quickAddInputRef;
        private int draggedTaskId = 0;
        private TaskStatus? dragOverStatus = null;
        private TaskItem? taskToEdit = null;

        /* 🆕 추가: 다중 선택 관련 상태 */
        private List<int> selectedTaskIds = new();
        private int? lastClickedTaskId = null;

        /* ──────────────── 생명주기 ---------------------- */
        protected override async Task OnInitializedAsync()
        {
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
                await LoadHideCompletedState();
            }
        }
        
        private void BuildRenderedTaskList()
        {
            renderedTasks.Clear();
            var allTasks = string.IsNullOrEmpty(Context) ? allTopLevelTasks : contextTasks;

            void Flatten(IEnumerable<TaskItem> tasks)
            {
                foreach (var task in tasks)
                {
                    renderedTasks.Add(task);
                    if (task.IsExpanded)
                    {
                        Flatten(task.Children.OrderBy(c => c.SortOrder));
                    }
                }
            }

            if (string.IsNullOrEmpty(Context))
            {
                // Today 컬럼
                Flatten(todayTasks.OrderBy(t => t.SortOrder));
                // 나머지 상태 컬럼
                foreach (var status in (TaskStatus[])Enum.GetValues(typeof(TaskStatus)))
                {
                    Flatten(GetTasksForStatus(status));
                }
            }
            else
            {
                Flatten(allTasks.OrderBy(t => t.SortOrder));
            }
        }

        /* ──────────────── 서비스 이벤트 ------------------ */
        private async void HandleTaskServiceChange()
        {
            await InvokeAsync(async () =>
            {
                await RefreshTasks();
                // 🔄 변경: 데이터 변경 시 선택 상태 초기화
                selectedTaskIds.Clear();
                lastClickedTaskId = null;
                StateHasChanged();
            });
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
            BuildRenderedTaskList(); // 렌더링 순서 리스트 업데이트
            StateHasChanged();
        }

        /* ──────────────── 다중 선택 핸들러 ------------------- */
        private void HandleTaskClick(int taskId, MouseEventArgs e)
        {
            if (e.ShiftKey && lastClickedTaskId.HasValue)
            {
                // Shift 클릭 로직
                var lastIndex = renderedTasks.FindIndex(t => t.Id == lastClickedTaskId.Value);
                var currentIndex = renderedTasks.FindIndex(t => t.Id == taskId);

                if (lastIndex != -1 && currentIndex != -1)
                {
                    var startIndex = Math.Min(lastIndex, currentIndex);
                    var endIndex = Math.Max(lastIndex, currentIndex);
                    var rangeIds = renderedTasks.Skip(startIndex).Take(endIndex - startIndex + 1).Select(t => t.Id);

                    if (!e.CtrlKey)
                    {
                        selectedTaskIds.Clear();
                    }
                    
                    foreach (var id in rangeIds)
                    {
                        if (!selectedTaskIds.Contains(id))
                        {
                            selectedTaskIds.Add(id);
                        }
                    }
                }
            }
            else if (e.CtrlKey)
            {
                // Ctrl 클릭 로직
                if (selectedTaskIds.Contains(taskId))
                {
                    selectedTaskIds.Remove(taskId);
                }
                else
                {
                    selectedTaskIds.Add(taskId);
                }
            }
            else
            {
                // 일반 클릭 로직
                selectedTaskIds.Clear();
                selectedTaskIds.Add(taskId);
            }

            lastClickedTaskId = taskId;
            StateHasChanged();
        }


        /* ──────────────── 예시 핸들러 (기존) ------------------- */
        private async Task AddTask(string title) => await TaskService.AddTaskAsync(title, TaskStatus.Inbox, null);
        private async Task DeleteTask(int id) => await TaskService.DeleteTaskAsync(id);

        /* ──────────────── IAsyncDisposable -------------- */
        public ValueTask DisposeAsync()
        {
            TaskService.OnChange -= HandleTaskServiceChange;
            return ValueTask.CompletedTask;
        }
    }
}