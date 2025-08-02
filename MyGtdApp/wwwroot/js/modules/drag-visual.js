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

    // 드래그 시작 애니메이션
    element.style.transition = 'transform 0.2s ease-out';
    element.style.transform = 'scale(1.05)';
    element.style.zIndex = '1000';
    element.classList.add("is-ghost");

    // 잠시 후 transition 제거 (드래그 중에는 부드럽게)
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
        // 경계 안에 있으면 정상 표시
        element.style.opacity = '0.6';
    } else {
        // 경계 밖으로 나가면 시각적 피드백
        element.style.opacity = '0.3';
    }
}

// ===== 드래그 진행률 시각화 =====
export function updateDragProgressVisual(element, progress) {
    if (!element) return;

    // progress: 0-1 사이 값
    const scale = 1 + (progress * 0.05);  // 최대 5% 확대
    const rotation = progress * 2;         // 최대 2도 회전

    element.style.transform = `scale(${scale}) rotate(${rotation}deg)`;
    element.style.filter = `brightness(${1 + progress * 0.1})`;
}

// ===== 모든 시각적 효과 제거 =====
export function removeAllVisualEffects() {
    try {
        document.querySelectorAll(".drop-above, .drop-inside, .drop-below, .is-ghost")
            .forEach(el => el.classList.remove("drop-above", "drop-inside", "drop-below", "is-ghost"));
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
}
