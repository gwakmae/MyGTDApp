using Microsoft.AspNetCore.Mvc;
using MyGtdApp.Models;
using MyGtdApp.Services;
using System.Threading.Tasks;

namespace MyGtdApp.Controllers
{
    // API 요청을 받기 위한 요청 모델(DTO)
    public class InboxItemRequest
    {
        public string Title { get; set; } = string.Empty;
    }

    [ApiController] // 이 클래스가 API 컨트롤러임을 나타냅니다.
    [Route("api/tasks")] // "http://.../api/tasks" 주소로 오는 요청을 처리합니다.
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;

        // 기존에 만들어둔 ITaskService를 그대로 주입받아 사용합니다.
        public TasksController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        // "api/tasks/inbox" 주소로 HTTP POST 요청이 오면 이 함수가 실행됩니다.
        [HttpPost("inbox")]
        public async Task<IActionResult> AddToInbox([FromBody] InboxItemRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Title))
            {
                return BadRequest("Title is required."); // 제목이 없으면 에러 응답
            }

            try
            {
                // TaskService를 사용해 Inbox 상태로 새 Task를 생성합니다.
                await _taskService.AddTaskAsync(
                    request.Title,
                    MyGtdApp.Models.TaskStatus.Inbox,
                    null);

                // 성공 응답
                return Ok(new { Message = "Task added to inbox successfully." });
            }
            catch
            {
                // 서버 내부 오류 응답
                return StatusCode(500, "An error occurred while adding the task.");
            }
        }
    }
}