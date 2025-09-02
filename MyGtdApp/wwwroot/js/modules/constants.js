// 드래그 앤 드롭 관련 상수 및 전역 변수

// ===== 상수 =====
export const DRAG_DELAY = 450;
export const MULTI_TOUCH_SELECTION_DELAY = 800; // 🆕 두 손가락 선택 모드
export const MOVE_TOLERANCE = 15;
export const MIN_DRAG_DISTANCE = 25;
export const MIN_MOVE_AFTER_DRAG = 12;

// ===== 전역 변수 =====
export let dotNetHelper = null;

// 터치 시작 정보
export let startX = 0;
export let startY = 0;
export let startTime = 0;

// 드래그 상태
export let pressTimer = null;
export let multiTouchSelectionTimer = null; // 🆕 두 손가락 타이머
export let isSelectionMode = false;
export let candidateElement = null;
export let draggedElement = null;
export let draggedTaskId = null;
export let savedDisplay = '';
export let isDragging = false;
export let hasMovedEnough = false;
export let lastDropInfo = null;
export let readyToDrag = false;
export let movedAfterDrag = false;

// 🆕 두 손가락 터치 상태
export let isMultiTouch = false;
export let multiTouchStartTime = 0;
export let multiTouchElement = null;

// 드래그 시작 좌표
export let dragStartX = 0;
export let dragStartY = 0;
export let lastTouchEvent = null;

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

export function setMultiTouchSelectionTimer(timer) { // 🆕
    multiTouchSelectionTimer = timer;
}

export function setIsSelectionMode(mode) {
    isSelectionMode = mode;
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

// 🆕 두 손가락 터치 setter들
export function setIsMultiTouch(isMulti) {
    isMultiTouch = isMulti;
}

export function setMultiTouchStartTime(time) {
    multiTouchStartTime = time;
}

export function setMultiTouchElement(element) {
    multiTouchElement = element;
}

// wwwroot/js/modules/constants.js에 추가
export function isValidForSelectionMode() {
    return isMultiTouch && multiTouchElement !== null;
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
    isSelectionMode = false;
    // 🆕 두 손가락 터치 상태 초기화
    isMultiTouch = false;
    multiTouchStartTime = 0;
    multiTouchElement = null;
}