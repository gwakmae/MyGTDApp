// GTD Board 드래그 앤 드롭 - 메인 진입점

import * as Constants from './modules/constants.js';
import { handler } from './modules/drag-main.js';

// ===== 메인 설정 함수 =====
export function setup(helper) {
    Constants.setDotNetHelper(helper);

    // 기존 이벤트 리스너 제거 후 새로 등록
    ["touchstart", "touchmove", "touchend", "touchcancel"].forEach(ev => {
        document.removeEventListener(ev, handler, true);
        document.addEventListener(ev, handler, { passive: false, capture: true });
    });

    console.log("[DRAG] GTD Board 드래그 시스템 초기화 완료");
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
