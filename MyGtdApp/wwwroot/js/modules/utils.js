// 유틸리티 함수들 (햅틱, 애니메이션, 성능 최적화 등)

// 성능 최적화 - RAF 기반 업데이트
let rafId = null;
let pendingUpdate = null;

// ===== 햅틱 피드백 =====
export function triggerHapticFeedback(type = 'light') {
    if ('vibrate' in navigator) {
        const patterns = {
            light: [10],
            medium: [20],
            heavy: [30],
            success: [10, 50, 10]
        };
        navigator.vibrate(patterns[type]);
    }
}

// ===== 드래그 경계 제한 =====
export function isValidDropZone(clientX, clientY) {
    // 드래그 가능 영역 확인
    const boardContainer = document.querySelector('.board-container');
    if (!boardContainer) return false;

    const rect = boardContainer.getBoundingClientRect();
    return (
        clientX >= rect.left &&
        clientX <= rect.right &&
        clientY >= rect.top &&
        clientY <= rect.bottom
    );
}

// ===== 성능 최적화 - RAF 기반 업데이트 =====
export function scheduleUpdate(updateFn) {
    pendingUpdate = updateFn;

    if (!rafId) {
        rafId = requestAnimationFrame(() => {
            if (pendingUpdate) {
                pendingUpdate();
                pendingUpdate = null;
            }
            rafId = null;
        });
    }
}

export function cancelScheduledUpdate() {
    if (rafId) {
        cancelAnimationFrame(rafId);
        rafId = null;
        pendingUpdate = null;
    }
}

// ===== 드래그 진행률 업데이트 =====
export function updateDragProgress(element, progress) {
    // progress: 0-1 사이 값
    const scale = 1 + (progress * 0.05);  // 최대 5% 확대
    const rotation = progress * 2;         // 최대 2도 회전

    element.style.transform = `scale(${scale}) rotate(${rotation}deg)`;
    element.style.filter = `brightness(${1 + progress * 0.1})`;
}

// ===== 부드러운 스냅백 애니메이션 =====
export function animateSnapBack(element, callback) {
    // 원래 위치 계산 (대략적)
    element.style.transition = 'transform 0.3s cubic-bezier(0.25, 0.46, 0.45, 0.94)';
    element.style.transform = 'translate(0, 0) scale(1)';
    element.style.filter = ''; // Reset filter

    setTimeout(() => {
        element.style.transition = '';
        if (callback) callback();
    }, 300);
}

// ===== 요소 상태 복원 =====
export function resetElementStyles(element, savedDisplay) {
    if (element && savedDisplay !== undefined) {
        element.style.transform = '';
        element.style.transition = '';
        element.style.zIndex = '';
        element.style.opacity = '';
        element.style.filter = '';
        element.style.display = savedDisplay;
    }
}

// ===== DOM 클래스 정리 =====
export function cleanupDragClasses() {
    try {
        document.querySelectorAll(".drop-above, .drop-inside, .drop-below, .is-ghost")
            .forEach(el => el.classList.remove("drop-above", "drop-inside", "drop-below", "is-ghost"));
    } catch (error) {
        console.error("[DRAG] 정리 중 오류:", error);
    }
}
