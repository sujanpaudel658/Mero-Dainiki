/**
 * Mero-Dainiki Theme Manager
 * Handles toggling between light and dark modes via data-theme attribute and CSS classes.
 */
window.themeManager = {
    /**
     * Sets the theme on the document root and persists it.
     * @param {string} theme - 'light' or 'dark'
     */
    setTheme: function (theme) {
        // Set data attribute for global styles
        document.documentElement.setAttribute('data-theme', theme);
        document.documentElement.setAttribute('data-bs-theme', theme); // Bootstrap support

        // Toggle CSS classes for scoped styles (html.dark / html.light)
        document.documentElement.classList.remove('light', 'dark');
        document.documentElement.classList.add(theme);

        localStorage.setItem('theme', theme);
        console.log(`Theme set to: ${theme}`);
    },

    /**
     * Gets the current theme from local storage or system preference.
     * @returns {string} 'light' or 'dark'
     */
    getTheme: function () {
        const storedTheme = localStorage.getItem('theme');
        if (storedTheme) {
            return storedTheme;
        }
        // Fallback to system preference
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            return 'dark';
        }
        return 'light';
    },

    /**
     * Initializes the theme on load.
     */
    initTheme: function () {
        const theme = this.getTheme();
        this.setTheme(theme);
    }
};

// Initialize immediately to prevent flash
window.themeManager.initTheme();

// Listen for system changes
window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', e => {
    if (!localStorage.getItem('theme')) {
        // Only react to system change if user hasn't manually set a preference
        window.themeManager.setTheme(e.matches ? 'dark' : 'light');
    }
});
