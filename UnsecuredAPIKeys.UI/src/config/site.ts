export type SiteConfig = typeof siteConfig;

export const siteConfig = {
  name: "Unsecured API Keys",
  description: "Help developers avoid that awkward conversation with their boss. Find and report exposed API keys in public GitHub repositories. Secure the web, one leaked key at a time.",
  navItems: [
    {
      label: "Home",
      href: "/",
    },
    {
      label: "🏆 Leaderboard",
      href: "/leaderboard",
    },
    {
      label: "About",
      href: "/about",
    },
  ],
  navMenuItems: [
    {
      label: "Home",
      href: "/",
    },
    {
      label: "🏆 Leaderboard",
      href: "/leaderboard",
    },

    {
      label: "About",
      href: "/about",
    },
  ],
  links: {},
};
