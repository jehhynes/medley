(function() {
    'use strict';
    
    // Mobile sidebar toggles - independent of Vue
    const leftToggle = document.getElementById('mobile-left-sidebar-toggle');
    const rightToggle = document.getElementById('mobile-right-sidebar-toggle');
    const backdrop = document.getElementById('mobile-sidebar-backdrop');
    
    function isMediumBreakpoint() {
        return window.innerWidth >= 993 && window.innerWidth <= 1199;
    }

    function isMobileBreakpoint() {
        return window.innerWidth <= 992;
    }

    function updateBackdrop() {
        const verticalMenu = document.querySelector('.vertical-menu');
        const leftSidebar = document.querySelector('.left-sidebar');
        const rightSidebar = document.querySelector('.right-sidebar');
        
        const isLeftOpen = verticalMenu && verticalMenu.classList.contains('show');
        const isRightOpen = rightSidebar && rightSidebar.classList.contains('show');
        
        if (backdrop) {
            // Remove show-initial if present (set by server on page load)
            backdrop.classList.remove('show-initial');
            
            // Show backdrop if either sidebar is open
            if (isLeftOpen || isRightOpen) {
                backdrop.classList.add('show');
            } else {
                backdrop.classList.remove('show');
            }
        }
    }
    
    function closeAllSidebars() {
        const verticalMenu = document.querySelector('.vertical-menu');
        const leftSidebar = document.querySelector('.left-sidebar');
        const rightSidebar = document.querySelector('.right-sidebar');
        
        if (verticalMenu) verticalMenu.classList.remove('show');
        if (leftSidebar) leftSidebar.classList.remove('show');
        if (rightSidebar) rightSidebar.classList.remove('show');
        
        updateBackdrop();
    }
    
    if (leftToggle) {
        leftToggle.addEventListener('click', function(e) {
            e.stopPropagation();
            
            // Only handle on mobile (< 993px)
            if (!isMobileBreakpoint()) return;
            
            const verticalMenu = document.querySelector('.vertical-menu');
            const leftSidebar = document.querySelector('.left-sidebar');
            
            if (verticalMenu) verticalMenu.classList.toggle('show');
            if (leftSidebar) leftSidebar.classList.toggle('show');
            
            // Close right sidebar if open
            const rightSidebar = document.querySelector('.right-sidebar');
            if (rightSidebar) rightSidebar.classList.remove('show');
            
            updateBackdrop();
        });
    }
    
    if (rightToggle) {
        rightToggle.addEventListener('click', function(e) {
            e.stopPropagation();
            const rightSidebar = document.querySelector('.right-sidebar');
            if (rightSidebar) rightSidebar.classList.toggle('show');
            
            // Only close left sidebars on mobile (< 993px)
            if (isMobileBreakpoint()) {
                const verticalMenu = document.querySelector('.vertical-menu');
                const leftSidebar = document.querySelector('.left-sidebar');
                if (verticalMenu) verticalMenu.classList.remove('show');
                if (leftSidebar) leftSidebar.classList.remove('show');
            }
            
            updateBackdrop();
        });
    }
    
    // Close sidebars when clicking backdrop
    if (backdrop) {
        backdrop.addEventListener('click', function(e) {
            e.stopPropagation();
            closeAllSidebars();
        });
    }
    
    // Close sidebars when clicking main content
    document.addEventListener('click', function(e) {
        const verticalMenu = document.querySelector('.vertical-menu');
        const leftSidebar = document.querySelector('.left-sidebar');
        const rightSidebar = document.querySelector('.right-sidebar');
        const header = document.querySelector('.mobile-header');
        
        // Don't close if clicking on header, sidebars, or backdrop
        if (header && header.contains(e.target)) return;
        if (verticalMenu && verticalMenu.contains(e.target)) return;
        if (leftSidebar && leftSidebar.contains(e.target)) return;
        if (rightSidebar && rightSidebar.contains(e.target)) return;
        if (backdrop && backdrop.contains(e.target)) return;
        
        // On mobile (< 993px): close all sidebars
        if (isMobileBreakpoint()) {
            if ((verticalMenu && verticalMenu.classList.contains('show')) ||
                (rightSidebar && rightSidebar.classList.contains('show'))) {
                closeAllSidebars();
            }
        }
        // On medium breakpoint (993-1199px): only close right sidebar
        else if (isMediumBreakpoint()) {
            if (rightSidebar && rightSidebar.classList.contains('show')) {
                rightSidebar.classList.remove('show');
                updateBackdrop();
            }
        }
    });
    
    // Handle window resize
    window.addEventListener('resize', function() {
        // On xl breakpoint and above (>= 1200px): close all sidebars
        if (window.innerWidth >= 1200) {
            closeAllSidebars();
        }
        // On mobile to medium transition: close left sidebars only
        else if (isMediumBreakpoint()) {
            const verticalMenu = document.querySelector('.vertical-menu');
            const leftSidebar = document.querySelector('.left-sidebar');
            if (verticalMenu) verticalMenu.classList.remove('show');
            if (leftSidebar) leftSidebar.classList.remove('show');
            updateBackdrop();
        }
    });
    
    // Expose public API for Vue components to collapse sidebars on record selection
    window.MedleySidebar = {
        /**
         * Collapse left sidebar on mobile devices
         * Called by Vue components when a record is selected from the left sidebar
         */
        collapseLeftSidebar: function() {
            // Only collapse on mobile breakpoint
            if (!isMobileBreakpoint()) return;
            
            const verticalMenu = document.querySelector('.vertical-menu');
            const leftSidebar = document.querySelector('.left-sidebar');
            
            if (verticalMenu) verticalMenu.classList.remove('show');
            if (leftSidebar) leftSidebar.classList.remove('show');
            
            updateBackdrop();
        },
        
        /**
         * Collapse right sidebar on mobile devices
         * Called by Vue components when a record is selected from the right sidebar
         */
        collapseRightSidebar: function() {
            // Only collapse on mobile breakpoint
            if (!isMobileBreakpoint()) return;
            
            const rightSidebar = document.querySelector('.right-sidebar');
            
            if (rightSidebar) rightSidebar.classList.remove('show');
            
            updateBackdrop();
        }
    };
})();
