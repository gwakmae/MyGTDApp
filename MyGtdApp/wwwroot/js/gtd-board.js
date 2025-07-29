// gtd-board.js
let draggedTaskId = null;
let touchStartX = 0;
let touchStartY = 0;

function startDrag(taskId) {
    draggedTaskId = taskId;
}

function getDraggedTask() {
    return draggedTaskId;
}

// 텍스트 편집 후 파란색 잔상 제거
function clearTextSelection() {
    window.getSelection().removeAllRanges();
}

// 터치 이벤트 핸들러 추가
function setupTouchHandlers() {
    document.addEventListener('touchstart', handleTouchStart, false);
    document.addEventListener('touchmove', handleTouchMove, false);
    document.addEventListener('touchend', handleTouchEnd, false);
}

function handleTouchStart(event) {
    const touch = event.touches[0];
    touchStartX = touch.clientX;
    touchStartY = touch.clientY;
    const target = event.target.closest('.task-node-self');
    if (target) {
        const taskId = target.getAttribute('data-task-id');
        if (taskId) {
            draggedTaskId = parseInt(taskId);
            target.classList.add('is-ghost');
            event.preventDefault();
        }
    }
}

function handleTouchMove(event) {
    if (draggedTaskId) {
        event.preventDefault();
        const touch = event.touches[0];
        const elements = document.elementsFromPoint(touch.clientX, touch.clientY);
        const target = elements.find(el => el.classList.contains('task-node-self') || el.classList.contains('board-column'));
        if (target) {
            // 드롭 가능 영역 표시 로직 (옵션)
            target.classList.add('drag-over');
        }
    }
}

function handleTouchEnd(event) {
    if (draggedTaskId) {
        const touch = event.changedTouches[0];
        const elements = document.elementsFromPoint(touch.clientX, touch.clientY);
        const target = elements.find(el => el.classList.contains('task-node-self'));
        if (target) {
            const targetTaskId = target.getAttribute('data-task-id');
            if (targetTaskId && targetTaskId != draggedTaskId) {
                // Blazor 컴포넌트로 이벤트 전달 (Invoke 메서드 필요)
                window.Blazor.invokeMethodAsync('MyGtdApp', 'HandleDropOnProject', parseInt(targetTaskId), 'Inside');
            }
        }
        // 드래그 종료
        draggedTaskId = null;
        document.querySelectorAll('.is-ghost').forEach(el => el.classList.remove('is-ghost'));
        document.querySelectorAll('.drag-over').forEach(el => el.classList.remove('drag-over'));
        event.preventDefault();
    }
}

// 페이지 로드 시 터치 핸들러 설정
document.addEventListener('DOMContentLoaded', setupTouchHandlers);