// 메인 드래그 로직 및 이벤트 핸들러들

import * as Constants from './constants.js';
import * as Utils from './utils.js';
import * as Detection from './drag-detection.js';
import * as Calculation from './drag-calculation.js';
import * as Visual from './drag-visual.js';

// ===== 메인 핸들러 =====
export function handler(e) {
    switch (e.type) {
        case "touchstart": onStart(e); break;
        case "touchmove":
            Constants.setLastTouchEvent(e); // Store the event for beginDrag
            onMove(e);
            break;
        case "touchend":
        case "touchcancel": onEnd(e); break;
    }
}

// ===== 터치 시작 핸들러 =====
export function onStart(e) {
    // 드래그 불가능한 요소 체크
    if (!Detection.isDraggableTarget(e.target)) {
        return;
    }

    const t = e.touches[0];
    Constants.setStartPosition(t.clientX, t.clientY, Date.now());
    Constants.setHasMovedEnough(false);
    Constants.setLastDropInfo(null);

    // 핵심 수정: 터치 좌표 기준으로 정확한 요소 찾기
    const candidateElement = Detection.findTaskElementAtPoint(t.clientX, t.clientY);

    if (!candidateElement) {
        console.log("[DRAG] 유효한 task-node-self 없음");
        return;
    }

    Constants.setCandidateElement(candidateElement);

    console.log("[DRAG] 터치 시작:", {
        candidateId: candidateElement.dataset.taskId,
        candidateTitle: candidateElement.querySelector('.task-title')?.textContent?.trim(),
        touchX: t.clientX,
        touchY: t.clientY
    });

    const timer = setTimeout(() => {
        // 준비 플래그만 세팅, 실제 beginDrag 는 onMove 에서
        if (Constants.candidateElement) {
            Constants.setReadyToDrag(true);
        }
    }, Constants.DRAG_DELAY);

    Constants.setPressTimer(timer);
}

// ===== 터치 이동 핸들러 =====
export function onMove(e) {
    if (!Constants.candidateElement && !Constants.isDragging) return;

    const t = e.touches[0];
    const dx = t.clientX - Constants.startX;
    const dy = t.clientY - Constants.startY;
    const dist = Calculation.calculateDistance(Constants.startX, Constants.startY, t.clientX, t.clientY);

    if (!Constants.isDragging) {
        // 스크롤 감지
        if (Detection.isScrollGesture(dx, dy, Constants.MOVE_TOLERANCE)) {
            console.log("[DRAG] 스크롤 감지 - 드래그 취소");
            cleanAll();
            return;
        }

        // long-press 를 끝냈고 이동량이 충분하면 drag 시작
        if (Detection.shouldStartDrag(Constants.readyToDrag, dist, Constants.MIN_DRAG_DISTANCE)) {
            beginDrag();
        }
        return;    // 아직 drag 모드 아님
    }

    // 여기부터는 이미 drag 중
    const currentDx = t.clientX - Constants.dragStartX;
    const currentDy = t.clientY - Constants.dragStartY;
    const currentDist = Calculation.calculateDistance(Constants.dragStartX, Constants.dragStartY, t.clientX, t.clientY);

    // 아직 충분히 안 움직였으면 drop 계산도 하지 않음
    if (!Constants.movedAfterDrag) {
        if (!Detection.hasMovedEnoughAfterDrag(currentDist, Constants.MIN_MOVE_AFTER_DRAG)) {
            // placeholder 위치 그대로 유지, 계산 스킵
            return;
        }
        Constants.setMovedAfterDrag(true);    // 한 번 넘으면 이후엔 계속 계산
    }

    e.preventDefault(); // 실제 드래그 중이고 충분히 이동했을 때만 기본 동작 방지

    // 드래그 경계 확인
    const isInBounds = Utils.isValidDropZone(t.clientX, t.clientY);
    Visual.updateBoundaryFeedback(Constants.draggedElement, isInBounds);

    if (!isInBounds) {
        return;
    }

    // 진행률 계산 및 시각화
    const progress = Calculation.calculateDragProgress(currentDist);
    if (Constants.draggedElement) {
        Utils.updateDragProgress(Constants.draggedElement, progress);
    }

    // RAF로 업데이트 스케줄링
    Utils.scheduleUpdate(() => {
        const dropInfo = Calculation.calculateUnifiedDropInfo(
            t.clientX, t.clientY, Constants.draggedElement, Constants.draggedTaskId
        );
        Constants.setLastDropInfo(dropInfo);
        Visual.highlightDropTargetUnified(dropInfo);
    });
}

// ===== 터치 종료 핸들러 =====
export function onEnd(e) {
    clearTimeout(Constants.pressTimer);

    if (!Constants.isDragging) {
        cleanAll();
        return;
    }

    const dropInfo = Constants.lastDropInfo;

    console.log("[DRAG] 최종 드롭:", {
        from: Constants.draggedTaskId,
        to: dropInfo?.targetId,
        position: dropInfo?.position
    });

    if (Calculation.isValidDrop(dropInfo, Constants.draggedTaskId) && Constants.dotNetHelper) {
        try {
            Constants.dotNetHelper.invokeMethodAsync("HandleDropOnProject", dropInfo.targetId, dropInfo.position);
            // 성공 시 햅틱 피드백
            Utils.triggerHapticFeedback('success');
        } catch (error) {
            console.error("[DRAG] 드롭 실패:", error);
        }
    } else {
        console.log("[DRAG] 드롭 취소 - 유효하지 않은 타겟");
        // 드롭 실패 시 스냅백 애니메이션
        if (Constants.draggedElement) {
            Utils.animateSnapBack(Constants.draggedElement, () => {
                cleanAll();
            });
            return; // cleanAll을 애니메이션 완료 후 호출
        }
    }

    cleanAll();
}

// ===== 드래그 시작 =====
export function beginDrag() {
    if (!Constants.candidateElement) return;

    Constants.setIsDragging(true);
    Constants.setDraggedElement(Constants.candidateElement);
    Constants.setDraggedTaskId(+Constants.draggedElement.dataset.taskId);
    Constants.setSavedDisplay(Constants.draggedElement.style.display || '');

    // 드래그 시작 시각적 효과
    Visual.applyDragStartEffects(Constants.draggedElement);

    // 햅틱 피드백 추가
    Utils.triggerHapticFeedback('light');

    console.log("[DRAG] 드래그 시작:", {
        taskId: Constants.draggedTaskId,
        title: Constants.draggedElement.querySelector('.task-title')?.textContent?.trim()
    });

    Constants.setReadyToDrag(false); // Set to false when drag begins

    // dragStartX / dragStartY 저장
    const t = Constants.lastTouchEvent.touches[0];    // beginDrag() 호출 직전에 onMove 에서 보관
    Constants.setDragStartPosition(t.clientX, t.clientY);
    Constants.setMovedAfterDrag(false);    // 리셋
}

// ===== 모든 상태 정리 =====
export function cleanAll() {
    clearTimeout(Constants.pressTimer);

    // 원래 상태로 복원
    Visual.restoreElementVisuals(Constants.draggedElement, Constants.savedDisplay);

    // 시각적 효과 제거
    Visual.removeAllVisualEffects();

    // 상태 초기화
    Constants.resetDragState();

    // RAF 정리
    Utils.cancelScheduledUpdate();

    console.log("[DRAG] 정리 완료");
}
