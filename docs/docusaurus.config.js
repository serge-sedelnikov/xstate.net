module.exports = {
  title: 'XStateNet',
  tagline: '.NET Finite State Machine',
  url: 'https://xstatenetdocs.z6.web.core.windows.net/',
  baseUrl: '/',
  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',
  favicon: 'img/favicon.ico',
  organizationName: 'serge-sedelnikov', // Usually your GitHub org/user name.
  projectName: 'XStateNet', // Usually your repo name.
  scripts: [
    {
      src: 'https://pagead2.googlesyndication.com/pagead/js/adsbygoogle.js?client=ca-pub-6340477093933407',
      async: true,
      crossorigin: 'anonymous'
    }
  ],
  themeConfig: {
    prism: {
      additionalLanguages: ['csharp'],
    },
    navbar: {
      title: 'XStateNet',
      logo: {
        alt: 'XStateNet',
        src: 'img/logo.png',
      },
      items: [
        // {
        //   to: 'docs/',
        //   activeBasePath: 'docs',
        //   label: 'Docs',
        //   position: 'left',
        // },
        // {to: 'blog', label: 'Blog', position: 'left'},
        {
          href: 'https://github.com/serge-sedelnikov/xstate.net',
          label: 'GitHub',
          position: 'right',
        },
        {
          href: 'https://www.nuget.org/packages/XStateNet',
          label: 'Nuget',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [],
      copyright: `Copyright © ${new Date().getFullYear()} Sergey Sedelnikov. Built with Docusaurus.`,
    },
  },
  presets: [
    [
      '@docusaurus/preset-classic',
      {
        // editable: false,
        docs: {
          routeBasePath: '/',
          sidebarPath: require.resolve('./sidebars.js'),
          // Please change this to your repo.
          // editUrl:
          //   'https://github.com/serge-sedelnikov/xstate.net/wiki',
        },
        theme: {
          customCss: [require.resolve('./src/css/custom.css')],
        },
      },
    ],
  ],
};
