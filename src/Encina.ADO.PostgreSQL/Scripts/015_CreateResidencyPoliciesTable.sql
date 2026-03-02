CREATE TABLE residencypolicies (
    datacategory              VARCHAR(256)  NOT NULL PRIMARY KEY,
    allowedregioncodes        VARCHAR(1024) NOT NULL,
    requireadequacydecision   BOOLEAN       NOT NULL,
    allowedtransferbasesvalue VARCHAR(256)  NULL,
    createdatutc              TIMESTAMPTZ   NOT NULL,
    lastmodifiedatutc         TIMESTAMPTZ   NULL
);
