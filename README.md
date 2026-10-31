# dashboard-data

Last published data of each GitHub Pages dashboard, laid out as on the site
(`<domain>/data/...`, `<domain>/badge.*`). Written by the publish-* workflows
and read by the docs.yml deploy job inside the `pages` concurrency lock, so no
deploy reverts or drops a dashboard's data (issue #1386). Never edit by hand.
