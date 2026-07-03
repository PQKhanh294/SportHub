/**
 * SportHub onboarding tour — spotlight overlay tự viết, không dependency.
 * Chạy tự động ở lần đăng nhập đầu (window.__spFirstLogin), có thể chạy lại qua SportHubTour.restart().
 */
window.SportHubTour = (function () {
    var steps = [
        { sel: '#nav-create-match',       title: 'Tạo trận đấu',   text: 'Đăng kèo tìm người chơi cùng — chế độ Nhanh chỉ cần 3 trường.' },
        { sel: '#nav-matchmaking-desktop', title: 'Ghép trận',      text: 'Tìm và tham gia các trận đấu quanh bạn.' },
        { sel: '#nav-wallet-btn',         title: 'Ví xu',           text: 'Nạp xu để đặt cọc khi tạo trận và thanh toán phí tham gia.' },
        { sel: '#nav-notif-btn',          title: 'Thông báo',       text: 'Theo dõi duyệt trận, nhắc lịch thi đấu và khuyến mãi tại đây.' },
        { sel: '#nav-avatar',             title: 'Hồ sơ của bạn',   text: 'Cập nhật môn thể thao & trình độ để ghép trận chuẩn hơn.' }
    ];

    var idx = 0, overlay = null, tip = null, spot = null;

    function visible(el) {
        if (!el) return false;
        var r = el.getBoundingClientRect();
        return r.width > 0 && r.height > 0;
    }

    function activeSteps() {
        return steps.filter(function (s) { return visible(document.querySelector(s.sel)); });
    }

    function build() {
        overlay = document.createElement('div');
        overlay.style.cssText = 'position:fixed;inset:0;z-index:10500;';
        spot = document.createElement('div');
        spot.style.cssText = 'position:absolute;border-radius:12px;box-shadow:0 0 0 9999px rgba(0,0,0,.62);transition:all .3s ease;pointer-events:none;';
        tip = document.createElement('div');
        tip.style.cssText = 'position:absolute;max-width:290px;background:#fff;border-radius:12px;padding:14px 16px;box-shadow:0 10px 40px rgba(0,0,0,.3);transition:all .3s ease;';
        overlay.appendChild(spot);
        overlay.appendChild(tip);
        document.body.appendChild(overlay);
        document.body.style.overflow = 'hidden';
    }

    function show(list) {
        var step = list[idx];
        var el = document.querySelector(step.sel);
        if (!el) { next(list); return; }
        var r = el.getBoundingClientRect();
        var pad = 6;
        spot.style.left = (r.left - pad) + 'px';
        spot.style.top = (r.top - pad) + 'px';
        spot.style.width = (r.width + pad * 2) + 'px';
        spot.style.height = (r.height + pad * 2) + 'px';

        tip.innerHTML =
            '<p style="font-weight:800;font-size:14px;color:#0f172a;margin-bottom:4px;">' + step.title + '</p>' +
            '<p style="font-size:12.5px;color:#475569;line-height:1.5;">' + step.text + '</p>' +
            '<div style="display:flex;align-items:center;justify-content:space-between;margin-top:12px;">' +
                '<span style="font-size:11px;color:#94a3b8;font-weight:600;">' + (idx + 1) + '/' + list.length + '</span>' +
                '<span style="display:flex;gap:8px;">' +
                    '<button id="spTourSkip" style="font-size:12px;color:#64748b;font-weight:600;padding:5px 10px;">Bỏ qua</button>' +
                    '<button id="spTourNext" style="font-size:12px;color:#fff;font-weight:700;background:#50A5B1;border-radius:8px;padding:5px 14px;">' +
                        (idx === list.length - 1 ? 'Hoàn tất' : 'Tiếp theo →') + '</button>' +
                '</span>' +
            '</div>';

        // Đặt tooltip dưới element; tràn màn hình thì đặt lên trên / kẹp mép
        var tw = 290, th = 130;
        var left = Math.min(Math.max(r.left, 12), window.innerWidth - tw - 12);
        var top = r.bottom + 14;
        if (top + th > window.innerHeight) top = Math.max(r.top - th - 14, 12);
        tip.style.left = left + 'px';
        tip.style.top = top + 'px';

        document.getElementById('spTourSkip').onclick = end;
        document.getElementById('spTourNext').onclick = function () { next(list); };
    }

    function next(list) {
        idx++;
        if (idx >= list.length) { end(); return; }
        show(list);
    }

    function end() {
        localStorage.setItem('spTourDone', '1');
        if (overlay) overlay.remove();
        overlay = null;
        document.body.style.overflow = '';
    }

    function start() {
        if (overlay) return;
        var list = activeSteps();
        if (list.length === 0) return;
        idx = 0;
        build();
        show(list);
    }

    // Tự chạy ở lần đăng nhập đầu
    function autoStart() {
        if (!window.__spFirstLogin) return;
        if (localStorage.getItem('spTourDone')) return;
        // Nhường popup hoàn thiện hồ sơ nếu đang mở
        var pcm = document.getElementById('pcmOverlay');
        if (pcm && !pcm.classList.contains('hidden')) return;
        setTimeout(start, 1200);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', autoStart);
    } else {
        autoStart();
    }

    return {
        start: start,
        restart: function () { localStorage.removeItem('spTourDone'); start(); }
    };
})();
