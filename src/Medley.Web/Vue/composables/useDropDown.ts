import { getCurrentInstance } from 'vue';

/**
 * Return type for useDropDown composable
 */
interface UseDropDownReturn {
  closeAllDropdowns: (excludeButton?: HTMLElement | null) => void;
  handleDropdownClick: (event: Event, articleId?: string) => void;
}

/**
 * Composable for managing Bootstrap dropdown behavior.
 * Handles closing other dropdowns when one is opened to prevent multiple open dropdowns.
 * 
 * @returns Dropdown management methods
 */
export function useDropDown(): UseDropDownReturn {
  const instance = getCurrentInstance();

  /**
   * Close all open dropdowns globally, excluding a specific button.
   * Finds the root sidebar container and closes all dropdowns within it.
   * This ensures dropdowns are closed across all component instances (including recursive ones).
   * 
   * @param excludeButton - Dropdown button to exclude from closing
   */
  const closeAllDropdowns = (excludeButton: HTMLElement | null = null): void => {
    // Find the root sidebar container (works for both tree and list views)
    const el = instance?.proxy?.$el;
    const sidebar = el?.closest('.sidebar-content') || el?.closest('.sidebar');

    if (!sidebar) {
      // Fallback: search from document root
      const sidebarContent = document.querySelector('.sidebar-content');
      if (sidebarContent) {
        const dropdownButtons = sidebarContent.querySelectorAll('[data-bs-toggle="dropdown"]');
        dropdownButtons.forEach(button => {
          if (button !== excludeButton) {
            const bootstrap = (window as any).bootstrap;
            if (bootstrap?.Dropdown) {
              const dropdown = bootstrap.Dropdown.getInstance(button);
              if (dropdown && dropdown._isShown()) {
                dropdown.hide();
              }
            }
          }
        });
      }
      return;
    }

    // Find all dropdown buttons in the sidebar
    const dropdownButtons = sidebar.querySelectorAll('[data-bs-toggle="dropdown"]');
    dropdownButtons.forEach(button => {
      if (button !== excludeButton) {
        const bootstrap = (window as any).bootstrap;
        if (bootstrap?.Dropdown) {
          const dropdown = bootstrap.Dropdown.getInstance(button);
          if (dropdown && dropdown._isShown()) {
            dropdown.hide();
          }
        }
      }
    });
  };

  /**
   * Handle dropdown button click.
   * Closes all other dropdowns before allowing Bootstrap to toggle the clicked one.
   * 
   * @param event - Click event
   * @param articleId - Article ID for the dropdown (optional, for logging/debugging)
   */
  const handleDropdownClick = (event: Event, articleId?: string): void => {
    // Close all other dropdowns first (excluding the clicked one)
    const clickedButton = (event.target as HTMLElement).closest('[data-bs-toggle="dropdown"]') as HTMLElement;
    closeAllDropdowns(clickedButton);
    // Let Bootstrap handle the toggle for this dropdown
    // The event will bubble and Bootstrap will handle it via data-bs-toggle
  };

  return {
    closeAllDropdowns,
    handleDropdownClick
  };
}
