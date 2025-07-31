namespace MyGtdApp.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public Priority Priority { get; set; }
        public TaskStatus Status { get; set; }

        // --- 계층 구조를 위해 추가된 속성 ---
        public int? ParentId { get; set; } // 부모 Task의 Id. 최상위 항목은 null.
        public List<TaskItem> Children { get; set; } = new(); // 자식 Task들의 목록

        // --- 순서 저장을 위한 속성 추가 ---
        public int SortOrder { get; set; }

        // --- 새로 추가될 속성들 ---

        /// <summary>
        /// 작업 완료 여부
        /// </summary>
        public bool IsCompleted { get; set; } = false;

        /// <summary>
        /// 작업 시작일 (선택 사항)
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// 작업 마감일 (선택 사항)
        /// </summary>
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// 컨텍스트 태그 목록 (예: "@Home", "@Work")
        /// </summary>
        public List<string> Contexts { get; set; } = new();

        /// <summary>
        /// 트리 노드 확장/축소 상태 (기본값: true - 펼침)
        /// </summary>
        public bool IsExpanded { get; set; } = true;
    }

    // --- TaskStatus Enum 수정 ---
    public enum TaskStatus
    {
        Inbox,
        NextActions,
        Projects,
        Someday,
        Completed // "Completed" 상태 추가
    }

    public enum Priority
    {
        Low,
        Medium,
        High
    }
}