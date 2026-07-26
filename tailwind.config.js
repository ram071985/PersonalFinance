/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./**/*.{razor,html,cshtml}",
    "./wwwroot/**/*.{html,js}"
  ],
  theme: {
    extend: {
      colors: {
        bg: "#0d0d0f",
        surface: "#16161a",
        "surface-2": "#1e1e24",
        "surface-3": "#25252c",
        border: "#2a2a32",
        foreground: "#ececef",        // was "text"
        "foreground-muted": "#8b8b96", // was "text-muted"
        primary: "#e11d48",
        "primary-hover": "#be123c",
        "primary-muted": "#3f1219",
        success: "#22c55e",
      }
    },
  },
  plugins: [],
}
