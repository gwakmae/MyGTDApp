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