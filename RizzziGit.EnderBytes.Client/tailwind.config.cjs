const path = require('path');
const tailwindcss = require('tailwindcss');

/** @type {import('tailwindcss').Config}*/
const config = {
  content: ["./src/**/*.{html,js,svelte,ts}"],

  theme: {
    extend: {}
  },

  plugins: [tailwindcss(path.resolve(__dirname, './tailwind.config.cjs'))]
};

module.exports = config;
