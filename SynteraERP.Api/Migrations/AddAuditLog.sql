-- Migration: AddAuditLog
-- Run this once against your database to create the AuditLogs table.
-- After running, restart the backend server so EF picks up the new table.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AuditLogs')
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id]               UNIQUEIDENTIFIER   NOT NULL DEFAULT NEWID(),
        [Action]           NVARCHAR(50)       NOT NULL,
        [Scope]            NVARCHAR(200)      NOT NULL,
        [TotalDeleted]     INT                NOT NULL DEFAULT 0,
        [Details]          NVARCHAR(MAX)      NOT NULL DEFAULT N'{}',
        [PerformedBy]      UNIQUEIDENTIFIER   NOT NULL,
        [PerformedByName]  NVARCHAR(100)      NOT NULL DEFAULT N'',
        [IpAddress]        NVARCHAR(45)       NULL,
        [CreatedAt]        DATETIMEOFFSET     NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
    PRINT 'AuditLogs table created.';
END
ELSE
BEGIN
    PRINT 'AuditLogs table already exists — skipped.';
END
