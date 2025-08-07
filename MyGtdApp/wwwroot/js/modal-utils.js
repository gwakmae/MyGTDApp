// 백그라운드 스크롤 차단/복원 함수 (폴더블 태블릿 대응)
window.preventBackgroundScroll = function(prevent) {
    const body = document.body;
    const html = document.documentElement;
    
    if (prevent) {
        // 폴더블/태블릿 환경 감지
        const isLargeScreen = window.innerWidth >= 768 && window.innerHeight >= 600;
        
        if (isLargeScreen) {
            // 큰 화면에서는 백그라운드 스크롤 허용 (모달 외부 터치 시)
            console.log('[MODAL] 큰 화면 감지 - 백그라운드 스크롤 허용');
            body.classList.add('modal-open-large-screen');
            
            // 모달 backdrop에서만 스크롤 허용하도록 설정
            const backdrop = document.querySelector('.modal-backdrop');
            if (backdrop) {
                backdrop.style.touchAction = 'pan-y';
                backdrop.style.overscrollBehavior = 'contain';
            }
        } else {
            // 작은 화면에서는 기존 방식 (완전 차단)
            const scrollY = window.scrollY;
            body.dataset.scrollY = scrollY.toString();
            
            body.style.position = 'fixed';
            body.style.top = `-${scrollY}px`;
            body.style.left = '0';
            body.style.right = '0';
            body.style.overflow = 'hidden';
            
            html.style.overflow = 'hidden';
            
            console.log('[MODAL] 백그라운드 스크롤 차단됨');
        }
    } else {
        // 복원 로직
        if (body.classList.contains('modal-open-large-screen')) {
            // 큰 화면 복원
            body.classList.remove('modal-open-large-screen');
            console.log('[MODAL] 큰 화면 모달 정리됨');
        } else {
            // 작은 화면 복원 (기존 방식)
            const scrollY = parseInt(body.dataset.scrollY || '0', 10);
            
            body.style.position = '';
            body.style.top = '';
            body.style.left = '';
            body.style.right = '';
            body.style.overflow = '';
            
            html.style.overflow = '';
            
            window.scrollTo(0, scrollY);
            delete body.dataset.scrollY;
            
            console.log('[MODAL] 백그라운드 스크롤 복원됨');
        }
    }
};

// 컨텍스트 입력 필드에서 엔터키 처리 개선
window.setupContextInputHandling = function() {
    document.addEventListener('keydown', function(e) {
        // 새 컨텍스트 입력 필드에서 엔터키가 눌린 경우
        if (e.target.matches('.new-context-input input') && e.key === 'Enter') {
            e.preventDefault(); // 폼 제출 방지
            console.log('[CONTEXT INPUT] 엔터키 기본 동작 방지됨');
        }
    });
};

// 모달이 열릴 때 자동 실행
document.addEventListener('DOMContentLoaded', function() {
    window.setupContextInputHandling();
});