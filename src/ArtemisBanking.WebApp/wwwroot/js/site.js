(() => {
    const storageKey = 'artemis-theme';
    const root = document.documentElement;
    const savedTheme = localStorage.getItem(storageKey);

    const applyTheme = (theme) => {
        const isDark = theme === 'dark';
        root.dataset.theme = isDark ? 'dark' : 'light';

        document.querySelectorAll('[data-theme-toggle]').forEach((button) => {
            button.setAttribute('aria-pressed', String(isDark));
            button.setAttribute('aria-label', isDark ? 'Cambiar a modo claro' : 'Cambiar a modo oscuro');
            button.querySelector('[data-theme-icon]').className = isDark
                ? 'fas fa-sun'
                : 'fas fa-moon';
            button.querySelector('[data-theme-label]').textContent = isDark
                ? 'Modo claro'
                : 'Modo oscuro';
        });
    };

    applyTheme(savedTheme === 'dark' ? 'dark' : 'light');

    document.addEventListener('click', (event) => {
        const toggle = event.target.closest('[data-theme-toggle]');
        if (!toggle) {
            return;
        }

        const nextTheme = root.dataset.theme === 'dark' ? 'light' : 'dark';
        localStorage.setItem(storageKey, nextTheme);
        applyTheme(nextTheme);
    });
})();
