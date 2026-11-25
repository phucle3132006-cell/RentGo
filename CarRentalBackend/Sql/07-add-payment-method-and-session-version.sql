SET XACT_ABORT ON;
SET ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS, ARITHABORT, CONCAT_NULL_YIELDS_NULL, QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.TransferRequests', 'PaymentMethod') IS NULL
    ALTER TABLE dbo.TransferRequests ADD PaymentMethod varchar(16) NOT NULL
        CONSTRAINT DF_TransferRequests_PaymentMethod DEFAULT ('BANK_TRANSFER')
        CONSTRAINT CK_TransferRequests_PaymentMethod CHECK (PaymentMethod IN ('BANK_TRANSFER', 'CASH'));
IF COL_LENGTH('dbo.users', 'session_version') IS NULL
    ALTER TABLE dbo.users ADD session_version uniqueidentifier NULL;
COMMIT;
