CREATE TABLE IF NOT EXISTS DataLocations (
    Id                TEXT    NOT NULL PRIMARY KEY,
    EntityId          TEXT    NOT NULL,
    DataCategory      TEXT    NOT NULL,
    RegionCode        TEXT    NOT NULL,
    StorageTypeValue  INTEGER NOT NULL,
    StoredAtUtc       TEXT    NOT NULL,
    LastVerifiedAtUtc TEXT    NULL,
    Metadata          TEXT    NULL
);

CREATE INDEX IF NOT EXISTS IX_DataLocations_EntityId ON DataLocations (EntityId);
CREATE INDEX IF NOT EXISTS IX_DataLocations_RegionCode ON DataLocations (RegionCode);
CREATE INDEX IF NOT EXISTS IX_DataLocations_DataCategory ON DataLocations (DataCategory);
