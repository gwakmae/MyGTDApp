// 터치 감지 및 요소 찾기 관련 함수들

// ===== 정확한 요소 찾기 =====
export function findTaskElementAtPoint(clientX, clientY) {
    // 1. 터치 지점의 정확한 요소 찾기
    const elementAtPoint = document.elementFromPoint(clientX, clientY);

    if (!elementAtPoint) {
        console.log("[DRAG] elementFromPoint 결과 없음");
        return null;
    }

    console.log("[DRAG] 터치 지점 요소:", {
        tagName: elementAtPoint.tagName,
        className: elementAtPoint.className,
        textContent: elementAtPoint.textContent?.trim().substring(0, 30)
    });

    // 2. 가장 가까운 task-node-self 찾기
    const taskNode = elementAtPoint.closest(".task-node-self");

    if (!taskNode) {
        console.log("[DRAG] task-node-self 찾을 수 없음");
        return null;
    }

    // 3. 유효성 검사 (data-task-id 있는지)
    const taskId = taskNode.dataset.taskId;
    if (!taskId) {
        console.log("[DRAG] task-id 없는 요소");
        return null;
    }

    console.log("[DRAG] 최종 선택된 요소:", {
        taskId: taskId,
        element: taskNode,
        bounds: taskNode.getBoundingClientRect()
    });

    return taskNode;
}

// ===== 드래그 가능한 요소인지 검사 =====
export function isDraggableTarget(target) {
    // 버튼 등은 무시
    if (target.closest("button, input[type=checkbox], .sidebar-toggle-btn, .mobile-header")) {
        console.log("[DRAG] 버튼 요소 무시");
        return false;
    }
    return true;
}

// ===== 스크롤 감지 =====
export function isScrollGesture(dx, dy, tolerance = 15) {
    // Y축 우선 스크롤 감지
    return Math.abs(dy) > tolerance && Math.abs(dy) > Math.abs(dx) * 1.2;
}

// ===== 드래그 시작 조건 검사 =====
export function shouldStartDrag(readyToDrag, distance, minDistance = 25) {
    return readyToDrag && distance >= minDistance;
}

// ===== 드래그 중 이동량 검사 =====
export function hasMovedEnoughAfterDrag(distance, minDistance = 12) {
    return distance >= minDistance;
}
