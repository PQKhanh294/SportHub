/**
 * SportHub tour — spotlight overlay tự viết, không dependency.
 * - Tour navbar: chạy 1 lần ở lần đăng nhập đầu (window.__spFirstLogin).
 * - Tour theo trang: mỗi trang cốt lõi có bộ bước riêng, tự chạy lần đầu user vào trang.
 * - Nút "?" nổi (góc trái-dưới) trên trang có tour để xem lại bất kỳ lúc nào.
 */
window.SportHubTour = (function () {

    // Anchor desktop và mobile khai báo song song — visible() tự lọc theo màn hình
    var navSteps = [
        { sel: '#nav-create-match',        title: 'Tạo trận đấu', text: 'Đăng kèo tìm người chơi cùng — chế độ Nhanh chỉ cần vài trường.' },
        { sel: '#nav-matchmaking-desktop', title: 'Ghép trận',    text: 'Tìm và tham gia các trận đấu quanh bạn.' },
        { sel: '#bn-matchmaking',          title: 'Ghép trận',    text: 'Tìm và tham gia các trận đấu quanh bạn — kèm nút Tạo trận ngay trong trang.' },
        { sel: '#nav-wallet-btn',          title: 'Ví xu',        text: 'Nạp xu để đặt cọc khi tạo trận và thanh toán phí tham gia.' },
        { sel: '#nav-notif-btn',           title: 'Thông báo',    text: 'Theo dõi duyệt trận, nhắc lịch thi đấu và khuyến mãi tại đây.' },
        { sel: '#nav-avatar',              title: 'Hồ sơ của bạn', text: 'Cập nhật môn thể thao & trình độ để ghép trận chuẩn hơn.' },
        { sel: '#bn-more',                 title: 'Menu Thêm',    text: 'Ví xu, bạn bè, cài đặt và các mục khác nằm trong đây.' }
    ];

    var pageTours = {
        matchmaking: {
            match: function (p) { return p === '/matchmaking' || p === '/matchmaking/index'; },
            steps: [
                { sel: '#mmStatusFilter',        title: 'Trạng thái kèo',  text: 'Chuyển giữa trận Đang mở, Đã tham gia, Đang chờ duyệt hoặc Trận của tôi.' },
                { sel: '#filterSidebar',         title: 'Bộ lọc',          text: 'Lọc theo môn, trình độ (nhấn (i) xem thang điểm từng môn), khu vực, giờ và chi phí.' },
                { sel: '#mmFilterBtnMobile',     title: 'Bộ lọc',          text: 'Nhấn để mở bộ lọc: môn, trình độ, khu vực, giờ và chi phí.' },
                { sel: '.draggable-match-card',  title: 'Card trận đấu',   text: 'Mỗi card hiển thị điểm phù hợp, khoảng cách, giá mỗi người và số chỗ còn trống.' },
                { sel: '.mm-join-btn',           title: 'Gửi yêu cầu',     text: 'Gửi yêu cầu tham gia — host duyệt trong 2 giờ, sau đó bạn thanh toán 5.000 xu để giữ chỗ.' },
                { sel: '#nav-create-match',      title: 'Tạo trận',        text: 'Không thấy kèo phù hợp? Tự tạo trận của bạn chỉ trong 30 giây.' }
            ]
        },
        create: {
            match: function (p) { return p === '/matchmaking/create'; },
            steps: [
                { sel: '#tabQuick',          title: 'Chế độ Nhanh / Đầy đủ', text: 'Nhanh: điền vài trường cơ bản là đăng được kèo. Đầy đủ: tùy chỉnh chi tiết loại trận, trình độ, phí.' },
                { sel: '#quickSportSelect',  title: 'Môn thể thao',          text: 'Chọn môn — loại trận và thang trình độ tự thay đổi theo từng môn.' },
                { sel: '#quickAddress',      title: 'Địa chỉ sân',           text: 'Gõ tên sân hoặc địa chỉ — có gợi ý tự động kèm bản đồ ghim vị trí.' },
                { sel: '#courtSearchInput',  title: 'Tìm sân có sẵn',        text: 'Gõ tên để tìm sân trong hệ thống — tự điền địa chỉ, không cần nhớ ID.' },
                { sel: '#splitFeeSection',   title: 'Chia đều tiền sân',     text: 'Bật để hệ thống tự tính xu mỗi người phải góp (tổng chi phí / số người).' },
                { sel: '#aiDescBtn',         title: 'AI viết mô tả',         text: 'Để AI viết mô tả trận hấp dẫn dựa trên thông tin bạn đã điền.' }
            ]
        },
        details: {
            match: function (p) { return p.indexOf('/matchmaking/details') === 0; },
            steps: [
                { sel: '#mdInfoCard',      title: 'Thông tin trận',      text: 'Ngày giờ, địa điểm, sân số, trình độ yêu cầu và chi phí tham gia.' },
                { sel: '#mdJoinBtn',       title: 'Tham gia trận',       text: 'Gửi yêu cầu → host duyệt (tối đa 1 giờ) → thanh toán 5.000 xu để giữ chỗ.' },
                { sel: '#mdParticipants',  title: 'Người tham gia',      text: 'Sau khi được duyệt, bạn thấy Zalo/SĐT người cùng trận để liên hệ trước giờ đấu.' },
                { sel: '.md-report-btn',   title: 'Khiếu nại',           text: 'Có vấn đề sau trận? Gửi khiếu nại kèm tối đa 3 ảnh — người trong trận sẽ được mời xác minh.' },
                { sel: '#mdMap',           title: 'Bản đồ',              text: 'Vị trí sân trên bản đồ — mở toàn màn hình để xem chỉ đường.' }
            ]
        },
        wallet: {
            match: function (p) { return p === '/wallet' || p === '/wallet/index'; },
            steps: [
                { sel: '#wlBalance',       title: 'Số dư xu',        text: 'Xu dùng để đặt cọc khi tạo trận và thanh toán phí tham gia — không cần chờ admin duyệt.' },
                { sel: '#wlTopUp',         title: 'Nạp xu',          text: 'Nạp qua chuyển khoản QR — tiền vào ví tự động trong vài giây.' },
                { sel: '#promoCodeInput',  title: 'Mã khuyến mãi',   text: 'Nhập mã để nhận xu miễn phí. Nhấn Xem để kiểm tra trước khi áp dụng.' },
                { sel: '#wlVouchers',      title: 'Voucher của tôi', text: 'Voucher được tặng từ sự kiện, sinh nhật... nằm ở đây — nhấn Dùng để cộng xu.' },
                { sel: '#wlHistory',       title: 'Lịch sử giao dịch', text: 'Mọi biến động xu (nạp, trừ phí, hoàn, khuyến mãi) đều ghi lại minh bạch.' }
            ]
        },
        profile: {
            match: function (p) { return p === '/profile' || p === '/profile/index'; },
            steps: [
                { sel: '#pfEditBtn',  title: 'Chỉnh sửa hồ sơ',    text: 'Cập nhật SĐT, Zalo, khu vực và ảnh đại diện tại đây.' },
                { sel: '#pfSkills',   title: 'Môn & Trình độ',     text: 'Trình độ từng môn quyết định bạn được ghép trận nào — điền chính xác để không bị từ chối oan.' },
                { sel: '#pfBadges',   title: 'Huy hiệu',           text: 'Chơi càng nhiều huy hiệu càng nhiều — tăng uy tín khi host duyệt bạn vào trận.' },
                { sel: '#pfReviews',  title: 'Đánh giá',           text: 'Điểm đánh giá từ các trận đã chơi — cả vai trò người chơi lẫn host.' }
            ]
        }
    };

    var idx = 0, overlay = null, tip = null, spot = null, currentKey = null;

    function pathname() {
        return location.pathname.toLowerCase().replace(/\/$/, '') || '/';
    }

    function detectPage() {
        var p = pathname();
        for (var key in pageTours)
            if (pageTours[key].match(p)) return key;
        return null;
    }

    function visible(el) {
        if (!el) return false;
        var r = el.getBoundingClientRect();
        if (r.width <= 0 || r.height <= 0) return false;
        // Loại phần tử bị đẩy ra ngoài khung hình bằng CSS transform/position
        // (vd: drawer bộ lọc mobile đóng translateY(100%) — vẫn có width/height
        // nhưng nằm hẳn ngoài viewport, không nên spotlight vào đó).
        if (r.bottom <= 0 || r.top >= window.innerHeight || r.right <= 0 || r.left >= window.innerWidth) return false;
        return true;
    }

    function stepsFor(key) {
        var defs = key === 'nav' ? navSteps : (pageTours[key] ? pageTours[key].steps : []);
        return defs.filter(function (s) { return visible(document.querySelector(s.sel)); });
    }

    function storageKey(key) {
        return key === 'nav' ? 'spTourDone' : 'spTour:' + key;
    }

    function isDark() {
        return document.documentElement.classList.contains('dark');
    }

    function build() {
        overlay = document.createElement('div');
        overlay.style.cssText = 'position:fixed;inset:0;z-index:10500;';
        spot = document.createElement('div');
        spot.style.cssText = 'position:absolute;border-radius:12px;box-shadow:0 0 0 9999px rgba(0,0,0,.62);transition:all .3s ease;pointer-events:none;';
        tip = document.createElement('div');
        tip.style.cssText = 'position:absolute;max-width:290px;border-radius:12px;padding:14px 16px;box-shadow:0 10px 40px rgba(0,0,0,.3);transition:all .3s ease;'
            + (isDark() ? 'background:#0f172a;border:1px solid #334155;' : 'background:#fff;');
        overlay.appendChild(spot);
        overlay.appendChild(tip);
        document.body.appendChild(overlay);
    }

    function show(list) {
        var step = list[idx];
        var el = document.querySelector(step.sel);
        if (!el) { next(list); return; }

        // Cuộn element vào giữa màn hình trước khi spotlight (trang dài: Wallet/Profile/Details)
        document.body.style.overflow = '';
        el.scrollIntoView({ behavior: 'auto', block: 'center' });
        document.body.style.overflow = 'hidden';

        requestAnimationFrame(function () {
            var r = el.getBoundingClientRect();
            var pad = 6;
            spot.style.left = (r.left - pad) + 'px';
            spot.style.top = (r.top - pad) + 'px';
            spot.style.width = (r.width + pad * 2) + 'px';
            spot.style.height = (r.height + pad * 2) + 'px';

            var titleColor = isDark() ? '#f1f5f9' : '#0f172a';
            var textColor  = isDark() ? '#94a3b8' : '#475569';
            var skipColor  = isDark() ? '#94a3b8' : '#64748b';
            tip.innerHTML =
                '<p style="font-weight:800;font-size:14px;color:' + titleColor + ';margin-bottom:4px;">' + step.title + '</p>' +
                '<p style="font-size:12.5px;color:' + textColor + ';line-height:1.5;">' + step.text + '</p>' +
                '<div style="display:flex;align-items:center;justify-content:space-between;margin-top:12px;">' +
                    '<span style="font-size:11px;color:#94a3b8;font-weight:600;">' + (idx + 1) + '/' + list.length + '</span>' +
                    '<span style="display:flex;gap:8px;">' +
                        '<button id="spTourSkip" style="font-size:12px;color:' + skipColor + ';font-weight:600;padding:5px 10px;">Bỏ qua</button>' +
                        '<button id="spTourNext" style="font-size:12px;color:#fff;font-weight:700;background:#50A5B1;border-radius:8px;padding:5px 14px;">' +
                            (idx === list.length - 1 ? 'Hoàn tất' : 'Tiếp theo →') + '</button>' +
                    '</span>' +
                '</div>';

            var tw = 290, th = 140;
            var left = Math.min(Math.max(r.left, 12), window.innerWidth - tw - 12);
            var top = r.bottom + 14;
            if (top + th > window.innerHeight) top = Math.max(r.top - th - 14, 12);
            tip.style.left = left + 'px';
            tip.style.top = top + 'px';

            document.getElementById('spTourSkip').onclick = end;
            document.getElementById('spTourNext').onclick = function () { next(list); };
        });
    }

    function next(list) {
        idx++;
        if (idx >= list.length) { end(); return; }
        show(list);
    }

    function end() {
        if (currentKey) localStorage.setItem(storageKey(currentKey), '1');
        if (overlay) overlay.remove();
        overlay = null;
        currentKey = null;
        document.body.style.overflow = '';
    }

    function runTour(key) {
        if (overlay) return;
        var list = stepsFor(key);
        if (list.length === 0) return;
        currentKey = key;
        idx = 0;
        build();
        show(list);
    }

    // Chờ popup hoàn thiện hồ sơ đóng rồi mới chạy tour (popup mở sau ~600ms)
    function waitProfileModalThen(fn) {
        var tries = 0;
        var timer = setInterval(function () {
            var pcm = document.getElementById('pcmOverlay');
            var open = pcm && !pcm.classList.contains('hidden');
            tries++;
            if (!open) {
                clearInterval(timer);
                setTimeout(fn, 400);
            } else if (tries > 240) {
                clearInterval(timer); // ~2 phút vẫn mở → thôi, lần sau chạy
            }
        }, 500);
    }

    function autoStart() {
        var pageKey = detectPage();

        setTimeout(function () {
            // Ưu tiên tour navbar cho lần đăng nhập đầu
            if (window.__spFirstLogin && !localStorage.getItem('spTourDone')) {
                waitProfileModalThen(function () { runTour('nav'); });
                return;
            }
            if (pageKey && !localStorage.getItem(storageKey(pageKey))) {
                waitProfileModalThen(function () { runTour(pageKey); });
            }
        }, 1200);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', autoStart);
    } else {
        autoStart();
    }

    return {
        start: runTour,
        // Menu avatar "Xem lại hướng dẫn": chạy lại tour trang hiện tại nếu có, không thì tour navbar
        restart: function () {
            var key = detectPage() || 'nav';
            localStorage.removeItem(storageKey(key));
            runTour(key);
        }
    };
})();
