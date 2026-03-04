CREATE TABLE breachauditentries (
    id                VARCHAR(36)   NOT NULL PRIMARY KEY,
    breachid          VARCHAR(36)   NOT NULL,
    action            VARCHAR(256)  NOT NULL,
    detail            TEXT          NULL,
    performedbyuserid VARCHAR(256)  NULL,
    occurredatutc     TIMESTAMPTZ   NOT NULL
);

CREATE INDEX ix_breachauditentries_breachid ON breachauditentries (breachid);
