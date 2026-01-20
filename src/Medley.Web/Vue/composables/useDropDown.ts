import { getCurrentInstance } from 'vue';

interface UseDropDownReturn {
  closeAllDropdowns: (excludeButton?: HTMLElement | null) => void;
  handleDropdownClick: (event: Event, articleId?: string) => void;
}

/** Manages Bootstrap dropdown behavior to prevent multiple open dropdowns */
export function useDropDown(): UseDropDownReturn {
  const instance = getCurrentInstance();

  const closeAllDropdowns = (excludeButton: HTMLElement | null = null) => {
    const el = instance?.proxy?.$el;
    const sidebar = el?.closest('.sidebar-content') || el?.closest('.sidebar');

    if (!sidebar) {
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

  const handleDropdownClick = (event: Event, articleId?: string) => {
    const clickedButton = (event.target as HTMLElement).closest('[data-bs-toggle="dropdown"]') as HTMLElement;
    closeAllDropdowns(clickedButton);
  };

  return {
    closeAllDropdowns,
    handleDropdownClick
  };
}
