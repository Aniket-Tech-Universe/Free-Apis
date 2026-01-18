import path from "node:path";
import { fileURLToPath } from "node:url";
import js from "@eslint/js";
import { FlatCompat } from "@eslint/eslintrc";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const compat = new FlatCompat({
  baseDirectory: __dirname,
});

const config = [
  js.configs.recommended,
  ...compat.extends("next/core-web-vitals"),
  {
    ignores: [
      ".next/**",
      ".vite/**",
      "out/**",
      "build/**",
      "dist/**",
      "coverage/**",
      "node_modules/**",
      "public/**",
      "scripts/**",
      "next-env.d.ts",
      "**/*.d.ts",
      "next.config.js",
      "tailwind.config.js",
      "postcss.config.js",
    ],
  },
  {
    rules: {
      "no-unused-vars": ["error", { "argsIgnorePattern": "^_", "varsIgnorePattern": "^_" }],
    },
  },
];

export default config;
