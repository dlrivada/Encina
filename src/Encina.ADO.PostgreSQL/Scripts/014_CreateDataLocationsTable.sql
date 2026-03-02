CREATE TABLE datalocations (
    id                VARCHAR(36)   NOT NULL PRIMARY KEY,
    entityid          VARCHAR(256)  NOT NULL,
    datacategory      VARCHAR(256)  NOT NULL,
    regioncode        VARCHAR(32)   NOT NULL,
    storagetypevalue  INT           NOT NULL,
    storedatutc       TIMESTAMPTZ   NOT NULL,
    lastverifiedatutc TIMESTAMPTZ   NULL,
    metadata          TEXT          NULL
);

CREATE INDEX ix_datalocations_entityid ON datalocations (entityid);
CREATE INDEX ix_datalocations_regioncode ON datalocations (regioncode);
CREATE INDEX ix_datalocations_datacategory ON datalocations (datacategory);
