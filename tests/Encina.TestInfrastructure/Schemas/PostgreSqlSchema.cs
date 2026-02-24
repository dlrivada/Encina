using Npgsql;

namespace Encina.TestInfrastructure.Schemas;

/// <summary>
/// PostgreSQL schema creation for Encina test databases.
/// </summary>
/// <remarks>
/// All table and column names use lowercase without quotes to match PostgreSQL's default
/// identifier folding behavior. This ensures queries using unquoted identifiers work correctly.
/// </remarks>
public static class PostgreSqlSchema
{
    /// <summary>
    /// Creates the Outbox table schema.
    /// </summary>
    public static async Task CreateOutboxSchemaAsync(NpgsqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS outboxmessages (
                id UUID PRIMARY KEY,
                notificationtype VARCHAR(500) NOT NULL,
                content TEXT NOT NULL,
                createdatutc TIMESTAMP NOT NULL,
                processedatutc TIMESTAMP NULL,
                errormessage TEXT NULL,
                retrycount INTEGER NOT NULL DEFAULT 0,
                nextretryatutc TIMESTAMP NULL
            );

            CREATE INDEX IF NOT EXISTS ix_outboxmessages_processedatutc_nextretryatutc
            ON outboxmessages(processedatutc, nextretryatutc);
            """;

        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the Inbox table schema.
    /// </summary>
    public static async Task CreateInboxSchemaAsync(NpgsqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS inboxmessages (
                messageid VARCHAR(256) PRIMARY KEY,
                requesttype VARCHAR(500) NOT NULL,
                receivedatutc TIMESTAMP NOT NULL,
                processedatutc TIMESTAMP NULL,
                response TEXT NULL,
                errormessage TEXT NULL,
                retrycount INTEGER NOT NULL DEFAULT 0,
                nextretryatutc TIMESTAMP NULL,
                expiresatutc TIMESTAMP NOT NULL,
                metadata TEXT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_inboxmessages_expiresatutc
            ON inboxmessages(expiresatutc);
            """;

        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the Saga table schema.
    /// </summary>
    public static async Task CreateSagaSchemaAsync(NpgsqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS sagastates (
                sagaid UUID PRIMARY KEY,
                sagatype VARCHAR(500) NOT NULL,
                currentstep INTEGER NOT NULL,
                status VARCHAR(50) NOT NULL,
                data TEXT NOT NULL,
                startedatutc TIMESTAMP NOT NULL,
                lastupdatedatutc TIMESTAMP NOT NULL,
                completedatutc TIMESTAMP NULL,
                errormessage TEXT NULL,
                timeoutatutc TIMESTAMP NULL,
                correlationid VARCHAR(256) NULL,
                metadata TEXT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_sagastates_status_lastupdatedatutc
            ON sagastates(status, lastupdatedatutc);
            """;

        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the Scheduling table schema.
    /// </summary>
    public static async Task CreateSchedulingSchemaAsync(NpgsqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS scheduledmessages (
                id UUID PRIMARY KEY,
                requesttype VARCHAR(500) NOT NULL,
                content TEXT NOT NULL,
                scheduledatutc TIMESTAMP NOT NULL,
                createdatutc TIMESTAMP NOT NULL,
                processedatutc TIMESTAMP NULL,
                lastexecutedatutc TIMESTAMP NULL,
                errormessage TEXT NULL,
                retrycount INTEGER NOT NULL DEFAULT 0,
                nextretryatutc TIMESTAMP NULL,
                correlationid VARCHAR(256) NULL,
                metadata TEXT NULL,
                isrecurring BOOLEAN NOT NULL DEFAULT FALSE,
                cronexpression VARCHAR(200) NULL
            );

            CREATE INDEX IF NOT EXISTS ix_scheduledmessages_scheduledatutc_processedatutc
            ON scheduledmessages(scheduledatutc, processedatutc);
            """;

        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the Orders table schema for immutable update integration tests.
    /// </summary>
    public static async Task CreateOrdersSchemaAsync(NpgsqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS orders (
                id UUID PRIMARY KEY,
                customername VARCHAR(200) NOT NULL,
                status VARCHAR(50) NOT NULL
            );
            """;

        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the TestRepositoryEntities table schema for repository integration tests.
    /// </summary>
    public static async Task CreateTestRepositorySchemaAsync(NpgsqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS testrepositoryentities (
                id UUID PRIMARY KEY,
                name VARCHAR(200) NOT NULL,
                amount DECIMAL(18,2) NOT NULL,
                isactive BOOLEAN NOT NULL,
                createdatutc TIMESTAMP NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_testrepositoryentities_isactive
            ON testrepositoryentities(isactive);
            """;

        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the TenantTestEntities table schema for multi-tenancy integration tests.
    /// </summary>
    public static async Task CreateTenantTestSchemaAsync(NpgsqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS tenanttestentities (
                id UUID PRIMARY KEY,
                tenantid VARCHAR(128) NOT NULL,
                name VARCHAR(200) NOT NULL,
                description VARCHAR(1000) NULL,
                amount DECIMAL(18,2) NOT NULL,
                isactive BOOLEAN NOT NULL DEFAULT TRUE,
                createdatutc TIMESTAMPTZ NOT NULL,
                updatedatutc TIMESTAMPTZ NULL
            );

            CREATE INDEX IF NOT EXISTS ix_tenanttestentities_tenantid
            ON tenanttestentities(tenantid);

            CREATE INDEX IF NOT EXISTS ix_tenanttestentities_tenantid_isactive
            ON tenanttestentities(tenantid, isactive);

            CREATE INDEX IF NOT EXISTS ix_tenanttestentities_createdatutc
            ON tenanttestentities(createdatutc);
            """;

        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the ReadWriteTestEntities table schema for read/write separation tests.
    /// </summary>
    public static async Task CreateReadWriteTestSchemaAsync(NpgsqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS readwritetestentities (
                id UUID PRIMARY KEY,
                name VARCHAR(256) NOT NULL,
                value INTEGER NOT NULL,
                timestamp TIMESTAMPTZ NOT NULL,
                writecounter INTEGER NOT NULL DEFAULT 0
            );

            CREATE INDEX IF NOT EXISTS ix_readwritetestentities_timestamp
            ON readwritetestentities(timestamp);
            """;

        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the consentrecords table schema for consent management integration tests.
    /// </summary>
    public static async Task CreateConsentSchemaAsync(NpgsqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS consentrecords (
                id UUID PRIMARY KEY,
                subjectid VARCHAR(256) NOT NULL,
                purpose VARCHAR(256) NOT NULL,
                status INTEGER NOT NULL,
                consentversionid VARCHAR(256) NOT NULL,
                givenatutc TIMESTAMPTZ NOT NULL,
                withdrawnatutc TIMESTAMPTZ NULL,
                expiresatutc TIMESTAMPTZ NULL,
                source VARCHAR(256) NOT NULL,
                ipaddress VARCHAR(45) NULL,
                proofofconsent TEXT NULL,
                metadata TEXT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_consentrecords_subjectid
            ON consentrecords(subjectid);

            CREATE INDEX IF NOT EXISTS ix_consentrecords_subjectid_purpose
            ON consentrecords(subjectid, purpose);

            CREATE INDEX IF NOT EXISTS ix_consentrecords_status
            ON consentrecords(status);
            """;

        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the lawful basis registration and LIA record table schemas for lawful basis integration tests.
    /// </summary>
    /// <remarks>
    /// <para>
    /// PostgreSQL ADO and Dapper stores use different table/column naming conventions:
    /// </para>
    /// <list type="bullet">
    /// <item><description>ADO: flat lowercase (e.g. <c>lawfulbasisregistrations</c>, <c>requesttypename</c>)</description></item>
    /// <item><description>Dapper: snake_case (e.g. <c>lawful_basis_registrations</c>, <c>request_type_name</c>)</description></item>
    /// </list>
    /// <para>
    /// Both sets of tables are created so that integration tests for both providers can run against the same database.
    /// </para>
    /// </remarks>
    public static async Task CreateLawfulBasisSchemaAsync(NpgsqlConnection connection)
    {
        const string sql = """
            -- ADO tables (flat lowercase naming)
            CREATE TABLE IF NOT EXISTS lawfulbasisregistrations (
                id VARCHAR(450) NOT NULL PRIMARY KEY,
                requesttypename VARCHAR(450) NOT NULL UNIQUE,
                basisvalue INTEGER NOT NULL,
                purpose TEXT NULL,
                liareference TEXT NULL,
                legalreference TEXT NULL,
                contractreference TEXT NULL,
                registeredatutc TIMESTAMPTZ NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_lawfulbasisregistrations_requesttypename
            ON lawfulbasisregistrations(requesttypename);

            CREATE TABLE IF NOT EXISTS liarecords (
                id VARCHAR(450) NOT NULL PRIMARY KEY,
                name VARCHAR(450) NOT NULL,
                purpose TEXT NOT NULL,
                legitimateinterest TEXT NOT NULL,
                benefits TEXT NOT NULL,
                consequencesifnotprocessed TEXT NOT NULL,
                necessityjustification TEXT NOT NULL,
                alternativesconsideredjson TEXT NOT NULL,
                dataminimisationnotes TEXT NOT NULL,
                natureofdata TEXT NOT NULL,
                reasonableexpectations TEXT NOT NULL,
                impactassessment TEXT NOT NULL,
                safeguardsjson TEXT NOT NULL,
                outcomevalue INTEGER NOT NULL,
                conclusion TEXT NOT NULL,
                conditions TEXT NULL,
                assessedatutc TIMESTAMPTZ NOT NULL,
                assessedby VARCHAR(450) NOT NULL,
                dpoinvolvement BOOLEAN NOT NULL,
                nextreviewatutc TIMESTAMPTZ NULL
            );

            CREATE INDEX IF NOT EXISTS ix_liarecords_nextreviewatutc
            ON liarecords(nextreviewatutc);

            CREATE INDEX IF NOT EXISTS ix_liarecords_outcomevalue
            ON liarecords(outcomevalue);

            -- Dapper tables (snake_case naming)
            CREATE TABLE IF NOT EXISTS lawful_basis_registrations (
                id VARCHAR(450) NOT NULL PRIMARY KEY,
                request_type_name VARCHAR(450) NOT NULL UNIQUE,
                basis_value INTEGER NOT NULL,
                purpose TEXT NULL,
                lia_reference TEXT NULL,
                legal_reference TEXT NULL,
                contract_reference TEXT NULL,
                registered_at_utc TIMESTAMPTZ NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_lawful_basis_registrations_request_type_name
            ON lawful_basis_registrations(request_type_name);

            CREATE TABLE IF NOT EXISTS lia_records (
                id VARCHAR(450) NOT NULL PRIMARY KEY,
                name VARCHAR(450) NOT NULL,
                purpose TEXT NOT NULL,
                legitimate_interest TEXT NOT NULL,
                benefits TEXT NOT NULL,
                consequences_if_not_processed TEXT NOT NULL,
                necessity_justification TEXT NOT NULL,
                alternatives_considered_json TEXT NOT NULL,
                data_minimisation_notes TEXT NOT NULL,
                nature_of_data TEXT NOT NULL,
                reasonable_expectations TEXT NOT NULL,
                impact_assessment TEXT NOT NULL,
                safeguards_json TEXT NOT NULL,
                outcome_value INTEGER NOT NULL,
                conclusion TEXT NOT NULL,
                conditions TEXT NULL,
                assessed_at_utc TIMESTAMPTZ NOT NULL,
                assessed_by VARCHAR(450) NOT NULL,
                dpo_involvement BOOLEAN NOT NULL,
                next_review_at_utc TIMESTAMPTZ NULL
            );

            CREATE INDEX IF NOT EXISTS ix_lia_records_next_review_at_utc
            ON lia_records(next_review_at_utc);

            CREATE INDEX IF NOT EXISTS ix_lia_records_outcome_value
            ON lia_records(outcome_value);

            -- EF Core tables (PascalCase quoted identifiers)
            CREATE TABLE IF NOT EXISTS "LawfulBasisRegistrations" (
                "Id" VARCHAR(36) NOT NULL PRIMARY KEY,
                "RequestTypeName" VARCHAR(512) NOT NULL,
                "BasisValue" INTEGER NOT NULL,
                "Purpose" VARCHAR(1024) NULL,
                "LIAReference" VARCHAR(256) NULL,
                "LegalReference" VARCHAR(256) NULL,
                "ContractReference" VARCHAR(256) NULL,
                "RegisteredAtUtc" TIMESTAMPTZ NOT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS "IX_LawfulBasisRegistrations_RequestTypeName"
            ON "LawfulBasisRegistrations"("RequestTypeName");

            CREATE TABLE IF NOT EXISTS "LIARecords" (
                "Id" VARCHAR(256) NOT NULL PRIMARY KEY,
                "Name" VARCHAR(512) NOT NULL,
                "Purpose" VARCHAR(1024) NOT NULL,
                "LegitimateInterest" TEXT NOT NULL,
                "Benefits" TEXT NOT NULL,
                "ConsequencesIfNotProcessed" TEXT NOT NULL,
                "NecessityJustification" TEXT NOT NULL,
                "AlternativesConsideredJson" TEXT NOT NULL,
                "DataMinimisationNotes" TEXT NOT NULL,
                "NatureOfData" TEXT NOT NULL,
                "ReasonableExpectations" TEXT NOT NULL,
                "ImpactAssessment" TEXT NOT NULL,
                "SafeguardsJson" TEXT NOT NULL,
                "OutcomeValue" INTEGER NOT NULL,
                "Conclusion" TEXT NOT NULL,
                "Conditions" TEXT NULL,
                "AssessedAtUtc" TIMESTAMPTZ NOT NULL,
                "AssessedBy" VARCHAR(256) NOT NULL,
                "DPOInvolvement" BOOLEAN NOT NULL,
                "NextReviewAtUtc" TIMESTAMPTZ NULL
            );

            CREATE INDEX IF NOT EXISTS "IX_LIARecords_OutcomeValue"
            ON "LIARecords"("OutcomeValue");
            """;

        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Drops all Encina tables.
    /// </summary>
    public static async Task DropAllSchemasAsync(NpgsqlConnection connection)
    {
        const string sql = """
            DROP TABLE IF EXISTS "LIARecords" CASCADE;
            DROP TABLE IF EXISTS "LawfulBasisRegistrations" CASCADE;
            DROP TABLE IF EXISTS lia_records CASCADE;
            DROP TABLE IF EXISTS lawful_basis_registrations CASCADE;
            DROP TABLE IF EXISTS liarecords CASCADE;
            DROP TABLE IF EXISTS lawfulbasisregistrations CASCADE;
            DROP TABLE IF EXISTS consentrecords CASCADE;
            DROP TABLE IF EXISTS tenanttestentities CASCADE;
            DROP TABLE IF EXISTS readwritetestentities CASCADE;
            DROP TABLE IF EXISTS orders CASCADE;
            DROP TABLE IF EXISTS testrepositoryentities CASCADE;
            DROP TABLE IF EXISTS scheduledmessages CASCADE;
            DROP TABLE IF EXISTS sagastates CASCADE;
            DROP TABLE IF EXISTS inboxmessages CASCADE;
            DROP TABLE IF EXISTS outboxmessages CASCADE;
            """;

        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Creates the processingactivities table schema for GDPR processing activity integration tests.
    /// </summary>
    public static async Task CreateProcessingActivitySchemaAsync(NpgsqlConnection connection)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS processingactivities (
                id                             VARCHAR(36)   NOT NULL PRIMARY KEY,
                requesttypename                VARCHAR(1000) NOT NULL,
                name                           VARCHAR(500)  NOT NULL,
                purpose                        TEXT          NOT NULL,
                lawfulbasisvalue               INT           NOT NULL,
                categoriesofdatasubjectsjson   TEXT          NOT NULL,
                categoriesofpersonaldatajson   TEXT          NOT NULL,
                recipientsjson                 TEXT          NOT NULL,
                thirdcountrytransfers          TEXT          NULL,
                safeguards                     TEXT          NULL,
                retentionperiodticks           BIGINT        NOT NULL,
                securitymeasures               TEXT          NOT NULL,
                createdatutc                   TIMESTAMPTZ   NOT NULL,
                lastupdatedatutc               TIMESTAMPTZ   NOT NULL,
                CONSTRAINT uq_processingactivities_requesttypename UNIQUE (requesttypename)
            );
            """;

        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Clears all data from Encina tables without dropping schemas.
    /// Useful for cleaning between tests that share a database fixture.
    /// </summary>
    public static async Task ClearAllDataAsync(NpgsqlConnection connection)
    {
        const string sql = """
            DELETE FROM processingactivities;
            DELETE FROM "LIARecords";
            DELETE FROM "LawfulBasisRegistrations";
            DELETE FROM lia_records;
            DELETE FROM lawful_basis_registrations;
            DELETE FROM liarecords;
            DELETE FROM lawfulbasisregistrations;
            DELETE FROM consentrecords;
            DELETE FROM tenanttestentities;
            DELETE FROM readwritetestentities;
            DELETE FROM orders;
            DELETE FROM testrepositoryentities;
            DELETE FROM scheduledmessages;
            DELETE FROM sagastates;
            DELETE FROM inboxmessages;
            DELETE FROM outboxmessages;
            """;

        using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
