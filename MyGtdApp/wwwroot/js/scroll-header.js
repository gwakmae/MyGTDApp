// 스크롤 헤더 숨김/보임 기능
class ScrollHeaderController {
    constructor() {
        this.header = null;
        this.lastScrollY = 0;
        this.scrollThreshold = 5;
        this.hideThreshold = 50;
        this.rafId = null;
        this.isHidden = false;

        this.init();
    }

    init() {
        // 헤더 찾기 - 더 구체적인 선택
        this.header = document.querySelector('.main-header, .modern-header');

        if (!this.header) {
            console.log('[SCROLL HEADER] 헤더를 찾을 수 없음 - 재시도 예약');
            setTimeout(() => this.init(), 1000);
            return;
        }

        console.log('[SCROLL HEADER] 헤더 발견:', {
            element: this.header.tagName,
            classes: this.header.className,
            children: this.header.children.length
        });

        if (this.isMobile()) {
            this.bindEvents();
            this.setupHeader();
            console.log('[SCROLL HEADER] 모바일 스크롤 제어 활성화');
        }
    }

    setupHeader() {
        if (!this.header) return;

        // 헤더에 필요한 CSS 속성 강제 적용
        this.header.style.position = 'sticky';
        this.header.style.top = '0';
        this.header.style.zIndex = '100';
        this.header.style.width = '100%';
        this.header.style.transition = 'transform 0.25s ease-out';
    }

    isMobile() {
        return window.innerWidth <= 1199;
    }

    bindEvents() {
        let ticking = false;

        const handleScroll = () => {
            if (!ticking) {
                this.rafId = requestAnimationFrame(() => {
                    this.handleScroll();
                    ticking = false;
                });
                ticking = true;
            }
        };

        window.addEventListener('scroll', handleScroll, { passive: true });

        window.addEventListener('resize', () => {
            if (!this.isMobile()) {
                this.showHeader();
            }
        });

        // 터치 시작 시 헤더 보이기
        document.addEventListener('touchstart', () => {
            if (this.isMobile() && this.isHidden) {
                this.showHeader();
            }
        }, { passive: true });
    }

    handleScroll() {
        // 🚀 최종 수정: 풀스크린 모드일 때 헤더 스크롤 기능 비활성화
        if (window.__SCROLL_HEADER_DISABLED === true) {
            if (this.isHidden) {
                this.showHeader(); // 만약 숨겨진 상태였다면, 안전하게 다시 보이도록 처리
            }
            return; // 이후 로직 실행 안 함
        }

        if (!this.header || !this.isMobile()) return;

        const currentScrollY = window.scrollY;
        const scrollDiff = Math.abs(currentScrollY - this.lastScrollY);

        if (scrollDiff < this.scrollThreshold) return;

        if (currentScrollY <= 20) {
            // 최상단 - 항상 헤더 보이기
            if (this.isHidden) {
                this.showHeader();
            }
        } else if (currentScrollY > this.lastScrollY && currentScrollY > this.hideThreshold) {
            // 스크롤 다운 - 헤더 숨기기
            if (!this.isHidden) {
                this.hideHeader();
            }
        } else if (currentScrollY < this.lastScrollY) {
            // 스크롤 업 - 헤더 보이기
            if (this.isHidden) {
                this.showHeader();
            }
        }

        this.lastScrollY = currentScrollY;
    }

    hideHeader() {
        if (!this.header) return;

        // 강력한 CSS 적용으로 전체 헤더 숨기기
        this.header.style.transform = 'translateY(-100%)';
        this.header.style.opacity = '0';
        this.header.style.pointerEvents = 'none';

        this.header.classList.add('header-hidden');
        this.header.classList.remove('header-visible');

        this.isHidden = true;

        console.log('[SCROLL HEADER] 헤더 숨김 완료');
    }

    showHeader() {
        if (!this.header) return;

        // 강력한 CSS 적용으로 전체 헤더 보이기
        this.header.style.transform = 'translateY(0)';
        this.header.style.opacity = '1';
        this.header.style.pointerEvents = 'auto';

        this.header.classList.remove('header-hidden');
        this.header.classList.add('header-visible');

        this.isHidden = false;

        console.log('[SCROLL HEADER] 헤더 표시 완료');
    }

    destroy() {
        if (this.rafId) {
            cancelAnimationFrame(this.rafId);
        }
    }
}

// 안전한 초기화
function initScrollHeader() {
    console.log('[SCROLL HEADER] 초기화 시작');
    new ScrollHeaderController();
}

// 다양한 시점에서 초기화 시도
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initScrollHeader);
} else {
    initScrollHeader();
}

// Blazor 환경 대응 - 추가 지연
setTimeout(initScrollHeader, 500);
setTimeout(initScrollHeader, 1500);