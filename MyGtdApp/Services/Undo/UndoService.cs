using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyGtdApp.Services.Undo
{
    // 실제 Undo 작업을 스택(Stack)처럼 관리하고 실행하는 서비스 구현체입니다.
    public class UndoService : IUndoService
    {
        private readonly LinkedList<UndoAction> _stack = new();
        private readonly int _capacity = 20; // 최대 20개까지 Undo 기록을 보관
        private readonly object _lock = new();
        public event Action? OnChange;

        public void Push(UndoAction action)
        {
            lock (_lock)
            {
                _stack.AddFirst(action);
                while (_stack.Count > _capacity)
                {
                    _stack.RemoveLast();
                }
            }
            OnChange?.Invoke();
        }

        public async Task<bool> UndoLatestAsync()
        {
            UndoAction? act;
            lock (_lock)
            {
                if (_stack.First is null) return false;
                act = _stack.First.Value;
                _stack.RemoveFirst();
            }

            try
            {
                if (act != null)
                {
                    await act.UndoAsync();
                }
                OnChange?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UNDO] Undo 작업 실패: {ex.Message}");
                return false;
            }
        }

        public UndoAction? GetLatestAction()
        {
            lock (_lock)
            {
                return _stack.FirstOrDefault();
            }
        }

        public bool CanUndo()
        {
            lock (_lock)
            {
                return _stack.Any();
            }
        }
    }
}
