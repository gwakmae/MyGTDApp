// 백그라운드 스크롤 차단/복원 함수 (간소화)
window.preventBackgroundScroll = function(prevent) {
    const body = document.body;
    const html = document.documentElement;
    
    if (prevent) {
        // 현재 스크롤 위치 저장
        const scrollY = window.scrollY;
        body.dataset.scrollY = scrollY.toString();
        
        // 간단한 스크롤 차단
        body.style.position = 'fixed';
        body.style.top = `-${scrollY}px`;
        body.style.left = '0';
        body.style.right = '0';
        body.style.overflow = 'hidden';
        
        html.style.overflow = 'hidden';
        
        console.log('[MODAL] 백그라운드 스크롤 차단됨');
    } else {
        // 스크롤 위치 복원
        const scrollY = parseInt(body.dataset.scrollY || '0', 10);
        
        body.style.position = '';
        body.style.top = '';
        body.style.left = '';
        body.style.right = '';
        body.style.overflow = '';
        
        html.style.overflow = '';
        
        // 스크롤 위치 복원
        window.scrollTo(0, scrollY);
        
        delete body.dataset.scrollY;
        
        console.log('[MODAL] 백그라운드 스크롤 복원됨');
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