---
name: mongodb-investigation
description: Investigate the read-only MongoDB Sportsbook catalogue (collections, projections, read preferences) that odds-engine-2 writes. Use when catalogue, fixture, market or selection data looks wrong or a Mongo read needs tracing.
---

# Skill: MongoDB Investigation

Investigate the sportsbook catalogue read model.

## Knowledge
- DB `Sportsbook` (AWS DocumentDB, unverified: connection strings come from SSM), key `ConnectionStrings:MongoDB-Sportsbook`. Collections:
  `Fixture`, `Market`, `MarketMapping`, `Selection`, `BettingStatus`, `Maintenance`.
- **platform-final reads only** (0 write ops). odds-engine-2 owns writes. Abbreviated field names.
- Reads via `.Find(filter).Project<TProjection>()`; **dual secondary/primary read preferences**
  (config-gated) in `Sportsbook.API/Repository/SportsbookRepository.cs`. Also read by
  `CashOutStake.Servicev2` and `QTech.API` (`MongoDB-Casino`).

## Method
1. Confirm which collection + projection the code reads ([docs/MongoDb.md](../../../docs/MongoDb.md)).
2. Decode abbreviated fields via [docs/BusinessGlossary.md](../../../docs/BusinessGlossary.md).
3. If data looks wrong/missing: the issue is likely **upstream in odds-engine-2** (the writer), not here.
4. Indexes are defined by odds-engine-2/DocumentDB — **Unknown** from this repo (U-28); don't assume.

## Don't
Add Mongo writes in platform-final. If you need to, you're in the wrong repo.
