using System;
using System.Threading.Tasks;

namespace MyGtdApp.Services.Undo
{
    // Undo 서비스의 인터페이스입니다.
    public interface IUndoService
    {
        event Action? OnChange;
        void Push(UndoAction action);
        Task<bool> UndoLatestAsync();
        UndoAction? GetLatestAction();
        bool CanUndo();
    }
}