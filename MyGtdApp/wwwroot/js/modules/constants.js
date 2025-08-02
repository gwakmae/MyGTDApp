// 드래그 앤 드롭 관련 상수 및 전역 변수

// ===== 상수 =====
export const DRAG_DELAY = 400;
export const MOVE_TOLERANCE = 15;
export const MIN_DRAG_DISTANCE = 25;
export const MIN_MOVE_AFTER_DRAG = 12;    // 드래그 시작 이후 유효 이동량(px)

// ===== 전역 변수 =====
export let dotNetHelper = null;

// 터치 시작 정보
export let startX = 0;
export let startY = 0;
export let startTime = 0;

// 드래그 상태
export let pressTimer = null;
export let candidateElement = null;
export let draggedElement = null;
export let draggedTaskId = null;
export let savedDisplay = '';
export let isDragging = false;
export let hasMovedEnough = false; // This variable is no longer needed but kept for completeness
export let lastDropInfo = null;
export let readyToDrag = false;    // long-press 완료 플래그
export let movedAfterDrag = false; // drag 시작 후 실제로 움직였는가

// 드래그 시작 좌표
export let dragStartX = 0;
export let dragStartY = 0;
export let lastTouchEvent = null; // To store the last touch event for beginDrag()

// ===== Setter 함수들 =====
export function setDotNetHelper(helper) {
    dotNetHelper = helper;
}

export function setStartPosition(x, y, time) {
    startX = x;
    startY = y;
    startTime = time;
}

export function setPressTimer(timer) {
    pressTimer = timer;
}

export function setCandidateElement(element) {
    candidateElement = element;
}

export function setDraggedElement(element) {
    draggedElement = element;
}

export function setDraggedTaskId(id) {
    draggedTaskId = id;
}

export function setSavedDisplay(display) {
    savedDisplay = display;
}

export function setIsDragging(dragging) {
    isDragging = dragging;
}

export function setHasMovedEnough(moved) {
    hasMovedEnough = moved;
}

export function setLastDropInfo(info) {
    lastDropInfo = info;
}

export function setReadyToDrag(ready) {
    readyToDrag = ready;
}

export function setMovedAfterDrag(moved) {
    movedAfterDrag = moved;
}

export function setDragStartPosition(x, y) {
    dragStartX = x;
    dragStartY = y;
}

export function setLastTouchEvent(event) {
    lastTouchEvent = event;
}

// ===== 상태 초기화 =====
export function resetDragState() {
    candidateElement = null;
    draggedElement = null;
    draggedTaskId = null;
    savedDisplay = '';
    isDragging = false;
    hasMovedEnough = false;
    lastDropInfo = null;
    readyToDrag = false;
    movedAfterDrag = false;
    lastTouchEvent = null;
}
