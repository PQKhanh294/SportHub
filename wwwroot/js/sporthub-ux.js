/**
 * SportHub UX helpers — dùng chung cho _Layout và _AuthLayout.
 * 1. Mobile: cuộn ô nhập đang focus vào giữa màn hình để bàn phím ảo không che
 *    input lẫn dropdown gợi ý ngay dưới nó.
 * 2. Thanh loading trên cùng khi chuyển trang / submit form.
 */
(function () {
    // ---- 1. Keyboard-safe focus scroll (mobile) ----
    document.addEventListener('focusin', function (e) {
        if (window.innerWidth >= 1024) return;
        var el = e.target;
        if (!el.matches || !el.matches('input:not([type=hidden]):not([type=checkbox]):not([type=radio]):not([type=file]), textarea, select')) return;
        // Chờ bàn phím trồi lên xong rồi mới cuộn (animation bàn phím ~250ms)
        setTimeout(function () {
            if (document.activeElement !== el) return;
            try { el.scrollIntoView({ block: 'center', behavior: 'smooth' }); } catch (err) { el.scrollIntoView(); }
        }, 350);
    });

    // ---- 2. Global top loading bar ----
    var bar = document.createElement('div');
    bar.id = 'sp-loadbar';
    bar.style.cssText = 'position:fixed;top:0;left:0;height:3px;width:0;background:linear-gradient(90deg,#50A5B1,#F1600D);z-index:2147483647;transition:width .25s ease,opacity .25s ease;pointer-events:none;opacity:0';
    document.body.appendChild(bar);

    var timer = null;
    function start() {
        if (timer) return;
        var w = 10;
        bar.style.opacity = '1';
        bar.style.width = w + '%';
        timer = setInterval(function () {
            w += (90 - w) * 0.12; // tiệm cận 90%, không bao giờ tự đầy
            bar.style.width = w + '%';
        }, 250);
    }
    function stop() {
        if (timer) { clearInterval(timer); timer = null; }
        bar.style.width = '100%';
        setTimeout(function () { bar.style.opacity = '0'; bar.style.width = '0'; }, 250);
    }

    // Bubble phase — chạy SAU handler riêng của trang, nên bỏ qua được các
    // form/link đã preventDefault (gửi chat qua SignalR, nút mở modal...)
    document.addEventListener('click', function (e) {
        if (e.defaultPrevented || e.ctrlKey || e.metaKey || e.shiftKey || e.button !== 0) return;
        var a = e.target.closest && e.target.closest('a[href]');
        if (!a || a.target === '_blank' || a.hasAttribute('download')) return;
        var href = a.getAttribute('href') || '';
        if (href.charAt(0) === '#' || /^(javascript|mailto|tel):/i.test(href)) return;
        if (a.origin && a.origin !== location.origin) return;
        start();
    });
    document.addEventListener('submit', function (e) {
        if (!e.defaultPrevented) start();
    });
    // Quay lại bằng nút back (bfcache) hoặc điều hướng bị hủy → tắt bar
    window.addEventListener('pageshow', stop);
})();
