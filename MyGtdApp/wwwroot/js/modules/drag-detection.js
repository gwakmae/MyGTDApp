// 터치 감지 및 요소 찾기 관련 함수들

// ===== 정확한 요소 찾기 =====
export function findTaskElementAtPoint(clientX, clientY) {
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

    const taskNode = elementAtPoint.closest(".task-node-self");

    if (!taskNode) {
        console.log("[DRAG] task-node-self 찾을 수 없음");
        return null;
    }

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
    if (target.closest(".sidebar")) {
        console.log("[DRAG] 사이드바 영역 - GTD 드래그 무시");
        return false;
    }

    // ▼ [수정] 새로 추가한 .task-checkbox-btn과 삭제 버튼 등을 드래그 대상에서 명시적으로 제외합니다.
    if (target.closest("button, input[type=checkbox], .sidebar-toggle-btn, .mobile-header, .task-checkbox-btn, .simple-delete")) {
        console.log("[DRAG] 버튼 요소 무시");
        return false;
    }
    return true;
}

// 🆕 두 손가락 터치 감지
export function isMultiTouchGesture(e) {
    return e.touches && e.touches.length >= 2;
}

// 🆕 두 손가락이 같은 태스크 위에 있는지 확인
export function findCommonTaskElement(e) {
    if (!isMultiTouchGesture(e)) return null;

    const touch1 = e.touches[0];
    const touch2 = e.touches[1];

    const element1 = findTaskElementAtPoint(touch1.clientX, touch1.clientY);
    const element2 = findTaskElementAtPoint(touch2.clientX, touch2.clientY);

    // 두 터치 포인트가 같은 태스크를 가리키는지 확인
    if (element1 && element2 && element1 === element2) {
        return element1;
    }

    // 또는 두 터치 포인트가 같은 태스크 영역 내에 있는지 확인
    if (element1 && element2) {
        const taskId1 = element1.dataset.taskId;
        const taskId2 = element2.dataset.taskId;
        if (taskId1 === taskId2) {
            return element1;
        }
    }

    return null;
}

// ===== 스크롤 감지 =====
export function isScrollGesture(dx, dy, tolerance = 15) {
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
