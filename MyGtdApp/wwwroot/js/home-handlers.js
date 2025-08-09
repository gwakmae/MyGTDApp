// 홈 페이지 전용 이벤트 핸들러

let dotNetHelper = null;
let keyboardListenersAttached = false;

// 키보드 핸들러 설정
window.setupKeyboardHandlers = function (helper) {
    dotNetHelper = helper;

    if (!keyboardListenersAttached) {
        document.addEventListener('keydown', handleKeyDown);
        document.addEventListener('click', handleBackgroundClick, true); // 캡처 단계에서 처리
        keyboardListenersAttached = true;
        console.log('[HOME] 키보드 및 클릭 핸들러 등록됨');
    }
};

// 키보드 이벤트 처리 수정
function handleKeyDown(e) {
    if (!dotNetHelper) return;

    // 모달이 열려있으면 일부 키보드 이벤트 무시
    if (document.body.classList.contains('disable-task-interaction')) {
        // ESC는 허용 (모달 닫기용)
        if (e.key === 'Escape') {
            e.preventDefault();
            dotNetHelper.invokeMethodAsync('HandleEscapeKey');
        }
        return; // 다른 키보드 이벤트는 무시
    }

    // 기존 키보드 처리 로직
    if (e.key === 'Escape') {
        e.preventDefault();
        dotNetHelper.invokeMethodAsync('HandleEscapeKey');
    }

    if (e.ctrlKey && e.key === 'a') {
        const isInTaskArea = e.target.closest('.board-container, .list-view-container');
        if (isInTaskArea) {
            e.preventDefault();
            dotNetHelper.invokeMethodAsync('SelectAllTasks');
        }
    }
}

// 빈 공간 클릭 처리 수정
function handleBackgroundClick(e) {
    if (!dotNetHelper) return;

    // 🆕 추가: 모달이 열려있으면 모든 배경 클릭 무시
    if (document.body.classList.contains('disable-task-interaction') ||
        document.body.classList.contains('disable-task-selection')) {
        console.log('[HOME] 모달 열린 상태 - 배경 클릭 무시');
        return;
    }

    // 🆕 추가: 풀스크린 Description 모드에서도 무시
    if (document.querySelector('.description-textarea.fullscreen-mode')) {
        console.log('[HOME] 풀스크린 Description 모드 - 배경 클릭 무시');
        return;
    }

    // 태스크 노드나 UI 요소가 아닌 곳을 클릭했는지 확인
    const isTaskElement = e.target.closest('.task-node-self, .bulk-action-bar, .bulk-edit-panel, button, input, select, textarea, .modal-container, .fullscreen-control-bar');

    if (!isTaskElement) {
        // 빈 공간이나 배경 클릭 시 선택 해제
        dotNetHelper.invokeMethodAsync('HandleBackgroundClick');
    }
}

// 🆕 모바일 디바이스 감지 개선
window.isMobileDevice = function () {
    const userAgent = navigator.userAgent || navigator.vendor || window.opera;
    const mobileRegex = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i;
    return mobileRegex.test(userAgent) || window.innerWidth <= 1199;
};

// 정리 함수
window.cleanupKeyboardHandlers = function () {
    if (keyboardListenersAttached) {
        document.removeEventListener('keydown', handleKeyDown);
        document.removeEventListener('click', handleBackgroundClick, true);
        keyboardListenersAttached = false;
        dotNetHelper = null;
        console.log('[HOME] 키보드 핸들러 정리됨');
    }
};
