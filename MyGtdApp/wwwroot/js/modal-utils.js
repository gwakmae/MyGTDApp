// 백그라운드 스크롤 차단/복원 함수 (폴더블 태블릿 대응)
window.preventBackgroundScroll = function (prevent) {
    const body = document.body;
    const html = document.documentElement;

    if (prevent) {
        // 🆕 모바일에서 완전한 스크롤 차단
        const scrollY = window.scrollY;
        body.dataset.scrollY = scrollY.toString();

        body.style.position = 'fixed';
        body.style.top = `-${scrollY}px`;
        body.style.left = '0';
        body.style.right = '0';
        body.style.width = '100%';
        body.style.overflow = 'hidden';

        html.style.overflow = 'hidden';

        // 🆕 터치 이벤트도 제어
        body.style.touchAction = 'none';

        console.log('[MODAL] 백그라운드 스크롤 완전 차단');
    } else {
        // 복원
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

        console.log('[MODAL] 백그라운드 스크롤 복원');
    }
};

// 컨텍스트 입력 필드에서 엔터키 처리 개선
window.setupContextInputHandling = function () {
    document.addEventListener('keydown', function (e) {
        // 새 컨텍스트 입력 필드에서 엔터키가 눌린 경우
        if (e.target.matches('.new-context-input input') && e.key === 'Enter') {
            e.preventDefault(); // 폼 제출 방지
            console.log('[CONTEXT INPUT] 엔터키 기본 동작 방지됨');
        }
    });
};

// 모달이 열릴 때 자동 실행
document.addEventListener('DOMContentLoaded', function () {
    window.setupContextInputHandling();
});
