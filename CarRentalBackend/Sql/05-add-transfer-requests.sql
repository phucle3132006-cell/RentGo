SET XACT_ABORT ON;
SET ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS, ARITHABORT, CONCAT_NULL_YIELDS_NULL, QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRANSACTION;
IF OBJECT_ID('dbo.TransferRequests', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TransferRequests (
        Id bigint IDENTITY PRIMARY KEY,
        OrderId bigint NOT NULL REFERENCES dbo.rental_orders(id),
        CustomerId bigint NOT NULL REFERENCES dbo.users(id),
        SubmissionKey uniqueidentifier NOT NULL,
        Status nvarchar(16) NOT NULL CHECK (Status IN ('PENDING', 'APPROVED', 'REJECTED', 'CANCELLED')),
        SubmittedAt datetime2 NOT NULL,
        ReviewedAt datetime2 NULL,
        ReviewedByUserId bigint NULL REFERENCES dbo.users(id),
        PaymentId bigint NULL REFERENCES dbo.payments(id),
        Reason nvarchar(500) NULL,
        CONSTRAINT UQ_TransferRequests_Submission UNIQUE (OrderId, SubmissionKey)
    );
    CREATE UNIQUE INDEX UX_TransferRequests_Pending ON dbo.TransferRequests(OrderId) WHERE Status = N'PENDING';
END;
COMMIT;
