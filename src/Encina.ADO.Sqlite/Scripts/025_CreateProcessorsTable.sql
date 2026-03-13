CREATE TABLE IF NOT EXISTS Processors (
    Id                                TEXT    NOT NULL PRIMARY KEY,
    Name                              TEXT    NOT NULL,
    Country                           TEXT    NOT NULL,
    ContactEmail                      TEXT    NULL,
    ParentProcessorId                 TEXT    NULL,
    Depth                             INTEGER NOT NULL,
    SubProcessorAuthorizationTypeValue INTEGER NOT NULL,
    TenantId                          TEXT    NULL,
    ModuleId                          TEXT    NULL,
    CreatedAtUtc                      TEXT    NOT NULL,
    LastUpdatedAtUtc                  TEXT    NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_Processors_ParentProcessorId
    ON Processors (ParentProcessorId);

CREATE INDEX IF NOT EXISTS IX_Processors_TenantId
    ON Processors (TenantId);
