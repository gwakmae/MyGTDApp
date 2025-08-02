// wwwroot/js/sidebar.js    (ES module)
// (수정) utils 모듈을 직접 가져온다
import * as utils from './modules/utils.js';

/* 이벤트 버스 구현 */
const listeners = [];
export function onSidebarToggled(fn) { listeners.push(fn); }
function emit(state) { listeners.forEach(f => f(state)); }

export function toggleSidebar() {
    const sidebar = document.querySelector('.sidebar');
    const backdrop = document.querySelector('.sidebar-backdrop');
    const isOpen = sidebar.classList.toggle('is-open');

    // 사이드바가 열리거나 닫힐 때 애니메이션 예약 취소
    utils.cancelScheduledUpdate();    // ← 이제 오류 없이 호출 가능

    emit(isOpen);       // Blazor 쪽에도 상태 알림
    return isOpen;      // C# 에서 받을 반환값

    if (isOpen) {
        // backdrop 없으면 생성
        if (!backdrop) {
            const b = document.createElement('div');
            b.className = 'sidebar-backdrop is-open';
            b.addEventListener('click', () => {
                sidebar.classList.remove('is-open');
                b.remove();
            });
            document.querySelector('.page')?.appendChild(b);
        } else {
            backdrop.classList.add('is-open');
        }
    } else {
        backdrop?.classList.remove('is-open');
        backdrop?.remove();
    }
}