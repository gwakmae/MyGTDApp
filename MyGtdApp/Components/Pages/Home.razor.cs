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

        private bool isMultiSelectMode = false; // 🆕 추가: 다중 선택 모드

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

                // 🆕 추가: 키보드 이벤트 등록
                await JSRuntime.InvokeVoidAsync("setupKeyboardHandlers", DotNetObjectReference.Create(this));
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

        // ───── 🚀 [핵심 수정] HandleTaskClick 메서드 전체를 이 코드로 교체하세요 ─────
        private void HandleTaskClick(int taskId, MouseEventArgs e)
        {
            // 데스크탑 더블클릭은 여기서 처리 (모바일 더블탭은 JS가 처리)
            if (e.Detail == 2)
            {
                ShowEditModal(taskId);
                return;
            }

            // === 다중 선택 모드 (모바일/데스크탑 공통) ===
            if (isMultiSelectMode || e.CtrlKey || e.ShiftKey)
            {
                // Shift 키를 사용한 범위 선택 (데스크탑)
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

                        foreach (var id in rangeIds)
                        {
                            if (!selectedTaskIds.Contains(id)) selectedTaskIds.Add(id);
                        }
                    }
                }
                else // Ctrl 키 또는 모바일 다중 선택 모드에서의 개별 토글
                {
                    if (selectedTaskIds.Contains(taskId))
                    {
                        selectedTaskIds.Remove(taskId);
                    }
                    else
                    {
                        selectedTaskIds.Add(taskId);
                    }
                }
            }
            else // === 일반 클릭 (선택되지 않은 항목 클릭) ===
            {
                if (selectedTaskIds.Any())
                {
                    selectedTaskIds.Clear();
                }
            }

            lastClickedTaskId = taskId;
            isBulkEditPanelVisible = false;
            StateHasChanged();
        }

        // 🆕 모바일 디바이스 감지 개선
        private async Task<bool> IsMobileDevice()
        {
            try
            {
                return await JSRuntime.InvokeAsync<bool>("isMobileDevice");
            }
            catch
            {
                return false;
            }
        }

        // 🆕 추가: ESC 키 처리
        [JSInvokable]
        public void HandleEscapeKey()
        {
            if (selectedTaskIds.Any() || isBulkEditPanelVisible)
            {
                DeselectAll();
                Console.WriteLine("[SELECTION] ESC 키로 선택 해제");
            }
        }

        // 🆕 추가: 빈 공간 클릭 처리  
        [JSInvokable]
        public void HandleBackgroundClick()
        {
            if (selectedTaskIds.Any() || isBulkEditPanelVisible)
            {
                DeselectAll();
                Console.WriteLine("[SELECTION] 빈 공간 클릭으로 선택 해제");
            }
        }

        // JSInvokable 메서드 수정
        [JSInvokable]
        public async Task EnterSelectionMode(int taskId)
        {
            // 🚀 [수정] JavaScript 로직을 신뢰하고 불필요한 JS interop 확인 제거

            // 선택 모드 진입 로직
            isMultiSelectMode = true; // ✅ 다중 선택 모드 플래그 활성화

            if (!selectedTaskIds.Contains(taskId))
            {
                selectedTaskIds.Add(taskId);
            }
            lastClickedTaskId = taskId;

            Console.WriteLine($"[SELECTION] 멀티터치로 선택 모드 진입: Task {taskId}");
            await InvokeAsync(StateHasChanged);
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
            isMultiSelectMode = false; // ✅ 다중 선택 모드 플래그 비활성화 추가
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

            // 🆕 추가: 키보드 이벤트 정리
            _ = JSRuntime.InvokeVoidAsync("cleanupKeyboardHandlers");

            return ValueTask.CompletedTask;
        }

        // ──────────────── [추가] 전체 선택/해제 도우미 메서드 ────────────────
        [JSInvokable]
        public void SelectAllTasks()
        {
            selectedTaskIds.Clear();
            selectedTaskIds.AddRange(renderedTasks.Select(t => t.Id));
            Console.WriteLine($"[SELECTION] 전체 선택: {selectedTaskIds.Count}개 항목");
            StateHasChanged();
        }

        private void ClearAllSelections()
        {
            selectedTaskIds.Clear();
            isBulkEditPanelVisible = false;
            lastClickedTaskId = null;
            StateHasChanged();
        }

        // 🆕 JavaScript와 C#에서 모두 호출 가능한 통합 ShowEditModal 메서드
        [JSInvokable]
        public void ShowEditModal(int taskId)
        {
            Console.WriteLine($"[MODAL] 모달 열기 요청: Task {taskId}");

            // 🔽 Focus 뷰의 Task도 찾을 수 있도록 로직 보강
            taskToEdit = FindTaskById(allTopLevelTasks, taskId) ??
                         FindTaskById(contextTasks, taskId) ??
                         FindTaskById(focusTasks, taskId);

            if (taskToEdit != null)
            {
                Console.WriteLine($"[MODAL] 모달 열기 성공: {taskToEdit.Title}");
                StateHasChanged();
            }
            else
            {
                Console.WriteLine($"[MODAL] Task {taskId}를 찾을 수 없음");
            }
        }
    }
}
