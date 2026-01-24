// Theme Manager
window.themeManager = {
    setTheme: function(theme) {
        const html = document.documentElement;
        const body = document.body;
        
        // Remove both classes first
        html.classList.remove('light', 'dark');
        body.classList.remove('light', 'dark');
        
        // Add the new theme class
        html.classList.add(theme);
        body.classList.add(theme);
        
        // Also set data attribute for CSS selectors
        html.setAttribute('data-theme', theme);
        body.setAttribute('data-theme', theme);
        
        // Store preference
        localStorage.setItem('theme', theme);
        
        console.log('Theme set to:', theme);
        
        // Force repaint
        document.body.style.display = 'none';
        document.body.offsetHeight; // Trigger reflow
        document.body.style.display = '';
    },
    
    getTheme: function() {
        return localStorage.getItem('theme') || 'light';
    },
    
    toggleTheme: function() {
        const currentTheme = this.getTheme();
        const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
        this.setTheme(newTheme);
        return newTheme;
    }
};

// Initialize theme on page load
document.addEventListener('DOMContentLoaded', function() {
    const savedTheme = localStorage.getItem('theme') || 'light';
    window.themeManager.setTheme(savedTheme);
});

// Also initialize immediately in case DOMContentLoaded already fired
(function() {
    const savedTheme = localStorage.getItem('theme') || 'light';
    if (document.documentElement) {
        document.documentElement.classList.add(savedTheme);
        document.documentElement.setAttribute('data-theme', savedTheme);
    }
})();
