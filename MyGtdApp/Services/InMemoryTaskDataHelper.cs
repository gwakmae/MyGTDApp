using MyGtdApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyGtdApp.Services
{
    /// <summary>
    /// 메모리 내 작업 목록의 데이터를 가져오고 내보내는 로직을 캡슐화합니다.
    /// </summary>
    internal class InMemoryTaskDataHelper
    {
        private readonly List<TaskItem> _tasks;
        private readonly Func<int> _getNextId;
        private readonly Action<int> _setNextId;

        public InMemoryTaskDataHelper(List<TaskItem> tasks, Func<int> getNextId, Action<int> setNextId)
        {
            _tasks = tasks;
            _getNextId = getNextId;
            _setNextId = setNextId;
        }

        /// <summary>
        /// 현재 작업 목록을 JSON 문자열로 직렬화합니다.
        /// </summary>
        public string ExportTasksToJson()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter() }
            };
            return JsonSerializer.Serialize(new { tasks = _tasks }, options);
        }

        /// <summary>
        /// JSON 데이터로부터 작업 목록을 가져와 현재 목록을 대체합니다.
        /// </summary>
        public void ImportTasksFromJson(string jsonData)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
            var importData = JsonSerializer.Deserialize<JsonTaskHelper>(jsonData, options);

            if (importData?.Tasks != null && importData.Tasks.Any())
            {
                _tasks.Clear();
                _tasks.AddRange(importData.Tasks);
                _setNextId(_getNextId()); // 서비스의 nextId를 다시 계산하여 업데이트
            }
        }
    }
}