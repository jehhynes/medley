import { ref, onMounted, onUnmounted, nextTick, type Ref } from 'vue';

interface DropdownPosition {
  vertical: 'down' | 'up';
  horizontal: 'left' | 'right';
}

interface UseDropDownReturn {
  openDropdownId: Ref<string | null>;
  dropdownPosition: Ref<DropdownPosition>;
  toggleDropdown: (event: Event, dropdownId: string) => void;
  closeDropdown: () => void;
  isDropdownOpen: (dropdownId: string) => boolean;
  getPositionClasses: () => string;
}

/** Manages dropdown behavior using pure Vue reactivity with smart positioning */
export function useDropDown(): UseDropDownReturn {
  const openDropdownId = ref<string | null>(null);
  const dropdownPosition = ref<DropdownPosition>({ vertical: 'down', horizontal: 'right' });

  const closeDropdown = () => {
    openDropdownId.value = null;
  };

  const calculatePosition = (button: HTMLElement, menu: HTMLElement): DropdownPosition => {
    const buttonRect = button.getBoundingClientRect();
    const menuRect = menu.getBoundingClientRect();
    const viewportHeight = window.innerHeight;
    const viewportWidth = window.innerWidth;

    // Calculate available space
    const spaceBelow = viewportHeight - buttonRect.bottom;
    const spaceAbove = buttonRect.top;
    const spaceRight = viewportWidth - buttonRect.left; // Space from left edge of button to right edge of viewport
    const spaceLeft = buttonRect.right; // Space from left edge of viewport to right edge of button

    // Determine vertical position
    const menuHeight = menuRect.height || 200; // fallback height
    const vertical: 'down' | 'up' = spaceBelow >= menuHeight || spaceBelow >= spaceAbove ? 'down' : 'up';

    // Determine horizontal position
    // Default is right-aligned (menu extends to the left from button's right edge)
    // Switch to left-aligned if there's not enough space on the left side for right-aligned menu
    const menuWidth = menuRect.width || 150; // fallback width
    
    // For right-aligned: need space from button's right edge going left
    const spaceForRightAligned = buttonRect.right;
    // For left-aligned: need space from button's left edge going right
    const spaceForLeftAligned = viewportWidth - buttonRect.left;
    
    // Prefer right-aligned (default), but switch to left if not enough space
    const horizontal: 'left' | 'right' = spaceForRightAligned >= menuWidth ? 'right' : 
                                         (spaceForLeftAligned >= menuWidth ? 'left' : 'right');

    return { vertical, horizontal };
  };

  const toggleDropdown = async (event: Event, dropdownId: string) => {
    event.stopPropagation();
    
    // If clicking the same dropdown, close it
    if (openDropdownId.value === dropdownId) {
      closeDropdown();
      return;
    }

    // Open the new dropdown
    openDropdownId.value = dropdownId;

    // Wait for the dropdown to render, then calculate position
    await nextTick();

    const button = (event.target as HTMLElement).closest('button') as HTMLElement;
    if (!button) return;

    const container = button.closest('.dropdown-container') as HTMLElement;
    if (!container) return;

    const menu = container.querySelector('.dropdown-menu') as HTMLElement;
    if (!menu) return;

    // Calculate and apply position
    dropdownPosition.value = calculatePosition(button, menu);
  };

  const isDropdownOpen = (dropdownId: string): boolean => {
    return openDropdownId.value === dropdownId;
  };

  const getPositionClasses = (): string => {
    const classes: string[] = [];
    
    if (dropdownPosition.value.vertical === 'up') {
      classes.push('dropdown-up');
    }
    
    if (dropdownPosition.value.horizontal === 'left') {
      classes.push('dropdown-left');
    }
    
    return classes.join(' ');
  };

  // Close dropdown when clicking outside
  const handleClickOutside = (event: MouseEvent) => {
    const target = event.target as HTMLElement;
    
    // Check if click is outside any dropdown
    if (!target.closest('.dropdown-container')) {
      closeDropdown();
    }
  };

  // Close dropdown on escape key
  const handleEscapeKey = (event: KeyboardEvent) => {
    if (event.key === 'Escape') {
      closeDropdown();
    }
  };

  onMounted(() => {
    document.addEventListener('click', handleClickOutside);
    document.addEventListener('keydown', handleEscapeKey);
  });

  onUnmounted(() => {
    document.removeEventListener('click', handleClickOutside);
    document.removeEventListener('keydown', handleEscapeKey);
  });

  return {
    openDropdownId,
    dropdownPosition,
    toggleDropdown,
    closeDropdown,
    isDropdownOpen,
    getPositionClasses
  };
}
