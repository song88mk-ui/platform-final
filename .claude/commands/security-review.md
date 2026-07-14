# /security-review

Security review tailored to platform-final's known exposure surface.

## Focus areas (from the validated risk register)
1. **Secrets (TD-01):** no new committed secrets; confirm prod uses SSM; don't print values.
2. **SQL injection (TD-01b/AP-2):** any `FromSqlRaw`/`ExecuteSqlRaw` must be parameterized — audit
   `AgentReports.API/ReportsRepository.cs` and any new raw SQL.
3. **AuthZ gap (R-6):** every controller has a `[ServiceFilter]`; no unprotected action; internal key
   scope. `UseAuthorization()` is inert — don't rely on it.
4. **Info leak (TD-04):** no `ex.Message`/`StackTrace` in responses; no global handler to catch leaks.
5. **Input parsing (AP-7):** `x-og-*` headers parsed without guards → DoS/500; validate new header parsing.
6. **Edge defaults (TD-12):** `AllowAllOrigins` CORS + `ForwardedHeaders` trust-all rely on the ingress
   being the sole entry — confirm.
7. **Partner auth:** QTech `Pass-Key`, RedCat `SharedKey`, OG HMAC-MD5, API keys — verify validation paths.
8. **Dependencies (TD-02):** EOL 3.1 + old packages — note CVE exposure (don't refactor unless asked).

## Output
Findings with file:line, severity, evidence, and suggested investigation (per the no-refactor rule).
Cross-ref [docs/ArchitectureRisks.md](../../docs/ArchitectureRisks.md), [docs/Authentication.md](../../docs/Authentication.md).
