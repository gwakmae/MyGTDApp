﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyGtdApp.Models
{
    /// <summary>
    /// GTD 작업(Task) 도메인 모델
    /// </summary>
    public class TaskItem
    {
        /* ──────────────────────────────
         * 기본 식별·분류 정보
         * ────────────────────────────── */
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public Priority Priority { get; set; }

        public TaskStatus Status { get; set; }

        /* ──────────────────────────────
         * 트리 구조(부모-자식 관계)
         * ────────────────────────────── */
        public int? ParentId { get; set; }                   // 최상위면 null

        [NotMapped]                                          // DB 매핑 제외 ← 변경된 부분
        public List<TaskItem> Children { get; set; } = new();

        public string Path { get; set; } = string.Empty;   // "4/102/201"
        public int Depth { get; set; }                  // 루트=0, 자식=1 …

        /* ──────────────────────────────
         * 정렬용
         * ────────────────────────────── */
        public int SortOrder { get; set; }

        /* ──────────────────────────────
         * 일정/상태 관리
         * ────────────────────────────── */
        /// <summary>완료 여부</summary>
        public bool IsCompleted { get; set; } = false;

        /// <summary>작업 시작일(선택)</summary>
        public DateTime? StartDate { get; set; }

        /// <summary>마감일(선택)</summary>
        public DateTime? DueDate { get; set; }

        /// <summary>완료 전 원래 상태 (완료 해제 시 복원용)</summary>
        public TaskStatus? OriginalStatus { get; set; }

        /* ──────────────────────────────
         * 태그/표시
         * ────────────────────────────── */
        /// <summary>컨텍스트 태그들 (예: "@Home", "@Work")</summary>
        public List<string> Contexts { get; set; } = new();

        /// <summary>트리 뷰 확장/축소 상태</summary>
        public bool IsExpanded { get; set; } = true;
    }

    /* ──────────────────────────────
     * 보조 열거형
     * ────────────────────────────── */
    public enum TaskStatus
    {
        Inbox,
        NextActions,
        Projects,
        Someday,
        Completed
    }

    public enum Priority
    {
        Low,
        Medium,
        High
    }
}
