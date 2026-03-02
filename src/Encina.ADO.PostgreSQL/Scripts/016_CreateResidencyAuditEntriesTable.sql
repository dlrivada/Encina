CREATE TABLE residencyauditentries (
    id           VARCHAR(36)   NOT NULL PRIMARY KEY,
    entityid     VARCHAR(256)  NULL,
    datacategory VARCHAR(256)  NOT NULL,
    sourceregion VARCHAR(32)   NOT NULL,
    targetregion VARCHAR(32)   NULL,
    actionvalue  INT           NOT NULL,
    outcomevalue INT           NOT NULL,
    legalbasis   VARCHAR(256)  NULL,
    requesttype  VARCHAR(512)  NULL,
    userid       VARCHAR(256)  NULL,
    timestamputc TIMESTAMPTZ   NOT NULL,
    details      TEXT          NULL
);

CREATE INDEX ix_residencyaudit_entityid ON residencyauditentries (entityid);
CREATE INDEX ix_residencyaudit_timestamputc ON residencyauditentries (timestamputc);
CREATE INDEX ix_residencyaudit_outcomevalue ON residencyauditentries (outcomevalue);
