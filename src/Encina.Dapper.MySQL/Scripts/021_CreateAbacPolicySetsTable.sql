-- =============================================
-- Create abac_policy_sets table for MySQL/MariaDB
-- ABAC policy set storage with JSON policy graph
-- =============================================

CREATE TABLE IF NOT EXISTS `abac_policy_sets`
(
    `Id` VARCHAR(256) NOT NULL,
    `Version` VARCHAR(256) NULL,
    `Description` TEXT NULL,
    `PolicyJson` JSON NOT NULL,
    `IsEnabled` TINYINT(1) NOT NULL DEFAULT 1,
    `Priority` INT NOT NULL DEFAULT 0,
    `CreatedAtUtc` DATETIME(6) NOT NULL,
    `UpdatedAtUtc` DATETIME(6) NOT NULL,

    PRIMARY KEY (`Id`),

    INDEX `IX_abac_policy_sets_IsEnabled_Priority` (`IsEnabled`, `Priority`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
