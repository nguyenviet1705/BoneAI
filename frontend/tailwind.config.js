/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        brand: {
          dark: "#0b152d",
          sidebar: "#0d1b3e",
          sidebarHover: "#162858",
          sidebarActive: "#1e3a79",
          card: "#ffffff",
          bg: "#f0f4f9",
          primary: "#2563eb",
          primaryHover: "#1d4ed8",
          primaryLight: "#eff6ff",
          border: "#e2e8f0",
          textPrimary: "#0f172a",
          textSecondary: "#64748b",
          accentGreen: "#10b981",
          accentRed: "#ef4444",
          accentYellow: "#f59e0b"
        }
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', '-apple-system', 'sans-serif'],
      },
      boxShadow: {
        'card': '0 2px 10px rgba(0, 0, 0, 0.03), 0 1px 3px rgba(0, 0, 0, 0.05)',
        'card-hover': '0 10px 25px -5px rgba(0, 0, 0, 0.06), 0 8px 10px -6px rgba(0, 0, 0, 0.03)',
      }
    },
  },
  plugins: [],
}
