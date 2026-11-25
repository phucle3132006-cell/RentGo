SET XACT_ABORT ON;
SET ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS, ARITHABORT, CONCAT_NULL_YIELDS_NULL, QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.TransferRequests', 'PaymentMethod') IS NOT NULL
BEGIN
    EXEC ('IF EXISTS (SELECT 1 FROM dbo.TransferRequests WHERE PaymentMethod = ''CASH'')
        THROW 51002, ''Cannot remove payment method while cash request history exists.'', 1;');
    ALTER TABLE dbo.TransferRequests DROP CONSTRAINT CK_TransferRequests_PaymentMethod,
        DF_TransferRequests_PaymentMethod;
    ALTER TABLE dbo.TransferRequests DROP COLUMN PaymentMethod;
END;
IF COL_LENGTH('dbo.users', 'session_version') IS NOT NULL
BEGIN
    EXEC ('IF EXISTS (SELECT 1 FROM dbo.users WHERE session_version IS NOT NULL)
        THROW 51003, ''Rotate the JWT signing key and use a deliberate data migration for rollback.'', 1;');
    ALTER TABLE dbo.users DROP COLUMN session_version;
END;
COMMIT;
