import { Injectable, signal, effect, inject, DOCUMENT } from '@angular/core';

export type Theme = 'light' | 'dark' | 'system';

export interface ThemeState {
    theme: Theme;
    isDark: boolean;
    systemPrefersDark: boolean;
}

@Injectable({
    providedIn: 'root'
})
export class ThemeService {
    private document = inject(DOCUMENT);

    // Private signals
    private _theme = signal<Theme>('system');
    private _systemPrefersDark = signal<boolean>(false);

    // Computed signal for actual dark mode state
    private _isDark = signal<boolean>(false);

    // Public readonly signals
    readonly theme = this._theme.asReadonly();
    readonly isDark = this._isDark.asReadonly();
    readonly systemPrefersDark = this._systemPrefersDark.asReadonly();

    // Media query matcher
    private mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');

    constructor() {
        this.initializeTheme();
        this.setupSystemThemeListener();

        // Effect to update DOM when theme changes
        effect(() => {
            this.updateDOMTheme();
        });
    }

    /**
     * Toggle between light and dark themes
     */
    toggleTheme(): void {
        const currentTheme = this._theme();
        let newTheme: Theme;

        if (currentTheme === 'light') {
            newTheme = 'dark';
        } else if (currentTheme === 'dark') {
            newTheme = 'light';
        } else {
            // If system, toggle to opposite of current system preference
            newTheme = this._systemPrefersDark() ? 'light' : 'dark';
        }

        this.setTheme(newTheme);
    }

    /**
     * Set specific theme
     */
    setTheme(theme: Theme): void {
        this._theme.set(theme);
        this.updateIsDark();
        this.saveThemePreference(theme);
    }

    /**
     * Get current theme state
     */
    getThemeState(): ThemeState {
        return {
            theme: this._theme(),
            isDark: this._isDark(),
            systemPrefersDark: this._systemPrefersDark()
        };
    }

    private initializeTheme(): void {
        // Check system preference first
        this._systemPrefersDark.set(this.mediaQuery.matches);

        // Load saved theme preference
        const savedTheme = this.loadThemePreference();
        this._theme.set(savedTheme);

        // Calculate initial dark mode state
        this.updateIsDark();
    }

    private setupSystemThemeListener(): void {
        // Listen for system theme changes
        this.mediaQuery.addEventListener('change', (e) => {
            this._systemPrefersDark.set(e.matches);
            // If using system theme, update dark mode state
            if (this._theme() === 'system') {
                this.updateIsDark();
            }
        });
    }

    private updateIsDark(): void {
        const theme = this._theme();
        let isDark: boolean;

        switch (theme) {
            case 'light':
                isDark = false;
                break;
            case 'dark':
                isDark = true;
                break;
            case 'system':
                isDark = this._systemPrefersDark();
                break;
        }

        this._isDark.set(isDark);
    }

    private updateDOMTheme(): void {
        const isDark = this._isDark();
        const html = this.document.documentElement;

        if (isDark) {
            html.classList.add('dark');
            html.style.colorScheme = 'dark';
        } else {
            html.classList.remove('dark');
            html.style.colorScheme = 'light';
        }

        // Add theme data attribute for additional styling hooks
        html.setAttribute('data-theme', this._theme());
    }

    private loadThemePreference(): Theme {
        try {
            const saved = localStorage.getItem('theme');
            if (saved && ['light', 'dark', 'system'].includes(saved)) {
                return saved as Theme;
            }
        } catch (e) {
            // localStorage might not be available
            console.warn('Could not load theme preference from localStorage:', e);
        }

        return 'system'; // Default to system preference
    }

    private saveThemePreference(theme: Theme): void {
        try {
            localStorage.setItem('theme', theme);
        } catch (e) {
            // localStorage might not be available
            console.warn('Could not save theme preference to localStorage:', e);
        }
    }
}
