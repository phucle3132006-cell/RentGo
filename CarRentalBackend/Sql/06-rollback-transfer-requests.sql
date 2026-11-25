SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('dbo.TransferRequests', 'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.TransferRequests WITH (TABLOCKX, HOLDLOCK))
        THROW 51000, 'Transfer request audit exists. Revert the app and retain the table.', 1;
    DROP TABLE dbo.TransferRequests;
END;
COMMIT;
