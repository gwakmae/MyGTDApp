// 이 파일은 wwwroot/js 폴더에 새로 생성합니다.

let draggedTaskId = null;

function startDrag(taskId) {
    draggedTaskId = taskId;
}

function getDraggedTask() {
    return draggedTaskId;
}

// --- 이 함수를 새로 추가합니다 ---
// 텍스트 편집 후 파란색 잔상을 제거하는 기능
function clearTextSelection() {
    window.getSelection().removeAllRanges();
}