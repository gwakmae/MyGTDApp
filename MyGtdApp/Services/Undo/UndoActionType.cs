namespace MyGtdApp.Services.Undo
{
    // 어떤 종류의 작업을 취소할지 정의합니다. (지금은 Delete만 사용)
    public enum UndoActionType
    {
        Delete,
        Move,
        Update
    }
}