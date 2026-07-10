const DEFAULT_BASE_URL = 'https://sporthub-dn.id.vn';

async function load() {
    const { apiKey, baseUrl, log } = await chrome.storage.local.get(['apiKey', 'baseUrl', 'log']);
    document.getElementById('apiKey').value = apiKey || '';
    document.getElementById('baseUrl').value = baseUrl || DEFAULT_BASE_URL;
    renderLog(log || []);
}

function renderLog(log) {
    const el = document.getElementById('log');
    if (log.length === 0) {
        el.innerHTML = '<p style="color:#94a3b8">Chưa có lần gửi nào.</p>';
        return;
    }
    el.innerHTML = log.map(item => {
        const time = new Date(item.time).toLocaleString('vi-VN');
        const cls = item.ok ? 'log-ok' : 'log-err';
        const text = item.ok
            ? `✓ ${item.title || '(không có tiêu đề)'} — ${item.parseStatus}`
            : `✗ ${item.error}`;
        return `<div class="log-item"><div class="${cls}">${text}</div><div style="color:#94a3b8">${time}</div></div>`;
    }).join('');
}

document.getElementById('saveBtn').addEventListener('click', async () => {
    const apiKey = document.getElementById('apiKey').value.trim();
    const baseUrl = document.getElementById('baseUrl').value.trim() || DEFAULT_BASE_URL;
    await chrome.storage.local.set({ apiKey, baseUrl });
    const status = document.getElementById('saveStatus');
    status.textContent = 'Đã lưu ✓';
    setTimeout(() => { status.textContent = ''; }, 2000);
});

load();
