// 파일 경로: wwwroot/js/column-resizer.js

// 전역 인스턴스를 두어, 이벤트 리스너가 중복 등록되는 것을 방지합니다.
let columnResizerInstance = null;

class ColumnResizer {
    constructor() {
        this.isDesktop = window.innerWidth >= 1200;
        this.isResizing = false;
        this.currentResizingColumn = null;
        this.startX = 0;
        this.startWidth = 0;

        // 마우스 이벤트는 document에 한 번만 등록하여, 핸들 밖으로 마우스가 나가도 계속 드래그 되도록 합니다.
        document.addEventListener('mousemove', this.handleMouseMove.bind(this), true);
        document.addEventListener('mouseup', this.handleMouseUp.bind(this), true);
        window.addEventListener('resize', this.handleWindowResize.bind(this));
    }

    // Blazor가 렌더링 후 호출할 메인 함수입니다.
    initialize(containerElement) {
        if (!containerElement) return;

        this.isDesktop = window.innerWidth >= 1200;

        // 함수가 재호출될 때를 대비해, 이전에 생성된 핸들을 모두 제거하여 초기화합니다.
        containerElement.querySelectorAll('.column-resizer').forEach(r => r.remove());

        if (this.isDesktop) {
            const columns = containerElement.querySelectorAll('.board-column.resizable');
            columns.forEach(column => this.addResizer(column));
            this.loadColumnWidths(containerElement);
        } else {
            // [수정] 모바일/태블릿 뷰로 전환될 때, 인라인 스타일을 모두 제거합니다.
            const columns = containerElement.querySelectorAll('.board-column.resizable');
            columns.forEach(column => {
                column.style.width = '';
                column.style.flex = '';
            });
        }
    }

    addResizer(column) {
        const resizer = document.createElement('div');
        resizer.className = 'column-resizer';
        resizer.title = '드래그하여 크기 조절 • 더블클릭으로 초기화';

        resizer.addEventListener('mousedown', (e) => {
            e.preventDefault(); // 페이지의 다른 요소가 선택되는 것을 방지
            this.isResizing = true;
            this.currentResizingColumn = column;
            this.startX = e.clientX;
            this.startWidth = column.offsetWidth; // 현재 요소의 실제 렌더링된 너비를 가져옵니다.
            document.body.classList.add('is-resizing'); // body에 클래스를 추가하여 전역 스타일 제어
            document.body.style.userSelect = 'none'; // 드래그 중 텍스트가 선택되는 현상 방지
        });

        resizer.addEventListener('dblclick', () => {
            column.style.width = '280px';
            column.style.flex = '0 0 280px';
            this.saveColumnWidth(column);
        });

        column.appendChild(resizer);
    }

    handleMouseMove(e) {
        if (!this.isResizing) return;
        const newWidth = this.startWidth + (e.clientX - this.startX);
        if (newWidth >= 250 && newWidth <= 600) { // 최소/최대 너비 제한
            this.currentResizingColumn.style.width = `${newWidth}px`;
            // flex-basis를 설정하여 다른 flex 아이템에 영향을 주지 않도록 너비를 고정합니다.
            this.currentResizingColumn.style.flex = `0 0 ${newWidth}px`;
        }
    }

    handleMouseUp() {
        if (this.isResizing) {
            this.isResizing = false;
            document.body.classList.remove('is-resizing');
            document.body.style.userSelect = '';
            if (this.currentResizingColumn) {
                this.saveColumnWidth(this.currentResizingColumn);
            }
            this.currentResizingColumn = null;
        }
    }

    handleWindowResize() {
        const previousIsDesktop = this.isDesktop;
        this.isDesktop = window.innerWidth >= 1200;
        if (previousIsDesktop !== this.isDesktop) {
            const container = document.querySelector('.board-container');
            if (container) this.initialize(container);
        }
    }

    saveColumnWidth(column) {
        if (!column) return;
        const header = column.querySelector('.column-header');
        if (header) {
            const columnType = header.textContent.trim();
            localStorage.setItem(`gtd-column-width-${columnType}`, column.style.width);
        }
    }

    loadColumnWidths(containerElement) {
        const columns = containerElement.querySelectorAll('.board-column.resizable');
        columns.forEach(column => {
            const header = column.querySelector('.column-header');
            const columnType = header.textContent.trim();
            const savedWidth = localStorage.getItem(`gtd-column-width-${columnType}`);
            if (savedWidth) {
                column.style.width = savedWidth;
                column.style.flex = `0 0 ${savedWidth}`;
            }
        });
    }
}

// Blazor에서 호출할 수 있도록 함수를 전역 window 객체에 할당합니다.
window.initializeColumnResizers = (containerElement) => {
    if (!columnResizerInstance) {
        columnResizerInstance = new ColumnResizer();
    }
    columnResizerInstance.initialize(containerElement);
};