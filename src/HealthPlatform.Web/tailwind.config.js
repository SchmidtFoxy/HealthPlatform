/** @type {import('tailwindcss').Config} */
const c = (v) => `rgb(var(--${v}) / <alpha-value>)`;
export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  darkMode: ['class', '[data-theme="dark"]'],
  theme: {
    extend: {
      colors: {
        bg: c('bg'), surface: c('surface'), raised: c('raised'), line: c('line'),
        fg: c('fg'), muted: c('muted'), faint: c('faint'),
        accent: c('accent'), 'accent-ink': c('accent-ink'), deep: c('deep'),
        ok: c('ok'), warn: c('warn'), bad: c('bad'), info: c('info'),
        prot: c('prot'), carb: c('carb'), fat: c('fat'),
      },
      fontFamily: {
        display: ['"Sora Variable"', 'Sora', 'system-ui', 'sans-serif'],
        sans: ['"Figtree Variable"', 'Figtree', 'system-ui', 'sans-serif'],
      },
      borderRadius: { xl2: '1.25rem', xl3: '1.75rem' },
      boxShadow: {
        glow: '0 0 0 1px rgb(var(--accent) / 0.25), 0 10px 40px -10px rgb(var(--accent) / 0.45)',
        card: '0 1px 0 rgb(var(--line) / 0.6) inset, 0 12px 32px -18px rgb(0 0 0 / 0.5)',
      },
      keyframes: {
        shimmer: { '0%': { backgroundPosition: '-200% 0' }, '100%': { backgroundPosition: '200% 0' } },
      },
      animation: { shimmer: 'shimmer 1.6s linear infinite' },
    },
  },
  plugins: [],
};
