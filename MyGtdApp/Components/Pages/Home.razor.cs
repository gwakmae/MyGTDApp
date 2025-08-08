using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MyGtdApp.Models;
using MyGtdApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Routing;
using TaskStatus = MyGtdApp.Models.TaskStatus;

namespace MyGtdApp.Components.Pages
{
    public partial class Home : ComponentBase, IAsyncDisposable
    {
        /* ──────────────── DI ──────────────── */
        [Inject] private ITaskService TaskService { get; set; } = default!;
        [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] private IGtdBoardJsService BoardJs { get; set; } = default!;
        [Inject] private NavigationManager NavManager { get; set; } = default!;

        /* ──────────────── 라우트 파라미터 ──────────────── */
        [Parameter] public string? Context { get; set; }

        /* ──────────────── 데이터 ------------------------- */
        private List<TaskItem> allTopLevelTasks = new();
        private List<TaskItem> todayTasks = new();
        private List<TaskItem> contextTasks = new();
        private List<TaskItem> focusTasks = new();
        private List<TaskItem> renderedTasks = new();

        /* ──────────────── UI 상태 ----------------------- */
        private bool hideCompleted = false;
        private TaskStatus? addingTaskStatus = null;
        private string newTaskTitle = "";
        private ElementReference quickAddInputRef;
        private int draggedTaskId = 0;
        private TaskStatus? dragOverStatus = null;
        private TaskItem? taskToEdit = null;
        private string pageTitle = "GTD Board";

        /* 🆕 다중 선택 관련 상태 */
        private List<int> selectedTaskIds = new();
        private int? lastClickedTaskId = null;
        private bool isBulkEditPanelVisible = false;
        
        /* 🆕 뷰 상태 프로퍼티 */
        private bool IsBoardView => !IsFocusView && !IsContextView;
        private bool IsFocusView => NavManager.Uri.EndsWith("/focus", StringComparison.OrdinalIgnoreCase);
        private bool IsContextView => !string.IsNullOrEmpty(Context);
        
        /* ──────────────── 생명주기 ---------------------- */
        protected override async Task OnInitializedAsync()
        {
            NavManager.LocationChanged += HandleLocationChanged;
            await RefreshDataBasedOnRoute();
            TaskService.OnChange += HandleTaskServiceChange;
        }
        
        protected override async Task OnAfterRenderAsync(bool first)
        {
            if (first)
            {
                var helper = DotNetObjectReference.Create<object>(this);
                await BoardJs.SetupAsync(helper);
                await LoadHideCompletedState();
            }
        }
        
        /* ──────────────── 데이터 새로고침 --------------- */
        private async Task RefreshDataBasedOnRoute()
        {
            focusTasks = await TaskService.GetFocusTasksAsync();

            if (IsFocusView) { pageTitle = "Focus"; }
            else if (IsContextView)
            {
                pageTitle = $"Context: @{Context}";
                contextTasks = await TaskService.GetTasksByContextAsync($"@{Context}");
            }
            else 
            {
                pageTitle = "GTD Board";
                allTopLevelTasks = await TaskService.GetAllTasksAsync();
                todayTasks = await TaskService.GetTodayTasksAsync();
            }

            BuildRenderedTaskList();
            StateHasChanged();
        }
        
        private void BuildRenderedTaskList()
        {
            renderedTasks.Clear();
            IEnumerable<TaskItem> tasksToFlatten;

            if (IsFocusView) tasksToFlatten = focusTasks;
            else if (IsContextView) tasksToFlatten = contextTasks;
            else
            {
                var boardOrderedTasks = new List<TaskItem>();
                boardOrderedTasks.AddRange(todayTasks.OrderBy(t => t.SortOrder));
                foreach (var status in (TaskStatus[])Enum.GetValues(typeof(TaskStatus)))
                {
                    boardOrderedTasks.AddRange(GetTasksForStatus(status));
                }
                tasksToFlatten = boardOrderedTasks;
            }

            void Flatten(IEnumerable<TaskItem> tasks)
            {
                foreach (var task in tasks)
                {
                    renderedTasks.Add(task);
                    if (task.IsExpanded && task.Children.Any())
                    {
                        Flatten(task.Children.OrderBy(c => c.SortOrder));
                    }
                }
            }
            
            Flatten(tasksToFlatten);
        }

        private async void HandleTaskServiceChange()
        {
            await InvokeAsync(RefreshDataBasedOnRoute);
        }
        
        private void HandleTaskClick(int taskId, MouseEventArgs e)
        {
            if (e.ShiftKey && lastClickedTaskId.HasValue)
            {
                var lastIndex = renderedTasks.FindIndex(t => t.Id == lastClickedTaskId.Value);
                var currentIndex = renderedTasks.FindIndex(t => t.Id == taskId);

                if (lastIndex != -1 && currentIndex != -1)
                {
                    var startIndex = Math.Min(lastIndex, currentIndex);
                    var endIndex = Math.Max(lastIndex, currentIndex);
                    var rangeIds = renderedTasks.Skip(startIndex).Take(endIndex - startIndex + 1).Select(t => t.Id);

                    if (!e.CtrlKey) selectedTaskIds.Clear();
                    
                    foreach (var id in rangeIds) { if (!selectedTaskIds.Contains(id)) selectedTaskIds.Add(id); }
                }
            }
            else if (e.CtrlKey)
            {
                if (selectedTaskIds.Contains(taskId)) selectedTaskIds.Remove(taskId);
                else selectedTaskIds.Add(taskId);
            }
            else
            {
                selectedTaskIds.Clear();
                selectedTaskIds.Add(taskId);
            }

            lastClickedTaskId = taskId;
            isBulkEditPanelVisible = false; // 선택 변경 시 편집 패널은 항상 닫기
            StateHasChanged();
        }

        private void OpenBulkEditPanel() => isBulkEditPanelVisible = true;

        private async Task HandleBulkUpdate(BulkUpdateModel model)
        {
            if (model.TaskIds.Any()) await TaskService.BulkUpdateTasksAsync(model);
            CloseBulkEditPanel();
        }

        private void CloseBulkEditPanel() => isBulkEditPanelVisible = false;

        private async Task HandleDeleteSelected()
        {
            if (!selectedTaskIds.Any()) return;
            
            var confirmed = await JSRuntime.InvokeAsync<bool>("confirm", $"{selectedTaskIds.Count}개의 항목과 모든 하위 항목을 삭제하시겠습니까?");
            if (confirmed)
            {
                await TaskService.DeleteTasksAsync(new List<int>(selectedTaskIds));
                DeselectAll();
            }
        }

        private void DeselectAll()
        {
            selectedTaskIds.Clear();
            lastClickedTaskId = null;
            isBulkEditPanelVisible = false;
            StateHasChanged();
        }

        private async void HandleLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            await InvokeAsync(RefreshDataBasedOnRoute);
        }

        public ValueTask DisposeAsync()
        {
            TaskService.OnChange -= HandleTaskServiceChange;
            NavManager.LocationChanged -= HandleLocationChanged;
            return ValueTask.CompletedTask;
        }
    }
}