// wwwroot/js/modules/drag-main.js

import * as Constants from './constants.js';
import * as Utils from './utils.js';
import * as Detection from './drag-detection.js';
import * as Calculation from './drag-calculation.js';
import * as Visual from './drag-visual.js';

const DOUBLE_TAP_DELAY = 300;
let tapCount = 0;
let tapTimer = null;
let lastTappedElement = null;

// ===== 메인 이벤트 핸들러 =====
export function handler(e) {
    switch (e.type) {
        case "touchstart": onStart(e); break;
        case "touchmove":
            Constants.setLastTouchEvent(e);
            onMove(e);
            break;
        case "touchend":
        case "touchcancel": onEnd(e); break;
    }
}

// ===== 🚀 [핵심 수정] 터치 시작 핸들러 (REWRITTEN) =====
export function onStart(e) {
    // 두 번째 손가락이 화면에 닿아 터치가 2개 이상이 되면,
    // 즉시 다중 터치 로직으로 전환합니다.
    if (Detection.isMultiTouchGesture(e)) {
        console.log("[DRAG] 다중 터치 감지 -> 다중 터치 모드로 전환");
        // 이전에 시작된 한 손가락 타이머(드래그, 탭)가 있다면 즉시 중단합니다.
        clearTimeout(Constants.pressTimer);
        clearTimeout(tapTimer);
        handleMultiTouchStart(e);
        return;
    }

    // 이하는 새로운 '한 손가락' 터치가 시작되는 경우입니다.
    if (e.touches.length === 1) {
        cleanAll(); // 이전 제스처 상태를 깨끗이 정리하고 시작합니다.

        if (!Detection.isDraggableTarget(e.target)) return;

        const t = e.touches[0];
        Constants.setStartPosition(t.clientX, t.clientY, Date.now());

        const candidateElement = Detection.findTaskElementAtPoint(t.clientX, t.clientY);
        if (!candidateElement) return;

        Constants.setCandidateElement(candidateElement);
        // '한 손가락'으로 할 수 있는 동작(롱프레스->드래그)을 위한 타이머를 시작합니다.
        startSingleTouchTimers(candidateElement);
    }
}

// '한 손가락' 제스처 타이머 설정 (롱프레스 -> 드래그 준비)
function startSingleTouchTimers(element) {
    if (!element || Constants.isMultiTouch) return; // 다중 터치 상태에서는 실행하지 않습니다.

    // 이 타이머는 오직 '드래그 준비' 상태를 만드는 역할만 합니다.
    const dragTimer = setTimeout(() => {
        if (Constants.candidateElement && !Constants.isDragging && !Constants.isMultiTouch) {
            Constants.setReadyToDrag(true);
            console.log("[DRAG] 드래그 준비 완료 (한 손가락 롱프레스).");
            // ✅ 중요: 여기서 선택 모드를 절대 활성화하지 않습니다.
        }
    }, Constants.DRAG_DELAY);
    Constants.setPressTimer(dragTimer);
}

// '두 손가락' 제스처 시작 처리
function handleMultiTouchStart(e) {
    const commonElement = Detection.findCommonTaskElement(e);
    if (!commonElement) return;

    // 상태를 '다중 터치' 모드로 전환합니다.
    Constants.setIsMultiTouch(true);
    Constants.setMultiTouchStartTime(Date.now());
    Constants.setMultiTouchElement(commonElement);
    Constants.setCandidateElement(commonElement);

    console.log("[MULTITOUCH] 다중 터치 대상 확정:", { taskId: commonElement.dataset.taskId });
    Visual.applyMultiTouchFeedback(commonElement); // '손가락 두 개' 아이콘 표시

    // '두 손가락 롱프레스'를 감지하여 선택 모드를 활성화하는 타이머를 시작합니다.
    const multiTouchTimer = setTimeout(() => {
        if (Constants.isMultiTouch && Constants.multiTouchElement) {
            triggerMultiSelectionMode(Constants.multiTouchElement);
        }
    }, Constants.MULTI_TOUCH_SELECTION_DELAY);
    Constants.setMultiTouchSelectionTimer(multiTouchTimer);
}

// '다중 선택 모드' 활성화
function triggerMultiSelectionMode(element) {
    const taskId = +element.dataset.taskId;

    // 이 함수는 오직 '두 손가락 롱프레스' 경로를 통해서만 호출됩니다.
    if (Constants.dotNetHelper) {
        console.log(`[MULTITOUCH] C# EnterSelectionMode 호출 (Task ID: ${taskId})`);
        Constants.dotNetHelper.invokeMethodAsync("EnterSelectionMode", taskId);
        Utils.triggerHapticFeedback('heavy');
        Visual.applySelectionModeEffects(element);
    } else {
        console.error("[MULTITOUCH] .NET 참조 객체를 찾을 수 없어 선택 모드를 활성화할 수 없습니다.");
    }

    cleanupMultiTouchState(); // 제스처가 완료되었으므로 상태를 정리합니다.
}

// 다중 터치 관련 상태 정리
function cleanupMultiTouchState() {
    clearTimeout(Constants.multiTouchSelectionTimer);
    if (Constants.multiTouchElement) {
        Visual.removeMultiTouchFeedback(Constants.multiTouchElement);
    }
    Constants.setIsMultiTouch(false);
    Constants.setMultiTouchElement(null);
    Constants.setMultiTouchStartTime(0);
}

// ===== 터치 이동 핸들러 =====
export function onMove(e) {
    // 다중 터치 모드에서는 드래그를 비활성화합니다.
    if (Constants.isMultiTouch) {
        // 손가락 하나라도 떼면 제스처를 취소합니다.
        if (!Detection.isMultiTouchGesture(e)) {
            console.log("[MULTITOUCH] 손가락 하나가 떨어져 제스처를 취소합니다.");
            cleanAll();
        }
        return;
    }

    if (!Constants.candidateElement) return;

    const t = e.touches[0];
    const dist = Calculation.calculateDistance(Constants.startX, Constants.startY, t.clientX, t.clientY);

    // 드래그가 아직 시작되지 않았을 때
    if (!Constants.isDragging) {
        // '드래그 준비' 상태이고 충분히 움직였다면 드래그를 시작합니다.
        if (Detection.shouldStartDrag(Constants.readyToDrag, dist, Constants.MIN_DRAG_DISTANCE)) {
            beginDrag();
        }
        return;
    }

    // 드래그가 시작된 후
    e.preventDefault(); // 화면 스크롤 방지
    Constants.setMovedAfterDrag(true);
    Utils.scheduleUpdate(() => {
        const dropInfo = Calculation.calculateUnifiedDropInfo(t.clientX, t.clientY, Constants.draggedElement, Constants.draggedTaskId);
        Constants.setLastDropInfo(dropInfo);
        Visual.highlightDropTargetUnified(dropInfo);
    });
}

// ===== 터치 종료 핸들러 =====
export function onEnd(e) {
    // 모든 타이머를 중단합니다.
    clearTimeout(Constants.pressTimer);
    clearTimeout(Constants.multiTouchSelectionTimer);
    clearTimeout(tapTimer);

    // 다중 터치 제스처였다면, 여기서 로직을 종료합니다.
    if (Constants.isMultiTouch) {
        console.log("[MULTITOUCH] 두 손가락 터치 종료.");
        cleanAll();
        return;
    }

    // 드래그 중이었다면, 드롭 처리를 합니다.
    if (Constants.isDragging) {
        const dropInfo = Constants.lastDropInfo;
        if (Calculation.isValidDrop(dropInfo, Constants.draggedTaskId) && Constants.dotNetHelper) {
            Constants.dotNetHelper.invokeMethodAsync("HandleDropOnProject", dropInfo.targetId, dropInfo.position);
            Utils.triggerHapticFeedback('success');
        }
        cleanAll();
        return;
    }

    // 드래그가 아니었다면, '탭' 또는 '더블탭'으로 간주합니다.
    const dist = Calculation.calculateDistance(Constants.startX, Constants.startY, e.changedTouches[0].clientX, e.changedTouches[0].clientY);
    if (dist > Constants.MOVE_TOLERANCE) { // 많이 움직였다면 탭이 아님
        cleanAll();
        return;
    }

    const tappedElement = Constants.candidateElement;
    if (!tappedElement) {
        cleanAll();
        return;
    }

    // 더블탭 로직
    tapCount++;
    if (lastTappedElement !== tappedElement) {
        tapCount = 1;
    }
    lastTappedElement = tappedElement;

    if (tapCount === 1) {
        tapTimer = setTimeout(() => { cleanAll(); }, DOUBLE_TAP_DELAY);
    } else if (tapCount === 2) {
        const taskId = +tappedElement.dataset.taskId;
        if (taskId && Constants.dotNetHelper) {
            console.log(`[DRAG-SYSTEM] 더블탭 감지 -> 모달 열기 요청 (Task ID: ${taskId})`);
            Constants.dotNetHelper.invokeMethodAsync("ShowEditModal", taskId);
        }
        cleanAll();
    }
}

// ===== 드래그 시작 처리 =====
export function beginDrag() {
    if (!Constants.candidateElement || Constants.isMultiTouch) return;

    clearTimeout(tapTimer);
    tapCount = 0;

    Constants.setIsDragging(true);
    Constants.setDraggedElement(Constants.candidateElement);
    Constants.setDraggedTaskId(+Constants.draggedElement.dataset.taskId);
    Constants.setSavedDisplay(Constants.draggedElement.style.display || '');

    Visual.applyDragStartEffects(Constants.draggedElement);
    Utils.triggerHapticFeedback('light');

    Constants.setReadyToDrag(false);
    const t = Constants.lastTouchEvent.touches[0];
    Constants.setDragStartPosition(t.clientX, t.clientY);
}

// ===== 모든 상태 정리 =====
export function cleanAll() {
    clearTimeout(Constants.pressTimer);
    clearTimeout(Constants.multiTouchSelectionTimer);
    clearTimeout(tapTimer);

    if (Constants.draggedElement) {
        Visual.restoreElementVisuals(Constants.draggedElement, Constants.savedDisplay);
    }
    if (Constants.multiTouchElement) {
        Visual.removeMultiTouchFeedback(Constants.multiTouchElement);
    }

    Visual.removeAllVisualEffects();
    Constants.resetDragState();
    Utils.cancelScheduledUpdate();

    tapCount = 0;
    tapTimer = null;
    lastTappedElement = null;
}
