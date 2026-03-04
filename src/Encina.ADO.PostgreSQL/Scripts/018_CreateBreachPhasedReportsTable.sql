CREATE TABLE breachphasedreports (
    id                VARCHAR(36)   NOT NULL PRIMARY KEY,
    breachid          VARCHAR(36)   NOT NULL REFERENCES breachrecords(id),
    reportnumber      INT           NOT NULL,
    content           TEXT          NOT NULL,
    submittedatutc    TIMESTAMPTZ   NOT NULL,
    submittedbyuserid VARCHAR(256)  NULL
);

CREATE INDEX ix_breachphasedreports_breachid ON breachphasedreports (breachid);
