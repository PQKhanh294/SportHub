// Service worker — gọi mạng ở đây (không phải content script) vì fetch từ content script
// vẫn có thể bị chặn bởi CSP connect-src của trang Facebook; service worker không bị ràng buộc bởi CSP đó.

const DEFAULT_BASE_URL = 'https://sporthub-dn.id.vn';

async function getSettings() {
    const { apiKey, baseUrl } = await chrome.storage.local.get(['apiKey', 'baseUrl']);
    return { apiKey: apiKey || '', baseUrl: (baseUrl || DEFAULT_BASE_URL).replace(/\/$/, '') };
}

async function appendLog(entry) {
    const { log } = await chrome.storage.local.get(['log']);
    const next = [entry, ...(log || [])].slice(0, 20);
    await chrome.storage.local.set({ log: next });
}

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (message.type !== 'submit') return false;
    console.log('[SportHub][background] Nhận message submit:', message.payload);

    (async () => {
        const { apiKey, baseUrl } = await getSettings();
        console.log('[SportHub][background] Settings:', { hasApiKey: !!apiKey, baseUrl });
        if (!apiKey) {
            sendResponse({ ok: false, error: 'Chưa cấu hình API Key — mở popup extension để nhập.' });
            return;
        }

        try {
            console.log('[SportHub][background] Đang fetch:', `${baseUrl}/api/community-listings/submit`);
            const res = await fetch(`${baseUrl}/api/community-listings/submit`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-Api-Key': apiKey
                },
                body: JSON.stringify(message.payload)
            });
            console.log('[SportHub][background] Fetch trả về status:', res.status);

            const data = await res.json().catch(() => null);

            if (!res.ok || !data || !data.success) {
                const errMsg = (data && data.message) || `Lỗi HTTP ${res.status}`;
                console.error('[SportHub][background] Lỗi:', errMsg);
                await appendLog({ time: Date.now(), ok: false, title: message.payload.rawText.slice(0, 60), error: errMsg });
                sendResponse({ ok: false, error: errMsg });
                return;
            }

            console.log('[SportHub][background] Thành công:', data);
            await appendLog({ time: Date.now(), ok: true, title: data.title, parseStatus: data.parseStatus });
            sendResponse({ ok: true, data });
        } catch (e) {
            const errMsg = 'Không kết nối được tới SportHub: ' + (e && e.message ? e.message : e);
            console.error('[SportHub][background] Exception khi fetch:', e);
            await appendLog({ time: Date.now(), ok: false, title: message.payload.rawText.slice(0, 60), error: errMsg });
            sendResponse({ ok: false, error: errMsg });
        }
    })();

    return true; // giữ message channel mở cho phản hồi bất đồng bộ
});
