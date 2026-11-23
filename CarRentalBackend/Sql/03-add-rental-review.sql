SET XACT_ABORT ON;
SET NOCOUNT ON;
SET LOCK_TIMEOUT 5000;
SET ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS, ARITHABORT, CONCAT_NULL_YIELDS_NULL, QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRANSACTION;

IF DB_NAME() <> N'CarMotorbikeRentalDB' THROW 51000, 'Unexpected database.', 1;
IF COL_LENGTH('dbo.payments', 'confirmed_by_user_id') IS NULL
   OR COL_LENGTH('dbo.payments', 'external_reference') IS NULL
    THROW 51001, 'Apply the manual payment migration first.', 1;
IF COL_LENGTH('dbo.rental_orders', 'reviewed_at') IS NOT NULL
    THROW 51002, 'Rental review migration already exists.', 1;

IF EXISTS (SELECT rental_order_id FROM dbo.payments WITH (TABLOCKX, HOLDLOCK)
           WHERE status = 'PAID' GROUP BY rental_order_id HAVING COUNT_BIG(*) > 1)
    THROW 51003, 'Duplicate PAID transactions require reconciliation before migration.', 1;

ALTER TABLE dbo.rental_orders ADD reviewed_by_user_id bigint NULL, reviewed_at datetime2 NULL,
    rejection_reason nvarchar(500) NULL;
EXEC(N'ALTER TABLE dbo.rental_orders WITH CHECK ADD
    CONSTRAINT FK_rental_orders_reviewed_by_user FOREIGN KEY (reviewed_by_user_id) REFERENCES dbo.users(id),
    CONSTRAINT CK_rental_orders_review_audit CHECK (
        (reviewed_by_user_id IS NULL AND reviewed_at IS NULL AND rejection_reason IS NULL)
        OR (reviewed_by_user_id IS NOT NULL AND reviewed_at IS NOT NULL AND status <> ''PENDING''
            AND (rejection_reason IS NULL OR (status = ''CANCELLED'' AND LEN(LTRIM(rejection_reason)) > 0)))
    );');
CREATE UNIQUE INDEX UX_payments_paid_order ON dbo.payments(rental_order_id) WHERE status = 'PAID';
COMMIT TRANSACTION;
