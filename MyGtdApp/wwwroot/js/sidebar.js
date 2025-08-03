// wwwroot/js/sidebar.js
import * as utils from './modules/utils.js';

/* 이벤트 버스 구현 */
const listeners = [];
export function onSidebarToggled(fn) { listeners.push(fn); }
function emit(state) { listeners.forEach(f => f(state)); }

// 드래그 관련 변수
let isDragging = false;
let startX = 0;
let currentX = 0;
let sidebarElement = null;

export function toggleSidebar() {
    const sidebar = document.querySelector('.sidebar');
    const backdrop = document.querySelector('.sidebar-backdrop');
    const isOpen = sidebar.classList.toggle('is-open');

    // 사이드바가 열리거나 닫힐 때 애니메이션 예약 취소
    utils.cancelScheduledUpdate();

    emit(isOpen);
    setupSidebarDragHandlers(sidebar, isOpen);

    if (isOpen) {
        if (!backdrop) {
            const b = document.createElement('div');
            b.className = 'sidebar-backdrop is-open';
            b.addEventListener('click', () => {
                sidebar.classList.remove('is-open');
                b.remove();
                setupSidebarDragHandlers(sidebar, false);
                emit(false);
            });
            document.querySelector('.page')?.appendChild(b);
        } else {
            backdrop.classList.add('is-open');
        }
    } else {
        backdrop?.classList.remove('is-open');
        backdrop?.remove();
    }

    return isOpen; // ✅ 맨 마지막으로 이동
}

// 사이드바 드래그 핸들러 설정
function setupSidebarDragHandlers(sidebar, isOpen) {
    if (!sidebar) return;

    sidebarElement = sidebar;

    if (isOpen) {
        console.log('[SIDEBAR DRAG] 터치 이벤트 리스너 추가됨');
        // 터치 이벤트 리스너 추가
        sidebar.addEventListener('touchstart', handleTouchStart, { passive: false });
        sidebar.addEventListener('touchmove', handleTouchMove, { passive: false });
        sidebar.addEventListener('touchend', handleTouchEnd, { passive: false });
    } else {
        console.log('[SIDEBAR DRAG] 터치 이벤트 리스너 제거됨');
        // 터치 이벤트 리스너 제거
        sidebar.removeEventListener('touchstart', handleTouchStart);
        sidebar.removeEventListener('touchmove', handleTouchMove);
        sidebar.removeEventListener('touchend', handleTouchEnd);
    }
}

function handleTouchStart(e) {
    // 기존 태스크 드래그와 충돌 방지
    if (e.target.closest('.task-node-self')) {
        console.log('[SIDEBAR DRAG] 태스크 노드 감지 - 드래그 무시');
        return;
    }

    isDragging = true;
    startX = e.touches[0].clientX;
    currentX = startX;

    console.log('[SIDEBAR DRAG] 터치 시작:', {
        startX: startX,
        targetElement: e.target.tagName,
        targetClass: e.target.className
    });

    // 터치 시작 시 transition 비활성화 (부드러운 드래그를 위해)
    sidebarElement.style.transition = 'none';
}

function handleTouchMove(e) {
    if (!isDragging) return;

    // 기존 태스크 드래그와 충돌 방지
    if (e.target.closest('.task-node-self')) {
        console.log('[SIDEBAR DRAG] 터치 이동 중 태스크 노드 감지 - 드래그 무시');
        return;
    }

    e.preventDefault(); // 스크롤 방지

    currentX = e.touches[0].clientX;
    const deltaX = currentX - startX;

    console.log('[SIDEBAR DRAG] 터치 이동:', {
        currentX: currentX,
        deltaX: deltaX,
        direction: deltaX < 0 ? '왼쪽' : '오른쪽'
    });

    // 왼쪽으로만 드래그 허용 (음수 값)
    if (deltaX < 0) {
        const translateX = Math.max(deltaX, -250); // 최대 250px (사이드바 너비)
        sidebarElement.style.transform = `translateX(${translateX}px)`;
        console.log('[SIDEBAR DRAG] 사이드바 이동:', translateX);
    }
}

function handleTouchEnd(e) {
    if (!isDragging) return;

    isDragging = false;
    const deltaX = currentX - startX;
    const threshold = -100; // 100px 이상 왼쪽으로 드래그하면 닫기

    console.log('[SIDEBAR DRAG] 터치 종료:', {
        deltaX: deltaX,
        threshold: threshold,
        shouldClose: deltaX < threshold
    });

    // transition 재활성화
    sidebarElement.style.transition = '';
    sidebarElement.style.transform = '';

    if (deltaX < threshold) {
        console.log('[SIDEBAR DRAG] 사이드바 닫기 실행');
        // 사이드바 닫기
        const backdrop = document.querySelector('.sidebar-backdrop');
        sidebarElement.classList.remove('is-open');
        backdrop?.classList.remove('is-open');
        backdrop?.remove();

        setupSidebarDragHandlers(sidebarElement, false);
        emit(false);

        // 햅틱 피드백 (utils 모듈 사용)
        if (utils.triggerHapticFeedback) {
            utils.triggerHapticFeedback('light');
        }
    } else {
        console.log('[SIDEBAR DRAG] 드래그 취소 - 임계값 미달');
    }
}
