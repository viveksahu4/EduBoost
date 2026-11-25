// EduBoost theme runtime: Light (Off-white) & Dark mode switching,
// collapsible sidebar and Chart.js helpers.
(function () {
    'use strict';

    var root = document.documentElement;
    var THEMES = ['light', 'dark'];
    var THEME_KEY = 'eduboost-theme';
    var SIDEBAR_KEY = 'eduboost-sidebar-collapsed';
    var mobileQuery = window.matchMedia('(max-width: 991.98px)');

    function safeGet(key) {
        try { return window.localStorage.getItem(key); } catch (e) { return null; }
    }

    function safeSet(key, value) {
        try { window.localStorage.setItem(key, value); } catch (e) { /* private mode */ }
    }

    function getTheme() {
        var theme = root.getAttribute('data-theme') || safeGet(THEME_KEY);
        return THEMES.indexOf(theme) !== -1 ? theme : 'light';
    }

    function cssVar(name, fallback) {
        var value = window.getComputedStyle(root).getPropertyValue(name).trim();
        return value || fallback || '';
    }

    function getThemeColors() {
        var isDark = getTheme() === 'dark';
        return {
            accent: cssVar('--accent', isDark ? '#3b82f6' : '#2563eb'),
            accent2: cssVar('--accent-2', '#0ea5e9'),
            text: cssVar('--text', isDark ? '#f8fafc' : '#0f172a'),
            muted: cssVar('--muted', isDark ? '#94a3b8' : '#64748b'),
            success: cssVar('--success', '#10b981'),
            warning: cssVar('--warning', '#f59e0b'),
            danger: cssVar('--danger', '#ef4444'),
            info: cssVar('--info', '#0284c7'),
            grid: cssVar('--chart-grid', isDark ? 'rgba(255,255,255,0.06)' : '#e2e8f0'),
            bg: cssVar('--bg-1', isDark ? '#090d16' : '#f4f6fa')
        };
    }

    function withAlpha(color, alpha) {
        var hex = String(color || '').trim().replace('#', '');
        if (hex.length === 3) {
            hex = hex.split('').map(function (char) { return char + char; }).join('');
        }
        if (/^[0-9a-f]{6}$/i.test(hex)) {
            var r = parseInt(hex.slice(0, 2), 16);
            var g = parseInt(hex.slice(2, 4), 16);
            var b = parseInt(hex.slice(4, 6), 16);
            return 'rgba(' + r + ', ' + g + ', ' + b + ', ' + alpha + ')';
        }
        return color;
    }

    function syncThemeButton() {
        var current = getTheme();
        var label = document.getElementById('themeToggleLabel');
        var btn = document.getElementById('themeToggleBtn');
        if (label) {
            label.textContent = current === 'dark' ? 'Dark' : 'Light';
        }
        if (btn) {
            btn.setAttribute('aria-label', current === 'dark' ? 'Switch to Light mode' : 'Switch to Dark mode');
            btn.setAttribute('title', current === 'dark' ? 'Switch to Light (Off-white) mode' : 'Switch to Dark mode');
        }
    }

    function applyTheme(theme, persist) {
        var next = THEMES.indexOf(theme) !== -1 ? theme : 'light';
        root.setAttribute('data-theme', next);
        root.setAttribute('data-bs-theme', next);

        if (persist !== false) {
            safeSet(THEME_KEY, next);
        }
        syncThemeButton();

        var meta = document.querySelector('meta[name="theme-color"]');
        if (meta) {
            meta.setAttribute('content', next === 'dark' ? '#090d16' : '#f4f6fa');
        }

        window.dispatchEvent(new CustomEvent('eduboost:themechange', {
            detail: { theme: next, colors: getThemeColors() }
        }));
    }

    function toggleTheme() {
        var current = getTheme();
        var next = current === 'dark' ? 'light' : 'dark';
        applyTheme(next, true);
    }

    function isMobile() {
        return mobileQuery.matches;
    }

    function syncSidebarToggle() {
        var toggle = document.getElementById('sidebarToggle');
        if (!toggle) return;
        var expanded = isMobile()
            ? document.body.classList.contains('sidebar-open')
            : !document.body.classList.contains('sidebar-collapsed');
        toggle.setAttribute('aria-expanded', expanded ? 'true' : 'false');
    }

    function toggleSidebar() {
        if (isMobile()) {
            document.body.classList.toggle('sidebar-open');
            document.body.classList.remove('sidebar-collapsed');
        } else {
            var collapsed = document.body.classList.toggle('sidebar-collapsed');
            safeSet(SIDEBAR_KEY, collapsed ? '1' : '0');
        }
        syncSidebarToggle();
    }

    function closeSidebar() {
        document.body.classList.remove('sidebar-open');
        syncSidebarToggle();
    }

    document.addEventListener('DOMContentLoaded', function () {
        applyTheme(getTheme(), false);
        syncSidebarToggle();

        var themeToggleBtn = document.getElementById('themeToggleBtn');
        if (themeToggleBtn) {
            themeToggleBtn.addEventListener('click', toggleTheme);
        }

        var toggle = document.getElementById('sidebarToggle');
        if (toggle) {
            toggle.addEventListener('click', toggleSidebar);
        }

        var backdrop = document.getElementById('sidebarBackdrop');
        if (backdrop) {
            backdrop.addEventListener('click', closeSidebar);
        }

        document.addEventListener('keydown', function (event) {
            if (event.key === 'Escape') {
                closeSidebar();
            }
        });

        document.querySelectorAll('.sidebar a').forEach(function (link) {
            link.addEventListener('click', function () {
                if (isMobile()) closeSidebar();
            });
        });

        var onViewportChange = function () {
            document.body.classList.remove('sidebar-open');
            syncSidebarToggle();
        };

        if (typeof mobileQuery.addEventListener === 'function') {
            mobileQuery.addEventListener('change', onViewportChange);
        } else if (typeof mobileQuery.addListener === 'function') {
            mobileQuery.addListener(onViewportChange);
        }
    });

    window.EduBoost = {
        themes: THEMES,
        getTheme: getTheme,
        setTheme: applyTheme,
        toggleTheme: toggleTheme,
        getThemeColors: getThemeColors,
        withAlpha: withAlpha,
        safeGet: safeGet,
        safeSet: safeSet
    };
})();
