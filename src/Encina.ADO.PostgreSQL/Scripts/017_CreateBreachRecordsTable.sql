CREATE TABLE breachrecords (
    id                                VARCHAR(36)   NOT NULL PRIMARY KEY,
    nature                            TEXT          NOT NULL,
    approximatesubjectsaffected       INT           NOT NULL,
    categoriesofdataaffected          TEXT          NOT NULL,
    dpocontactdetails                 VARCHAR(1024) NOT NULL,
    likelyconsequences                TEXT          NOT NULL,
    measurestaken                     TEXT          NOT NULL,
    detectedatutc                     TIMESTAMPTZ   NOT NULL,
    notificationdeadlineutc           TIMESTAMPTZ   NOT NULL,
    notifiedauthorityatutc            TIMESTAMPTZ   NULL,
    notifiedsubjectsatutc             TIMESTAMPTZ   NULL,
    severityvalue                     INT           NOT NULL,
    statusvalue                       INT           NOT NULL,
    delayreason                       TEXT          NULL,
    subjectnotificationexemptionvalue INT           NOT NULL,
    resolvedatutc                     TIMESTAMPTZ   NULL,
    resolutionsummary                 TEXT          NULL
);

CREATE INDEX ix_breachrecords_statusvalue ON breachrecords (statusvalue);
CREATE INDEX ix_breachrecords_detectedatutc ON breachrecords (detectedatutc);
