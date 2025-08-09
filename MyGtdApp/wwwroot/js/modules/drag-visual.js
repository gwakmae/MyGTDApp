// 시각적 피드백 및 하이라이트 관련 함수들

// ===== 드롭 타겟 하이라이트 =====
export function highlightDropTargetUnified(dropInfo) {
    try {
        document.querySelectorAll(".drop-above, .drop-inside, .drop-below")
            .forEach(el => el.classList.remove("drop-above", "drop-inside", "drop-below"));

        if (!dropInfo || !dropInfo.target) return;

        const className = `drop-${dropInfo.position.toLowerCase()}`;
        dropInfo.target.classList.add(className);
    } catch (error) {
        console.error("[DRAG] 하이라이트 오류:", error);
    }
}

// ===== 드래그 시작 시각적 효과 =====
export function applyDragStartEffects(element) {
    if (!element) return;

    element.style.transition = 'transform 0.2s ease-out';
    element.style.transform = 'scale(1.05)';
    element.style.zIndex = '1000';
    element.classList.add("is-ghost");

    setTimeout(() => {
        if (element) {
            element.style.transition = '';
        }
    }, 200);
}

// ===== 드래그 중 경계 피드백 =====
export function updateBoundaryFeedback(element, isInBounds) {
    if (!element) return;

    if (isInBounds) {
        element.style.opacity = '0.6';
    } else {
        element.style.opacity = '0.3';
    }
}

// ===== 드래그 진행률 시각화 =====
export function updateDragProgressVisual(element, progress) {
    if (!element) return;

    const scale = 1 + (progress * 0.05);
    const rotation = progress * 2;

    element.style.transform = `scale(${scale}) rotate(${rotation}deg)`;
    element.style.filter = `brightness(${1 + progress * 0.1})`;
}

// 🆕 두 손가락 터치 시각적 피드백
export function applyMultiTouchFeedback(element) {
    if (!element) return;

    element.classList.add('multi-touch-active');
    element.style.boxShadow = '0 0 0 3px rgba(59, 130, 246, 0.5)';
    element.style.transform = 'scale(1.02)';

    console.log("[VISUAL] 두 손가락 터치 피드백 적용");
}

// 🆕 두 손가락 터치 피드백 제거
export function removeMultiTouchFeedback() {
    try {
        document.querySelectorAll('.multi-touch-active')
            .forEach(el => {
                el.classList.remove('multi-touch-active');
                el.style.boxShadow = '';
                el.style.transform = '';
            });
    } catch (error) {
        console.error("[VISUAL] 두 손가락 피드백 제거 중 오류:", error);
    }
}

// 선택 모드 시각적 효과 관리
export function applySelectionModeEffects(element) {
    if (!element) return;

    element.classList.add('selection-mode');
    element.style.transition = 'all 0.3s ease-out';
}

export function removeSelectionModeEffects() {
    try {
        document.querySelectorAll('.selection-mode')
            .forEach(el => {
                el.classList.remove('selection-mode');
                el.style.transition = '';
            });
    } catch (error) {
        console.error("[SELECTION] 선택 모드 효과 제거 중 오류:", error);
    }
}

// ===== 모든 시각적 효과 제거 =====
export function removeAllVisualEffects() {
    try {
        document.querySelectorAll(".drop-above, .drop-inside, .drop-below, .is-ghost, .selection-mode, .drag-ready, .multi-touch-active")
            .forEach(el => el.classList.remove("drop-above", "drop-inside", "drop-below", "is-ghost", "selection-mode", "drag-ready", "multi-touch-active"));
    } catch (error) {
        console.error("[DRAG] 시각적 효과 제거 중 오류:", error);
    }
}

// ===== 요소 스타일 복원 =====
export function restoreElementVisuals(element, savedDisplay) {
    if (!element || savedDisplay === undefined) return;

    element.style.transform = '';
    element.style.transition = '';
    element.style.zIndex = '';
    element.style.opacity = '';
    element.style.filter = '';
    element.style.display = savedDisplay;
    element.style.boxShadow = '';
}
