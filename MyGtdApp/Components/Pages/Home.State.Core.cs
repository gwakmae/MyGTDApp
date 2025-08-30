// 파일명: Components/Pages/Home.State.Core.cs
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Routing;
using MyGtdApp.Models;
using MyGtdApp.Services;
using MyGtdApp.Services.Undo;
using System;
using System.Collections.Generic;
using System.Linq;
using TaskStatus = MyGtdApp.Models.TaskStatus;

namespace MyGtdApp.Components.Pages;

public partial class Home
{
    /* ───────── DI & Navigation ───────── */
    [Inject] private ITaskService TaskService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private IGtdBoardJsService BoardJs { get; set; } = default!;
    [Inject] private NavigationManager NavManager { get; set; } = default!;
    [Inject] private IUndoService UndoService { get; set; } = default!;

    /* ───────── Route Parameter ───────── */
    [Parameter] public string? Context { get; set; }

    /* ───────── Data Collections ───────── */
    private List<TaskItem> allTopLevelTasks = new();
    private List<TaskItem> todayTasks = new();
    private List<TaskItem> contextTasks = new();
    private List<TaskItem> focusTasks = new();
    private List<TaskItem> activeTasks = new(); // ✨ 추가

    // Shift / Ctrl 범위 선택의 "정렬 기준 단일 평면" (중복 제거된 Id 시퀀스)
    private List<TaskItem> renderedTasks = new();

    /* ───────── UI / Interaction State ───────── */
    private bool canUndo = false;

    private TaskStatus? addingTaskStatus = null;
    private string newTaskTitle = "";
    private ElementReference quickAddInputRef;

    private int draggedTaskId = 0;
    private TaskStatus? dragOverStatus = null;

    private TaskItem? taskToEdit = null;
    private string pageTitle = "GTD Board";

    /* Multi-selection */
    private List<int> selectedTaskIds = new();
    private int? lastClickedTaskId = null;
    private bool isBulkEditPanelVisible = false;
    private bool isMultiSelectMode = false;

    /* View Helpers */
    // ✨ 수정: IsBoardView에 IsActiveTasksView 조건 추가
    private bool IsBoardView => !IsFocusView && !IsContextView && !IsActiveTasksView;
    private bool IsFocusView => NavManager.Uri.EndsWith("/focus", StringComparison.OrdinalIgnoreCase);
    private bool IsContextView => !string.IsNullOrEmpty(Context);
    private bool IsActiveTasksView => NavManager.Uri.EndsWith("/active", StringComparison.OrdinalIgnoreCase); // ✨ 추가

    private ElementReference boardContainerElement;

    /* ───────── Interop Helper ─────────
     * DotNetObjectReference 누수 방지: firstRender 생성 후 DisposeAsync에서 해제
     */
    private DotNetObjectReference<object>? _dotNetRef;
}
