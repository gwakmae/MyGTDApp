// TaskDetail_Description.js - 기존 파일 대체용
// 풀스크린 Description 에디터를 위한 JavaScript 함수들

// 풀스크린 모드 활성화
window.enableFullscreenMode = function () {
    console.log('[FullScreen] 모드 활성화');

    // 스크롤 방지
    document.body.style.overflow = 'hidden';
    document.documentElement.style.overflow = 'hidden';

    // 풀스크린 클래스 추가
    document.body.classList.add('fullscreen-active');
};

// 풀스크린 모드 비활성화
window.disableFullscreenMode = function () {
    console.log('[FullScreen] 모드 비활성화');

    // 스크롤 복원
    document.body.style.overflow = '';
    document.documentElement.style.overflow = '';

    // 풀스크린 클래스 제거
    document.body.classList.remove('fullscreen-active');
};

// 안전한 요소 포커스 (클래스명 기반)
window.focusElement = function (className) {
    try {
        // 클래스명으로 요소 찾기
        const element = document.querySelector('.' + className);

        if (element && element.focus && typeof element.focus === 'function') {
            // 약간의 지연 후 포커스
            setTimeout(() => {
                try {
                    element.focus();
                    console.log('[Focus] 성공:', className);
                } catch (e) {
                    console.log('[Focus] 실패 (무시됨):', e.message);
                }
            }, 50);
        } else {
            console.log('[Focus] 요소를 찾을 수 없음:', className);
        }
    } catch (error) {
        console.log('[Focus] 오류 (무시됨):', error.message);
    }
};

// 안전한 요소 포커스 (ID 기반) - 기존 코드 호환성을 위해 유지
window.focusElementById = function (elementId) {
    try {
        const element = document.getElementById(elementId);

        if (element && element.focus && typeof element.focus === 'function') {
            setTimeout(() => {
                try {
                    element.focus();
                    console.log('[Focus] ID 기반 성공:', elementId);
                } catch (e) {
                    console.log('[Focus] ID 기반 실패 (무시됨):', e.message);
                }
            }, 50);
        } else {
            console.log('[Focus] ID 요소를 찾을 수 없음:', elementId);
        }
    } catch (error) {
        console.log('[Focus] ID 기반 오류 (무시됨):', error.message);
    }
};

// ESC 키로 풀스크린 종료 감지
document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape' && document.body.classList.contains('fullscreen-active')) {
        console.log('[ESC] 풀스크린 종료 요청 감지');
        // Blazor 컴포넌트에서 keydown 이벤트로 처리됨
    }
});

// 페이지 언로드 시 정리
window.addEventListener('beforeunload', function () {
    disableFullscreenMode();
});

// 기존 함수들과의 호환성을 위한 별칭들
window.toggleFullscreenDescriptionMode = function (enable) {
    if (enable) {
        enableFullscreenMode();
    } else {
        disableFullscreenMode();
    }
};

// 안전한 포커스 설정 (기존 코드 호환성)
window.safeSetFocus = function (elementId) {
    focusElementById(elementId);
};

// 디버그 정보
console.log('[TaskDetail_Description.js] 로드 완료');