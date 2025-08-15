// GTD Board 드래그 앤 드롭 - 메인 진입점

import * as Constants from './modules/constants.js';
import { handler } from './modules/drag-main.js';
import { onSidebarToggled } from './sidebar.js';    /* 사이드바 버스 */
import * as utils from './modules/utils.js';         /* ⬅️ 추가 */

// ===== 메인 설정 함수 =====
export function setup(helper) {
    Constants.setDotNetHelper(helper);

    // 🆕 추가: 모달 상태 체크 함수
    const isModalOpen = () => {
        return document.body.classList.contains('disable-task-interaction') ||
            document.body.classList.contains('disable-task-selection') ||
            document.querySelector('.modal-container') !== null;
    };

    // 🔧 기존 이벤트 리스너를 래핑하여 모달 상태 체크
    const wrappedHandler = (e) => {
        if (isModalOpen()) {
            console.log('[DRAG] 모달 열린 상태 - 드래그 이벤트 무시');
            return;
        }
        handler(e);
    };

    // 기존 이벤트 리스너 제거 후 새로 등록
    ["touchstart", "touchmove", "touchend", "touchcancel"].forEach(ev => {
        document.removeEventListener(ev, handler, true);
        document.removeEventListener(ev, wrappedHandler, true);
        document.addEventListener(ev, wrappedHandler, { passive: false, capture: true });
    });

    // ✨ [핵심 수정] 태블릿에서 롱터치 시 컨텍스트 메뉴가 뜨는 현상 방지
    // .task-node-self 요소 위에서 발생하는 contextmenu 이벤트를 감지하여 브라우저 기본 동작을 차단합니다.
    document.addEventListener('contextmenu', function (e) {
        // 이벤트가 드래그 가능한 작업 항목 내부에서 발생했는지 확인합니다.
        if (e.target.closest('.task-node-self')) {
            // 기본 컨텍스트 메뉴가 나타나는 것을 막습니다.
            e.preventDefault();
        }
    }, { passive: false });


    console.log("[DRAG] GTD Board 드래그 시스템 초기화 완료 (모달 보호 및 컨텍스트 메뉴 차단 포함)");

    /* 사이드바 열림 시 RAF 취소 – 좌표 틀어짐 방지 */
    onSidebarToggled(() => {
        utils.cancelScheduledUpdate(); // 이제 utils 는 모듈 스코프에 존재
    });
}

/*
    이제 각 기능별로 모듈이 분리되어 있습니다:
    
    - constants.js: 상수 및 전역 변수 관리
    - utils.js: 햅틱, 애니메이션, 성능 최적화 유틸리티
    - drag-detection.js: 터치 감지 및 요소 찾기
    - drag-calculation.js: 드롭 위치 계산 및 검증
    - drag-visual.js: 시각적 피드백 및 하이라이트
    - drag-main.js: 메인 드래그 로직 및 이벤트 핸들러
    - gtd-board.js: 진입점 및 통합
*/