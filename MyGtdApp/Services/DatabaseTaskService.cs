using Microsoft.EntityFrameworkCore;
using MyGtdApp.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// [수정됨] 이름 충돌을 피하기 위해 using 구문을 명시적으로 사용합니다.
using TaskStatus = MyGtdApp.Models.TaskStatus;

namespace MyGtdApp.Services
{
    public class DatabaseTaskService : ITaskService
    {
        // [변경] DbContext 대신 DbContextFactory를 주입받습니다.
        private readonly IDbContextFactory<GtdDbContext> _dbContextFactory;

        public event System.Action? OnChange;

        // [변경] 생성자에서 DbContextFactory를 주입받습니다.
        public DatabaseTaskService(IDbContextFactory<GtdDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        private void NotifyStateChanged() => OnChange?.Invoke();

        // [변경] 모든 메서드에서 using 구문으로 context를 생성하도록 수정

        public async Task<TaskItem> AddTaskAsync(string title, Models.TaskStatus status, int? parentId)
        {
            using var context = _dbContextFactory.CreateDbContext();

            var maxSortOrder = await context.Tasks
                .Where(t => t.ParentId == parentId && t.Status == status)
                .Select(t => (int?)t.SortOrder)
                .MaxAsync() ?? -1;

            var newTask = new TaskItem
            {
                Title = title,
                Status = status,
                ParentId = parentId,
                SortOrder = maxSortOrder + 1
            };

            context.Tasks.Add(newTask);
            await context.SaveChangesAsync();
            NotifyStateChanged();
            return newTask;
        }

        public async Task DeleteTaskAsync(int taskId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var taskToDelete = await context.Tasks.FindAsync(taskId);
            if (taskToDelete != null)
            {
                await DeleteChildrenRecursive(context, taskId);
                context.Tasks.Remove(taskToDelete);
                await context.SaveChangesAsync();
                NotifyStateChanged();
            }
        }
        private async Task DeleteChildrenRecursive(GtdDbContext context, int parentId)
        {
            var children = await context.Tasks.Where(t => t.ParentId == parentId).ToListAsync();
            foreach (var child in children)
            {
                await DeleteChildrenRecursive(context, child.Id);
                context.Tasks.Remove(child);
            }
        }

        // [수정] GetAllTasksAsync: 재귀형 트리 변환 방식으로 전체 변경
        public async Task<List<TaskItem>> GetAllTasksAsync()
        {
            using var context = _dbContextFactory.CreateDbContext();

            // 1) 모든 Task 를 한 번에 가져온다
            var allTasks = await context.Tasks
                                         .AsNoTracking()    // 트래킹 안 해도 OK
                                         .OrderBy(t => t.SortOrder)
                                         .ToListAsync();

            // 2) 빠른 참조용 딕셔너리
            var lookup = allTasks.ToDictionary(t => t.Id);

            // 3) Children 컬렉션 초기화
            foreach (var t in allTasks) t.Children = new();

            // 4) 부모-자식 연결
            foreach (var t in allTasks)
            {
                if (t.ParentId.HasValue && lookup.TryGetValue(t.ParentId.Value, out var parent))
                {
                    parent.Children.Add(t);
                }
            }

            // 5) 정렬 & 재귀로 하위까지 정리
            void SortRecursive(TaskItem node)
            {
                node.Children = node.Children.OrderBy(c => c.SortOrder).ToList();
                foreach (var c in node.Children) SortRecursive(c);
            }

            var topLevel = allTasks.Where(t => t.ParentId == null)
                                   .OrderBy(t => t.SortOrder)
                                   .ToList();

            foreach (var root in topLevel) SortRecursive(root);

            return topLevel;
        }

        public async Task<List<string>> GetAllContextsAsync()
        {
            using var context = _dbContextFactory.CreateDbContext();

            // 1. 먼저 DB에서 모든 Task를 메모리로 가져옵니다. (단순한 요청)
            var allTasks = await context.Tasks.ToListAsync();

            // 2. 메모리로 가져온 데이터를 C# 코드로 가공합니다. (복잡한 작업)
            var allContexts = allTasks
                                           .SelectMany(t => t.Contexts)
                                           .Distinct()
                                           .OrderBy(c => c)
                                           .ToList(); // 이미 메모리에 있으므로 ToList() 사용

            return allContexts;
        }

        public async Task<List<TaskItem>> GetTasksByContextAsync(string context)
        {
            using var contextDb = _dbContextFactory.CreateDbContext();
            return await contextDb.Tasks
                .Where(t => !t.IsCompleted && t.Contexts.Contains(context))
                .OrderBy(t => t.Status)
                .ThenBy(t => t.SortOrder)
                .ToListAsync();
        }

        public async Task<List<TaskItem>> GetTodayTasksAsync()
        {
            using var context = _dbContextFactory.CreateDbContext();
            var today = System.DateTime.Today;
            return await context.Tasks.Where(t =>
                !t.IsCompleted &&
                t.StartDate.HasValue &&
                t.StartDate.Value.Date <= today
            ).OrderBy(t => t.DueDate ?? System.DateTime.MaxValue)
             .ThenByDescending(t => t.Priority)
             .ToListAsync();
        }

        public async Task MoveTaskAsync(
            int taskId,
            TaskStatus newStatus,
            int? newParentId,
            int newSortOrder)
        {
            using var context = _dbContextFactory.CreateDbContext();

            var taskToMove = await context.Tasks.FindAsync(taskId);
            if (taskToMove == null) return;

            /* ---------- 순환 방지 로직 추가 시작 ---------- */
            if (newParentId != null)
            {
                int cursorId = newParentId.Value;
                while (true)
                {
                    if (cursorId == taskId)
                    {
                        // 자기 자손에게 넣으려는 시도 → 무시하고 그냥 리턴
                        return;
                    }

                    var parentInfo = await context.Tasks
                                                     .AsNoTracking()
                                                     .Where(t => t.Id == cursorId)
                                                     .Select(t => new { t.ParentId })
                                                     .FirstOrDefaultAsync();

                    if (parentInfo?.ParentId == null) break; // 더 올라갈 부모 없음
                    cursorId = parentInfo.ParentId.Value;    // 한 단계 위로
                }
            }
            /* ---------- 순환 방지 로직 추가 끝 ---------- */

            var oldStatus = taskToMove.Status;

            // 원래 형제들의 SortOrder 재정렬
            var oldSiblings = await context.Tasks
                .Where(t => t.ParentId == taskToMove.ParentId
                         && t.Status == oldStatus
                         && t.Id != taskId)
                .OrderBy(t => t.SortOrder)
                .ToListAsync();
            for (int i = 0; i < oldSiblings.Count; i++)
                oldSiblings[i].SortOrder = i;

            // 이동
            taskToMove.ParentId = newParentId;
            taskToMove.Status = newStatus;

            // 새 위치 형제들 + 자기 자신 정렬
            var newSiblings = await context.Tasks
                .Where(t => t.ParentId == newParentId
                         && t.Status == newStatus
                         && t.Id != taskId)
                .OrderBy(t => t.SortOrder)
                .ToListAsync();

            newSortOrder = System.Math.Clamp(newSortOrder, 0, newSiblings.Count);
            newSiblings.Insert(newSortOrder, taskToMove);

            for (int i = 0; i < newSiblings.Count; i++)
                newSiblings[i].SortOrder = i;

            await context.SaveChangesAsync();
            NotifyStateChanged();
        }

        public async Task ToggleCompleteStatusAsync(int taskId)
        {
            using var context = _dbContextFactory.CreateDbContext();
            var task = await context.Tasks.FindAsync(taskId);
            if (task != null)
            {
                task.IsCompleted = !task.IsCompleted;
                task.Status = task.IsCompleted ? Models.TaskStatus.Completed : Models.TaskStatus.NextActions;
                await context.SaveChangesAsync();
                NotifyStateChanged();
            }
        }

        public async Task UpdateTaskAsync(TaskItem taskToUpdate)
        {
            using var context = _dbContextFactory.CreateDbContext();
            context.Tasks.Update(taskToUpdate);
            await context.SaveChangesAsync();
            NotifyStateChanged();
        }
    }
}