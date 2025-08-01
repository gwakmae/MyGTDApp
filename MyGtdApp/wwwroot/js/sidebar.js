// wwwroot/js/sidebar.js   (ES module)
export function toggleSidebar() {
    const sidebar = document.querySelector('.sidebar');
    const backdrop = document.querySelector('.sidebar-backdrop');
    const isOpen = sidebar.classList.toggle('is-open');   // 토글

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

    return isOpen;                 // C# 으로 상태 반환
}
