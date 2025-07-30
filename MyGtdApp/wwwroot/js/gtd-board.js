// wwwroot/js/gtd-board.js
// ------------------------------------------------------------
// ① 롱-프레스(200 ms) → 드래그 시작
// ② 세로 10 px 이상 먼저 움직이면 스크롤 우선
// ③ elementFromPoint 용 display:none → 원래 값 복구 버그 수정
// ------------------------------------------------------------

let dotNetHelper = null;

let startX = 0, startY = 0, startTime = 0;
const DRAG_DELAY = 200;  // 길게 누르기 시간(ms)
const MOVE_TOLERANCE = 10;   // 스크롤 판단 Y 이동(px)

let pressTimer = null;
let candidateElement = null;  // 아직 드래그 확정 안 된 상태
let draggedElement = null;  // 드래그 중인 실제 요소
let draggedTaskId = null;
let savedDisplay = '';    // draggedElement 원래 display 값
let isDragging = false;

/* ----------------------------------------------------------------
 * set-up
 * ---------------------------------------------------------------- */
export function setup(helper) {
    dotNetHelper = helper;

    ["touchstart", "touchmove", "touchend", "touchcancel"].forEach(ev => {
        document.removeEventListener(ev, handler, true);
        document.addEventListener(ev, handler, { passive: false, capture: true });
    });
}

/* ----------------------------------------------------------------
 * 공통 이벤트 라우터
 * ---------------------------------------------------------------- */
function handler(e) {
    switch (e.type) {
        case "touchstart": onStart(e); break;
        case "touchmove": onMove(e); break;
        case "touchend":
        case "touchcancel": onEnd(e); break;
    }
}

/* ----------------------------------------------------------------
 * touchstart
 * ---------------------------------------------------------------- */
function onStart(e) {
    // 버튼·체크박스 등은 드래그 무시
    if (e.target.closest("button, input[type=checkbox], .sidebar-toggle-btn, .mobile-header")) return;

    candidateElement = e.target.closest(".task-node-self");
    if (!candidateElement) return;

    const t = e.touches[0];
    startX = t.clientX;
    startY = t.clientY;
    startTime = Date.now();

    // 일정 시간 후 드래그 모드 돌입
    pressTimer = setTimeout(beginDrag, DRAG_DELAY);
}

/* ----------------------------------------------------------------
 * touchmove
 * ---------------------------------------------------------------- */
function onMove(e) {
    if (!candidateElement && !isDragging) return;

    const t = e.touches[0];
    const dx = t.clientX - startX;
    const dy = t.clientY - startY;

    /* 아직 드래그 확정 전 : 세로 MOVE_TOLERANCE 초과 → 스크롤 */
    if (!isDragging) {
        if (Math.abs(dy) > MOVE_TOLERANCE) {
            cleanAll();                 // pressTimer 취소 및 변수 초기화
            return;                     // 스크롤 동작을 그대로 허용
        }
        // 가만히 있거나 가로로 살짝 이동 중 → 계속 대기
        return;
    }

    /* ─────────── 이미 드래그 중 ─────────── */
    e.preventDefault(); // 페이지 스크롤 방지

    hideTemp(draggedElement);
    const elUnder = document.elementFromPoint(t.clientX, t.clientY);
    showTemp(draggedElement);

    highlightDropTarget(elUnder, t.clientY);
}

/* ----------------------------------------------------------------
 * touchend / touchcancel
 * ---------------------------------------------------------------- */
function onEnd(e) {
    clearTimeout(pressTimer);

    // 드래그 모드가 아니었다면 단순 탭/스크롤
    if (!isDragging) {
        cleanAll();
        return;
    }

    const t = e.changedTouches[0];

    hideTemp(draggedElement);
    const elUnder = document.elementFromPoint(t.clientX, t.clientY);
    showTemp(draggedElement);

    const dropTarget = elUnder ? elUnder.closest(".task-node-self") : null;

    if (dropTarget && dotNetHelper) {
        const targetId = +dropTarget.dataset.taskId;
        if (targetId !== draggedTaskId) {
            let pos = "Inside";
            if (dropTarget.classList.contains("drop-above")) pos = "Above";
            else if (dropTarget.classList.contains("drop-below")) pos = "Below";

            dotNetHelper.invokeMethodAsync("HandleDropOnProject", targetId, pos);
        }
    }

    cleanAll();
}

/* ----------------------------------------------------------------
 * 드래그 시작
 * ---------------------------------------------------------------- */
function beginDrag() {
    if (!candidateElement) return;

    isDragging = true;
    draggedElement = candidateElement;
    draggedTaskId = +draggedElement.dataset.taskId;
    savedDisplay = draggedElement.style.display || '';  // 원래 값 백업

    draggedElement.classList.add("is-ghost");
}

/* ----------------------------------------------------------------
 * drop 하이라이트 표시
 * ---------------------------------------------------------------- */
function highlightDropTarget(elUnder, clientY) {
    document
        .querySelectorAll(".drop-above, .drop-inside, .drop-below")
        .forEach(el => el.classList.remove("drop-above", "drop-inside", "drop-below"));

    const dropTarget = elUnder ? elUnder.closest(".task-node-self") : null;
    if (!dropTarget || dropTarget === draggedElement) return;

    const h = dropTarget.offsetHeight;
    const rect = dropTarget.getBoundingClientRect();
    const offset = clientY - rect.top;
    const zone = h / 3;

    if (offset < zone) dropTarget.classList.add("drop-above");
    else if (offset > h - zone) dropTarget.classList.add("drop-below");
    else dropTarget.classList.add("drop-inside");
}

/* ----------------------------------------------------------------
 * 임시 숨김 / 복원
 * ---------------------------------------------------------------- */
function hideTemp(el) { el.style.display = "none"; }
function showTemp(el) { el.style.display = savedDisplay; }

/* ----------------------------------------------------------------
 * 정리
 * ---------------------------------------------------------------- */
function cleanAll() {
    // 혹시라도 복원 안 된 경우 대비
    if (draggedElement) draggedElement.style.display = savedDisplay;

    document
        .querySelectorAll(".drop-above, .drop-inside, .drop-below, .is-ghost")
        .forEach(el =>
            el.classList.remove("drop-above", "drop-inside", "drop-below", "is-ghost")
        );

    candidateElement = draggedElement = null;
    draggedTaskId = null;
    savedDisplay = '';
    isDragging = false;
}
