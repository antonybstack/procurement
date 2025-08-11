/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/**/*.{html,ts}",
  ],
  darkMode: 'class', // Enable class-based dark mode
  theme: {
    extend: {
      colors: {
        // Grok-inspired color palette using #161618 as base
        primary: {
          50: '#f8fafc',
          100: '#f1f5f9',
          200: '#e2e8f0',
          300: '#cbd5e1',
          400: '#94a3b8',
          500: '#64748b',
          600: '#475569',
          700: '#334155',
          800: '#1e293b',
          900: '#0f172a',
          950: '#020617'
        },
        // Grok dark theme colors
        grok: {
          bg: '#161618',        // Main dark background
          'bg-light': '#1f1f21', // Slightly lighter background
          'bg-lighter': '#2a2a2c', // Card/modal backgrounds
          surface: '#202022',    // Surface elements
          border: '#343437',     // Border color
          'border-light': '#404045', // Lighter borders
          text: '#ffffff',       // Primary text
          'text-dim': '#d1d5db', // Secondary text
          'text-muted': '#9ca3af', // Muted text
          accent: '#3b82f6',     // Blue accent
          'accent-hover': '#2563eb' // Blue accent hover
        },
        // Light theme colors
        light: {
          bg: '#ffffff',         // Main light background
          'bg-alt': '#f9fafb',   // Alternative background
          'bg-surface': '#f3f4f6', // Surface elements
          surface: '#ffffff',    // Card/modal backgrounds
          border: '#e5e7eb',     // Border color
          'border-light': '#f3f4f6', // Lighter borders
          text: '#111827',       // Primary text
          'text-dim': '#374151', // Secondary text
          'text-muted': '#6b7280', // Muted text
          accent: '#3b82f6',     // Blue accent
          'accent-hover': '#2563eb' // Blue accent hover
        },
        // Enhanced gray scale
        gray: {
          50: '#f9fafb',
          100: '#f3f4f6',
          200: '#e5e7eb',
          300: '#d1d5db',
          400: '#9ca3af',
          500: '#6b7280',
          600: '#4b5563',
          700: '#374151',
          800: '#1f2937',
          850: '#1a202c',
          875: '#161618',  // Grok base color
          900: '#111827',
          925: '#0d1117',
          950: '#0a0f1a'
        }
      },
      fontFamily: {
        sans: [
          '-apple-system',
          'BlinkMacSystemFont',
          'Segoe UI',
          'Roboto',
          'Helvetica Neue',
          'Arial',
          'sans-serif'
        ],
      },
      borderRadius: {
        'xl': '0.75rem',
        '2xl': '1rem',
        '3xl': '1.5rem',
      },
      backdropBlur: {
        xs: '2px',
      },
      animation: {
        'fade-in': 'fadeIn 0.2s ease-out',
        'slide-up': 'slideUp 0.2s ease-out',
        'scale-in': 'scaleIn 0.2s ease-out',
      },
      keyframes: {
        fadeIn: {
          '0%': { opacity: '0' },
          '100%': { opacity: '1' },
        },
        slideUp: {
          '0%': { transform: 'translateY(10px)', opacity: '0' },
          '100%': { transform: 'translateY(0)', opacity: '1' },
        },
        scaleIn: {
          '0%': { transform: 'scale(0.95)', opacity: '0' },
          '100%': { transform: 'scale(1)', opacity: '1' },
        },
      }
    },
  },
  plugins: [],
}
