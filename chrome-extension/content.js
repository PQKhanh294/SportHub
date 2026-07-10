// SportHub — content script chạy trên facebook.com
// Facebook đổi cấu trúc DOM (class ngẫu nhiên) thường xuyên, nên mọi selector ở đây đều có fallback —
// nếu Facebook đổi giao diện, việc trích xuất có thể sai và cần cập nhật lại các heuristic bên dưới.

(function () {
    const PROCESSED_ATTR = 'data-sporthub-injected';

    function extractPostText(article) {
        const preview = article.querySelector('[data-ad-preview="message"]');
        if (preview && preview.innerText.trim().length > 0) return preview.innerText.trim();

        // Fallback: nội dung bài viết thường nằm trong các div[dir="auto"] khá dài — lấy đoạn dài nhất,
        // loại trừ text nằm trong khu vực bình luận (<ul>) để không lấy nhầm 1 comment dài hơn caption gốc
        const candidates = Array.from(article.querySelectorAll('div[dir="auto"]'))
            .filter(el => !el.closest('ul'))
            .map(el => el.innerText.trim())
            .filter(t => t.length > 20);
        if (candidates.length > 0) {
            return candidates.reduce((a, b) => (b.length > a.length ? b : a));
        }

        return article.innerText.trim().slice(0, 2000);
    }

    function extractPermalink(article) {
        const anchors = Array.from(article.querySelectorAll('a[href]'));

        // Ưu tiên link timestamp (thường là permalink thật của bài viết)
        const timeLink = anchors.find(a => a.querySelector('abbr') || /\d+\s*(giờ|phút|ngày|tuần|h|m|d|w)\b/i.test(a.getAttribute('aria-label') || ''));
        if (timeLink) return absoluteUrl(timeLink.href);

        const permalinkPattern = /\/(posts|permalink\.php|videos)\//;
        const byPattern = anchors.find(a => permalinkPattern.test(a.href) || a.href.includes('story_fbid='));
        if (byPattern) return absoluteUrl(byPattern.href);

        return location.href.split('?')[0];
    }

    function absoluteUrl(href) {
        try {
            const u = new URL(href, location.href);
            u.search = '';
            return u.toString();
        } catch {
            return href;
        }
    }

    function extractAuthorName(article) {
        const strong = article.querySelector('h2 a[role="link"], h3 a[role="link"], strong a');
        if (strong && strong.innerText.trim()) return strong.innerText.trim();
        const anyLink = article.querySelector('a[role="link"] > span > span');
        return anyLink ? anyLink.innerText.trim() : null;
    }

    function buildButton() {
        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'sporthub-capture-btn';
        btn.textContent = '🏸 Gửi về SportHub';
        return btn;
    }

    function openReviewPanel(article, btn) {
        const rawText = extractPostText(article);
        const sourceUrl = extractPermalink(article);
        const authorName = extractAuthorName(article);

        const existing = document.getElementById('sporthub-review-panel');
        if (existing) existing.remove();

        const panel = document.createElement('div');
        panel.id = 'sporthub-review-panel';
        panel.className = 'sporthub-panel';
        panel.innerHTML = `
            <div class="sporthub-panel-header">
                <span>Xác nhận gửi về SportHub</span>
                <button type="button" class="sporthub-panel-close">✕</button>
            </div>
            <label>Nội dung bài viết</label>
            <textarea class="sporthub-panel-text" rows="6"></textarea>
            <label>Link bài gốc</label>
            <input type="text" class="sporthub-panel-url" />
            <label>Người đăng</label>
            <input type="text" class="sporthub-panel-author" />
            <div class="sporthub-panel-status"></div>
            <div class="sporthub-panel-actions">
                <button type="button" class="sporthub-panel-cancel">Hủy</button>
                <button type="button" class="sporthub-panel-submit">Gửi</button>
            </div>
        `;
        document.body.appendChild(panel);

        panel.querySelector('.sporthub-panel-text').value = rawText;
        panel.querySelector('.sporthub-panel-url').value = sourceUrl;
        panel.querySelector('.sporthub-panel-author').value = authorName || '';

        const close = () => panel.remove();
        panel.querySelector('.sporthub-panel-close').addEventListener('click', close);
        panel.querySelector('.sporthub-panel-cancel').addEventListener('click', close);

        panel.querySelector('.sporthub-panel-submit').addEventListener('click', () => {
            const statusEl = panel.querySelector('.sporthub-panel-status');
            const submitBtn = panel.querySelector('.sporthub-panel-submit');
            const payload = {
                rawText: panel.querySelector('.sporthub-panel-text').value.trim(),
                sourceUrl: panel.querySelector('.sporthub-panel-url').value.trim(),
                sourceAuthorName: panel.querySelector('.sporthub-panel-author').value.trim() || null
            };
            if (!payload.rawText || !payload.sourceUrl) {
                statusEl.textContent = 'Thiếu nội dung hoặc link bài viết.';
                statusEl.className = 'sporthub-panel-status error';
                return;
            }

            submitBtn.disabled = true;
            statusEl.textContent = 'Đang gửi...';
            statusEl.className = 'sporthub-panel-status';

            // Guard chống treo UI vĩnh viễn nếu message tới service worker bị rớt (MV3 service worker
            // có thể tự ngủ giữa chừng) — không có timeout thì "Đang gửi..." có thể đứng mãi không báo lỗi.
            let settled = false;
            const timeoutId = setTimeout(() => {
                if (settled) return;
                settled = true;
                submitBtn.disabled = false;
                statusEl.textContent = 'Quá thời gian chờ (15s) — thử bấm Gửi lại. Nếu vẫn vậy, mở Console (F12) trên trang này để xem log [SportHub].';
                statusEl.className = 'sporthub-panel-status error';
                console.warn('[SportHub] Timeout: không nhận phản hồi từ background sau 15s.', payload);
            }, 15000);

            console.log('[SportHub] Gửi message tới background:', payload);
            chrome.runtime.sendMessage({ type: 'submit', payload }, (response) => {
                if (settled) return;
                settled = true;
                clearTimeout(timeoutId);
                submitBtn.disabled = false;

                if (chrome.runtime.lastError) {
                    console.error('[SportHub] chrome.runtime.lastError:', chrome.runtime.lastError.message);
                    statusEl.textContent = 'Lỗi kết nối tới extension: ' + chrome.runtime.lastError.message;
                    statusEl.className = 'sporthub-panel-status error';
                    return;
                }
                if (!response) {
                    console.error('[SportHub] Không nhận được response từ background.js.');
                    statusEl.textContent = 'Không nhận được phản hồi từ extension.';
                    statusEl.className = 'sporthub-panel-status error';
                    return;
                }

                console.log('[SportHub] Response từ background:', response);
                if (response.ok) {
                    statusEl.textContent = `Đã gửi ✓ (${response.data.parseStatus === 'Parsed' ? 'AI đã bóc tách' : 'AI chưa bóc tách được, vẫn hiển thị dạng thô'})`;
                    statusEl.className = 'sporthub-panel-status success';
                    btn.textContent = '✅ Đã gửi';
                    btn.disabled = true;
                    setTimeout(close, 1800);
                } else {
                    statusEl.textContent = response.error || 'Gửi thất bại.';
                    statusEl.className = 'sporthub-panel-status error';
                }
            });
        });
    }

    // role="article" KHÔNG đáng tin cho bài viết gốc trên Facebook hiện tại (đã kiểm chứng thực tế:
    // bài không có bình luận cũng chẳng có role="article" nào) — chỉ bình luận mới chắc chắn có.
    // Đổi chiến lược: nhận diện bài viết qua nhãn nút "Bình luận" (Facebook dùng "Trả lời" riêng cho
    // bình luận, không trùng chữ) — đây là text người dùng nhìn thấy, ổn định hơn class/role nội bộ
    // hay đổi. Leo từ nhãn đó lên tới tổ tiên gần nhất bao trọn đúng 1 bài viết (dừng ngay khi tổ tiên
    // kế tiếp đã chứa nhãn "Bình luận" của bài khác trong feed).
    const LABEL_PROCESSED_ATTR = 'data-sporthub-label-seen';

    function isExactLeafText(el, text) {
        return el.children.length === 0 && el.textContent.trim() === text;
    }

    function countCommentActionsIn(root) {
        return Array.from(root.querySelectorAll('div, span')).filter(el => isExactLeafText(el, 'Bình luận')).length;
    }

    function findPostCards() {
        const labels = Array.from(document.querySelectorAll('div, span'))
            .filter(el => isExactLeafText(el, 'Bình luận') && !el.getAttribute(LABEL_PROCESSED_ATTR));

        const cards = new Set();
        labels.forEach(label => {
            label.setAttribute(LABEL_PROCESSED_ATTR, '1');
            let node = label;
            while (node.parentElement && countCommentActionsIn(node.parentElement) <= 1) {
                node = node.parentElement;
            }
            cards.add(node);
        });
        return Array.from(cards);
    }

    function injectButtons() {
        findPostCards().forEach(card => {
            if (card.getAttribute(PROCESSED_ATTR)) return;
            card.setAttribute(PROCESSED_ATTR, '1');

            card.style.position = card.style.position || 'relative';
            const btn = buildButton();
            btn.addEventListener('click', (e) => {
                e.preventDefault();
                e.stopPropagation();
                openReviewPanel(card, btn);
            });
            card.appendChild(btn);
        });
    }

    // Debounce — Facebook mutate DOM liên tục, tránh quét lại toàn trang ở mỗi mutation nhỏ
    let scheduled = false;
    function scheduleInject() {
        if (scheduled) return;
        scheduled = true;
        requestAnimationFrame(() => {
            scheduled = false;
            injectButtons();
        });
    }

    const observer = new MutationObserver(() => scheduleInject());
    observer.observe(document.body, { childList: true, subtree: true });
    scheduleInject();
})();
