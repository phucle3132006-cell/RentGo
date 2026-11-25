SET XACT_ABORT ON;
SET NOCOUNT ON;
SET LOCK_TIMEOUT 5000;
BEGIN TRANSACTION;

IF DB_NAME() <> N'CarMotorbikeRentalDB' THROW 51000, 'Unexpected database.', 1;
IF COL_LENGTH('dbo.rental_orders', 'reviewed_at') IS NULL
    THROW 51001, 'Rental review migration is missing.', 1;

DECLARE @hasAudits bit = 0;
EXEC sp_executesql N'SELECT @hasAudits = CASE WHEN EXISTS
    (SELECT 1 FROM dbo.rental_orders WITH (TABLOCKX, HOLDLOCK) WHERE reviewed_at IS NOT NULL)
    THEN 1 ELSE 0 END;', N'@hasAudits bit OUTPUT', @hasAudits OUTPUT;
IF @hasAudits = 1 THROW 51002, 'Rollback would discard review audits. Preserve the audit data first.', 1;

DROP INDEX UX_payments_paid_order ON dbo.payments;
ALTER TABLE dbo.rental_orders DROP CONSTRAINT CK_rental_orders_review_audit, FK_rental_orders_reviewed_by_user;
ALTER TABLE dbo.rental_orders DROP COLUMN rejection_reason, reviewed_at, reviewed_by_user_id;
COMMIT TRANSACTION;
