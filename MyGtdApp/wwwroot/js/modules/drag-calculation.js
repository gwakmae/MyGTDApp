// 드롭 위치 계산 및 검증 관련 함수들

// ===== 통합 드롭 정보 계산 =====
export function calculateUnifiedDropInfo(clientX, clientY, draggedElement, draggedTaskId) {
    let targetElement = null;

    const originalDisplay = draggedElement ? draggedElement.style.display : '';
    if (draggedElement) {
        draggedElement.style.display = 'none';
    }

    try {
        targetElement = document.elementFromPoint(clientX, clientY);
    } catch (error) {
        console.error("[DRAG] elementFromPoint 오류:", error);
    } finally {
        if (draggedElement) {
            draggedElement.style.display = originalDisplay;
        }
    }

    const dropTarget = targetElement ? targetElement.closest(".task-node-self") : null;

    if (!dropTarget || dropTarget === draggedElement) {
        return { target: null, targetId: null, position: "Inside" };
    }

    if (isCircularReference(dropTarget, draggedTaskId)) {
        console.log("[DRAG] 순환 참조 방지");
        return { target: null, targetId: null, position: "Inside" };
    }

    const targetId = +dropTarget.dataset.taskId;
    const position = calculatePreciseDropPosition(dropTarget, clientY, clientX);

    return { target: dropTarget, targetId, position };
}

// ===== 정확한 드롭 위치 계산 =====
export function calculatePreciseDropPosition(dropTarget, clientY, clientX) {
    try {
        const rect = dropTarget.getBoundingClientRect();
        const offsetY = clientY - rect.top;
        const offsetX = clientX - rect.left;

        const topZone = rect.height * 0.35;
        const bottomZone = rect.height * 0.65;
        const indentX = 28; // expander + padding

        if (offsetY < topZone) return "Above";
        if (offsetY > bottomZone) return "Below";

        /* 중앙 영역이라도 왼쪽 인덴트 안(28px) 에 손가락이 있으면
           Inside → 형제 간 이동으로 간주 */
        if (offsetX < indentX) {
            return offsetY < rect.height / 2 ? "Above" : "Below";
        }

        return "Inside";
    } catch (error) {
        console.error("[DRAG] 위치 계산 오류:", error);
        return "Inside";
    }
}

// ===== 순환 참조 검사 =====
export function isCircularReference(targetElement, draggedId) {
    let current = targetElement;
    while (current) {
        const currentId = +current.dataset.taskId;
        if (currentId === draggedId) return true;

        current = current.parentElement;
        if (current) {
            current = current.closest(".task-node-self");
        }
    }
    return false;
}

// ===== 드롭 유효성 검사 =====
export function isValidDrop(dropInfo, draggedTaskId) {
    return dropInfo &&
        dropInfo.target &&
        dropInfo.targetId &&
        dropInfo.targetId !== draggedTaskId;
}

// ===== 거리 계산 유틸리티 =====
export function calculateDistance(x1, y1, x2, y2) {
    return Math.hypot(x2 - x1, y2 - y1);
}

// ===== 드래그 진행률 계산 =====
export function calculateDragProgress(distance, maxDistance = 100) {
    return Math.min(distance / maxDistance, 1);
}
