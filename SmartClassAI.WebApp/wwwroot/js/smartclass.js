(function () {
    'use strict';

    const THEME_KEY = 'smartclass-theme';

    function initTheme() {
        const saved = localStorage.getItem(THEME_KEY);
        const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
        const theme = saved || (prefersDark ? 'dark' : 'light');
        document.documentElement.setAttribute('data-theme', theme);
        updateThemeIcon(theme);
    }

    function toggleTheme() {
        const current = document.documentElement.getAttribute('data-theme') || 'light';
        const next = current === 'dark' ? 'light' : 'dark';
        document.documentElement.setAttribute('data-theme', next);
        localStorage.setItem(THEME_KEY, next);
        updateThemeIcon(next);
    }

    function updateThemeIcon(theme) {
        const btn = document.getElementById('themeToggle');
        if (!btn) return;
        const icon = btn.querySelector('i');
        if (icon) {
            icon.className = theme === 'dark' ? 'fa-solid fa-sun' : 'fa-solid fa-moon';
        }
    }

    function initSidebar() {
        const sidebar = document.getElementById('appSidebar');
        const toggle = document.getElementById('sidebarToggle');
        const overlay = document.getElementById('sidebarOverlay');
        if (!sidebar || !toggle) return;

        function open() {
            sidebar.classList.add('is-open');
            overlay?.classList.add('show');
        }

        function close() {
            sidebar.classList.remove('is-open');
            overlay?.classList.remove('show');
        }

        toggle.addEventListener('click', () => {
            sidebar.classList.contains('is-open') ? close() : open();
        });

        overlay?.addEventListener('click', close);

        document.querySelectorAll('.sidebar-nav a').forEach(link => {
            const href = link.getAttribute('href');
            if (href && window.location.pathname.toLowerCase().includes(href.split('?')[0].toLowerCase().replace(/^\/+/, ''))) {
                if (href.length > 1) link.classList.add('active');
            }
        });

        const path = window.location.pathname.toLowerCase();
        document.querySelectorAll('.sidebar-nav a[data-nav]').forEach(link => {
            const key = link.getAttribute('data-nav');
            if (key && path.includes(key)) link.classList.add('active');
        });
    }

    window.copyJoinCode = function (code) {
        navigator.clipboard.writeText(code).then(() => {
            if (window.SmartNotify) SmartNotify.toastSuccess('Join code copied!');
            else alert('Join code copied');
        });
    };

    document.addEventListener('DOMContentLoaded', () => {
        initTheme();
        initSidebar();

        document.getElementById('themeToggle')?.addEventListener('click', toggleTheme);
    });
})();
