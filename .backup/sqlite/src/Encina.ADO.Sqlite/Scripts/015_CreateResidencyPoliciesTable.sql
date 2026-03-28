CREATE TABLE IF NOT EXISTS ResidencyPolicies (
    DataCategory              TEXT    NOT NULL PRIMARY KEY,
    AllowedRegionCodes        TEXT    NOT NULL,
    RequireAdequacyDecision   INTEGER NOT NULL,
    AllowedTransferBasesValue TEXT    NULL,
    CreatedAtUtc              TEXT    NOT NULL,
    LastModifiedAtUtc         TEXT    NULL
);
