// 배경 스크롤 차단/복원 함수 (기존 기능 유지)
window.preventBackgroundScroll = function (prevent) {
    const body = document.body;
    const html = document.documentElement;

    if (prevent) {
        const isMobile = window.innerWidth <= 768;
        if (isMobile) {
            const scrollY = window.scrollY;
            body.dataset.scrollY = scrollY.toString();
            body.style.position = 'fixed';
            body.style.top = `-${scrollY}px`;
            body.style.left = '0';
            body.style.right = '0';
            body.style.width = '100%';
            body.style.overflow = 'hidden';
            html.style.overflow = 'hidden';
            body.style.touchAction = 'none';
        }
        body.classList.add('modal-open', 'disable-task-interaction', 'disable-task-selection');
    } else {
        const isMobile = window.innerWidth <= 768;
        if (isMobile) {
            const scrollY = parseInt(body.dataset.scrollY || '0', 10);
            body.style.position = '';
            body.style.top = '';
            body.style.left = '';
            body.style.right = '';
            body.style.width = '';
            body.style.overflow = '';
            body.style.touchAction = '';
            html.style.overflow = '';
            window.scrollTo(0, scrollY);
            delete body.dataset.scrollY;
        }
        body.classList.remove('modal-open', 'disable-task-interaction', 'disable-task-selection');
    }
};

const style = document.createElement('style');
style.textContent = `
  @media (min-width: 769px) {
      body.modal-open { padding-right: 15px; overflow-y: scroll; }
      body.modal-open .modal-backdrop { pointer-events: none; }
      body.modal-open .modal-container { pointer-events: auto; }
  }
`;
document.head.appendChild(style);

window.setupContextInputHandling = function () {
    document.addEventListener('keydown', function (e) {
        if (e.target.matches('.new-context-input input') && e.key === 'Enter') {
            e.preventDefault();
        }
    });
};

document.addEventListener('DOMContentLoaded', function () {
    window.setupContextInputHandling();
});

// 🚀 새로 추가: 안전한 포커스 설정 함수
window.focusElementById = function (elementId) {
    return new Promise((resolve, reject) => {
        try {
            const element = document.getElementById(elementId);

            if (!element) {
                reject(new Error(`Element with ID '${elementId}' not found`));
                return;
            }

            // 요소가 실제로 보이는지 확인
            if (element.offsetParent === null && element.style.display !== 'none') {
                reject(new Error(`Element '${elementId}' is not visible`));
                return;
            }

            // 포커스 설정
            element.focus();

            // 포커스가 실제로 설정되었는지 확인
            if (document.activeElement === element) {
                console.log(`[JS] 포커스 설정 성공: ${elementId}`);
                resolve();
            } else {
                reject(new Error(`Failed to focus element '${elementId}'`));
            }
        } catch (error) {
            reject(error);
        }
    });
};

// 풀스크린 Description 모드 토글 함수 (기존 기능 유지)
window.toggleFullscreenDescriptionMode = function (enable) {
    const body = document.body;
    const html = document.documentElement;

    if (enable) {
        // 1. 현재 스크롤 위치 기억
        const scrollY = window.scrollY;
        body.dataset.fsScrollY = scrollY.toString();

        // 2. [핵심] 화면을 맨 위로 강제 스크롤
        window.scrollTo(0, 0);

        // 3. 맨 위로 올라간 상태에서 화면을 고정
        body.style.position = 'fixed';
        body.style.top = '0';
        body.style.left = '0';
        body.style.right = '0';
        body.style.width = '100%';

        // 4. 클래스 부여 및 스크립트 충돌 방지
        body.classList.add('fullscreen-description-mode');
        html.classList.add('fullscreen-description-mode');
        window.__SCROLL_HEADER_DISABLED = true;

        console.log('[FULLSCREEN DESC] 활성화. Original ScrollY=', scrollY);
    } else {
        // 1. 복구할 스크롤 위치 가져오기
        const prevScrollY = parseInt(body.dataset.fsScrollY || '0', 10);

        // 2. 고정 해제
        body.style.position = '';
        body.style.top = '';
        body.style.left = '';
        body.style.right = '';
        body.style.width = '';

        // 3. 클래스 제거 및 스크립트 활성화
        body.classList.remove('fullscreen-description-mode');
        html.classList.remove('fullscreen-description-mode');
        window.__SCROLL_HEADER_DISABLED = false;

        // 4. [핵심] 원래 스크롤 위치로 복원
        window.scrollTo(0, prevScrollY);
        delete body.dataset.fsScrollY;

        console.log('[FULLSCREEN DESC] 비활성화 및 스크롤 복원:', prevScrollY);
    }
};

window.setDescriptionFullscreenMode = function (isFullscreen) {
    const body = document.body;
    if (isFullscreen) {
        body.classList.add('description-fullscreen-mode');
        body.style.overflow = 'hidden';
    } else {
        body.classList.remove('description-fullscreen-mode');
        body.style.overflow = '';
    }
};