using System;
using System.Threading.Tasks;

namespace MyGtdApp.Services.Undo
{
    // 취소할 작업의 정보(설명, 복원 로직 등)를 담는 클래스입니다.
    public sealed class UndoAction
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public UndoActionType Type { get; init; }
        public string Description { get; init; } = "";
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public Func<Task> UndoAsync { get; init; } = default!;
    }
}