// wwwroot/js/gtd-board.js (수정된 최종본)

let dotNetHelper = null; // C# Home.razor 컴포넌트를 담을 변수
let draggedTaskId = null;
let draggedElement = null; // 드래그 중인 요소를 저장할 변수

// C#에서 호출할 초기화 함수
window.setup = (helper) => {
    dotNetHelper = helper;
};

// --- 기존 이벤트 리스너 제거 (중복 방지) ---
// document.removeEventListener('touchstart', handleTouchStart);
// document.removeEventListener('touchmove', handleTouchMove);
// document.removeEventListener('touchend', handleTouchEnd);

// --- 새로운 이벤트 리스너 등록 ---
document.addEventListener('touchstart', handleTouchStart, { passive: false });
document.addEventListener('touchmove', handleTouchMove, { passive: false });
document.addEventListener('touchend', handleTouchEnd);


function handleTouchStart(event) {
    const target = event.target.closest('.task-node-self');
    if (target) {
        // 기본 스크롤 동작을 막아 드래그가 시작될 수 있도록 함
        event.preventDefault();

        draggedElement = target;
        const taskId = target.getAttribute('data-task-id');
        if (taskId) {
            draggedTaskId = parseInt(taskId);
            // 시각적 효과를 위해 'is-ghost' 클래스 추가
            target.classList.add('is-ghost');
        }
    }
}

function handleTouchMove(event) {
    if (!draggedTaskId || !draggedElement) return;

    // 스크롤 방지
    event.preventDefault();

    // 현재 터치 위치에 있는 요소 찾기
    const touch = event.touches[0];
    draggedElement.style.display = 'none'; // 임시로 드래그 요소를 숨겨야 그 아래 요소를 찾을 수 있음
    const elementUnderTouch = document.elementFromPoint(touch.clientX, touch.clientY);
    draggedElement.style.display = ''; // 다시 표시

    // 모든 기존 드롭 표시기 제거
    document.querySelectorAll('.drop-above, .drop-inside, .drop-below').forEach(el => {
        el.classList.remove('drop-above', 'drop-inside', 'drop-below');
    });

    const dropTarget = elementUnderTouch ? elementUnderTouch.closest('.task-node-self') : null;

    if (dropTarget) {
        // 드롭 표시기 클래스 추가 로직 (ProjectTaskNode.razor의 로직과 유사하게)
        const elementHeight = dropTarget.offsetHeight;
        const dropZoneHeight = elementHeight / 3.0;
        const rect = dropTarget.getBoundingClientRect();
        const offsetY = touch.clientY - rect.top;

        if (offsetY < dropZoneHeight) {
            dropTarget.classList.add('drop-above');
        } else if (offsetY > elementHeight - dropZoneHeight) {
            dropTarget.classList.add('drop-below');
        } else {
            dropTarget.classList.add('drop-inside');
        }
    }
}

function handleTouchEnd(event) {
    if (!draggedTaskId) return;

    // 모든 기존 드롭 표시기 및 고스트 효과 제거
    document.querySelectorAll('.drop-above, .drop-inside, .drop-below, .is-ghost').forEach(el => {
        el.classList.remove('drop-above', 'drop-inside', 'drop-below', 'is-ghost');
    });

    // 드롭 위치의 요소 찾기
    const touch = event.changedTouches[0];
    const elementUnderTouch = document.elementFromPoint(touch.clientX, touch.clientY);
    const dropTarget = elementUnderTouch ? elementUnderTouch.closest('.task-node-self') : null;

    if (dropTarget && dotNetHelper) {
        const targetTaskId = parseInt(dropTarget.getAttribute('data-task-id'));

        let position = 'Inside';
        if (dropTarget.classList.contains('drop-above')) position = 'Above';
        else if (dropTarget.classList.contains('drop-below')) position = 'Below';

        // 저장해둔 dotNetHelper 객체를 통해 C# 인스턴스 메서드를 호출!
        dotNetHelper.invokeMethodAsync('HandleDropOnProject', targetTaskId, position);
    }

    // 드래그 상태 초기화
    draggedTaskId = null;
    draggedElement = null;
}