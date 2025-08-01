using System.Collections.Generic;

namespace MyGtdApp.Models;

/// <summary>
/// JSON 파일 ↔ 객체 변환 시 사용하는 래퍼
/// (속성 이름은 파일 구조 <code>{ "tasks": [...] }</code>와 맞춥니다)
/// </summary>
public class JsonTaskHelper
{
    public List<TaskItem> Tasks { get; set; } = new();
}
