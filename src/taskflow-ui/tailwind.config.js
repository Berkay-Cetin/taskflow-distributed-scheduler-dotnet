/** @type {import('tailwindcss').Config} */
export default {
  content: ["./index.html", "./src/**/*.{js,ts,jsx,tsx}"],
  theme: {
    extend: {
      colors: {
        dark: {
          900: "#0a0a0f",
          800: "#12121a",
          700: "#1a1a2e",
          600: "#16213e",
        },
        accent: {
          500: "#0f3460",
          400: "#533483",
          300: "#e94560",
        }
      }
    },
  },
  plugins: [],
}