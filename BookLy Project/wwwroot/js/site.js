// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Bộ lọc danh sách dùng chung: tìm kiếm theo nội dung và lọc theo trạng thái.
document.addEventListener('DOMContentLoaded', () => {
    const configs = [
                { selector: '.category-row', title: 'Lọc thể loại', states: [] },
        { selector: '.user-row', title: 'Lọc người dùng', states: ['Active', 'Locked', 'Hoạt động', 'Đã khóa'] },
                { selector: '.borrow-filter-row', title: 'Lọc phiếu mượn', states: ['Pending', 'Approved', 'Overdue', 'Returned', 'Cancelled', 'Chờ duyệt', 'Đang mượn', 'Quá hạn', 'Đã trả', 'Đã hủy'] },
        { selector: '.book-filter-card', title: 'Lọc sách', states: ['Còn sách', 'Hết sách'] },
        { selector: '.borrow-history-card', title: 'Lọc lịch sử mượn', states: ['Pending', 'Approved', 'Overdue', 'Returned', 'Cancelled', 'Chờ duyệt', 'Đang mượn', 'Quá hạn', 'Đã trả', 'Đã hủy'] }
    ];

    configs.forEach((config, index) => {
        const items = Array.from(document.querySelectorAll(config.selector));
        if (!items.length) return;
        const isTableRow = items[0].tagName === 'TR';
        const host = isTableRow ? items[0].closest('table')?.parentElement : items[0].parentElement;
        if (!host || host.querySelector(':scope > .list-filter-panel')) return;

        const panel = document.createElement('div');
        panel.className = 'list-filter-panel';
        panel.style.cssText = 'display:flex;flex-wrap:wrap;gap:10px;margin:0 0 16px;padding:14px;background:#f8fafc;border:1px solid #e2e8f0;border-radius:12px;';
        const search = document.createElement('input');
        search.type = 'search'; search.placeholder = 'Tìm kiếm trong danh sách...';
        search.setAttribute('aria-label', config.title);
        search.style.cssText = 'flex:1;min-width:210px;padding:10px 12px;border:1px solid #cbd5e1;border-radius:8px;';
        panel.appendChild(search);

        let state = null;
        if (config.states.length) {
            state = document.createElement('select');
            state.style.cssText = 'min-width:170px;padding:10px 12px;border:1px solid #cbd5e1;border-radius:8px;background:white;';
            state.innerHTML = '<option value="">Tất cả trạng thái</option>' + [...new Set(config.states)].map(x => `<option value="${x}">${x}</option>`).join('');
            panel.appendChild(state);
        }
        const count = document.createElement('span');
        count.style.cssText = 'align-self:center;color:#64748b;font-size:13px;';
        panel.appendChild(count);
        host.insertBefore(panel, isTableRow ? items[0].closest('table') : items[0]);

        const apply = () => {
            const query = search.value.trim().toLocaleLowerCase('vi');
            const selected = state?.value.toLocaleLowerCase('vi') || '';
            let shown = 0;
            items.forEach(item => {
                const content = item.innerText.toLocaleLowerCase('vi');
                const matchesText = !query || content.includes(query);
                const matchesState = !selected || content.includes(selected);
                item.hidden = !(matchesText && matchesState);
                if (!item.hidden) shown++;
            });
            count.textContent = `Hiển thị ${shown}/${items.length}`;
        };
        search.addEventListener('input', apply);
        state?.addEventListener('change', apply);
        apply();
    });
});
