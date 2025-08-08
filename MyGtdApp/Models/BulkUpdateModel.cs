namespace MyGtdApp.Models
{
    /// <summary>
    /// 여러 작업을 일괄적으로 업데이트하기 위한 데이터 모델
    /// </summary>
    public class BulkUpdateModel
    {
        public List<int> TaskIds { get; set; } = new();

        /// <summary>
        /// null이 아니면 해당 값으로 마감일을 업데이트합니다.
        /// </summary>
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// null이 아니면 해당 값으로 중요도를 업데이트합니다.
        /// </summary>
        public Priority? Priority { get; set; }

        /// <summary>
        /// 비어있지 않으면 선택된 모든 작업에 이 컨텍스트를 추가합니다.
        /// </summary>
        public string? ContextToAdd { get; set; }

        /// <summary>
        /// 비어있지 않으면 선택된 모든 작업에서 이 컨텍스트를 제거합니다.
        /// </summary>
        public string? ContextToRemove { get; set; }
    }
}