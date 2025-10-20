/*!
* Color mode toggler for Medley Application
* Adapted from CoreUI's color-modes.js to work with CoreUI Icons font
*/

(() => {
    const THEME = 'medley-theme';
    const getStoredTheme = () => localStorage.getItem(THEME);
    const setStoredTheme = theme => localStorage.setItem(THEME, theme);
    const getPreferredTheme = () => {
        const storedTheme = getStoredTheme();
        if (storedTheme) {
            return storedTheme;
        }
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    };
    const setTheme = theme => {
        if (theme === 'auto' && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            document.documentElement.setAttribute('data-bs-theme', 'dark');
        } else {
            document.documentElement.setAttribute('data-bs-theme', theme);
        }
        const event = new Event('ColorSchemeChange');
        document.documentElement.dispatchEvent(event);
    };
    setTheme(getPreferredTheme());

    const showActiveTheme = theme => {
        const activeThemeIcon = document.querySelector('.theme-icon-active');
        const btnToActive = document.querySelector(`[data-bs-theme-value="${theme}"]`);
        const iconOfActiveBtn = btnToActive.querySelector('i');

        // Remove active class from all theme buttons
        for (const element of document.querySelectorAll('[data-bs-theme-value]')) {
            element.classList.remove('active');
        }

        // Add active class to the selected button
        btnToActive.classList.add('active');

        // Update the active theme icon
        if (activeThemeIcon && iconOfActiveBtn) {
            // Remove all existing icon classes
            activeThemeIcon.className = activeThemeIcon.className.replace(/cil-\w+/g, '');
            // Add the new icon class
            const newIconClass = iconOfActiveBtn.className.match(/cil-\w+/);
            if (newIconClass) {
                activeThemeIcon.classList.add(newIconClass[0]);
            }
        }
    };

    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
        const storedTheme = getStoredTheme();
        if (storedTheme === 'auto') {
            setTheme(getPreferredTheme());
            showActiveTheme('auto');
        }
    });

    window.addEventListener('DOMContentLoaded', () => {
        showActiveTheme(getPreferredTheme());
        for (const toggle of document.querySelectorAll('[data-bs-theme-value]')) {
            toggle.addEventListener('click', () => {
                const theme = toggle.getAttribute('data-bs-theme-value');
                setStoredTheme(theme);
                setTheme(theme);
                showActiveTheme(theme);
            });
        }
    });
})();

/*!
* Active page highlighting for navigation
*/
(() => {
    function highlightActivePage() {
        const currentPath = window.location.pathname.toLowerCase();
        const navLinks = document.querySelectorAll('.sidebar-nav .nav-link, .header-nav .nav-link');
        
        navLinks.forEach(link => {
            const href = link.getAttribute('href');
            if (href && href !== '#') {
                const linkPath = href.toLowerCase();
                
                // Remove active class from all links
                link.classList.remove('active');
                
                // Add active class if current path matches
                if (currentPath === linkPath || 
                    (currentPath === '/' && linkPath.includes('home')) ||
                    (currentPath.includes('home') && linkPath.includes('home'))) {
                    link.classList.add('active');
                }
            }
        });
    }

    // Highlight active page on load
    window.addEventListener('DOMContentLoaded', highlightActivePage);
    
    // Re-highlight when navigating (for SPA-like behavior)
    window.addEventListener('popstate', highlightActivePage);
})();