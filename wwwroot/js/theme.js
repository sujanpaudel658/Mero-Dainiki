/**
 * Mero-Dainiki Theme Manager
 * Handles toggling between light and dark modes via data-theme attribute and CSS classes.
 */
window.themeManager = {
    setTheme: function (theme) {
        const root = document.documentElement;
        let resolvedTheme = theme;

        // If system mode is chosen, we need to determine the actual visual theme
        if (theme === 'system') {
            resolvedTheme = window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
        }

        // Set attributes for CSS variable scoping
        root.setAttribute('data-theme', resolvedTheme);
        root.setAttribute('data-mode', theme); // Store the original selection
        root.setAttribute('data-bs-theme', resolvedTheme);

        // Update classes
        root.classList.remove('light', 'dark');
        root.classList.add(resolvedTheme);

        localStorage.setItem('theme', theme);
        console.log(`Theme selection: ${theme}, Resolved to: ${resolvedTheme}`);
    },

    getTheme: function () {
        return localStorage.getItem('theme') || 'system';
    },

    initTheme: function () {
        this.setTheme(this.getTheme());
    }
};

// Initialize immediately
window.themeManager.initTheme();

// Listen for system changes and update if 'system' is currently selected
window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => {
    if (window.themeManager.getTheme() === 'system') {
        window.themeManager.setTheme('system');
    }
});
