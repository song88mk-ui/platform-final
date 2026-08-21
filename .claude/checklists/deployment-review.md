# Checklist: Deployment Review

- [ ] **CI image exists** for the project in [.github/workflows/ci.yml](../../.github/workflows/ci.yml)
      (note: several integrations are absent from CI — TD-09; confirm their deploy path).
- [ ] **Dockerfile** present; multi-stage build SDK 6.0 → runtime aspnet:3.1; `EXPOSE 80`;
      private NuGet feed (`NUGET_PASSWORD`); DocumentDB CA bundle if it reads Mongo.
- [ ] **Config externalized:** all secrets/connection strings come from **SSM via External Secrets**
      (`Section__Key` env vars); committed `appsettings.json` is dev/staging fallback only (TD-01).
- [ ] **K8s wiring (argocd):** service on `:3000` (container `:80`); points at
      `rabbitmq-main-cluster.bet-genius-apps.svc`; correct env (dev/stg/prd account/region).
- [ ] **Live variant:** deploying the current project, not a superseded duplicate (TD-07).
- [ ] **Cross-repo coordination:** if contracts/schema changed, sibling deploys ordered correctly (R-2/R-12).
- [ ] **Migrations:** schema is DB-first — any DB change applied out-of-band (no EF migrations).
