-- RentGo - Combined revenue demo seed and verification
-- Run in SSMS on backed-up local/test database after vehicle seed.
-- Cleanup is included as commented reference; it never executes by default.
-- To remove demo rows, run ONLY section 3 separately after removing its line comments and setting @confirm=1.

-- ===== SECTION 1: INSERT SEED DATA =====
-- RentGo DEMO ONLY. Run on a backed-up local/test database; stop application first.
-- All times are UTC; calendar grouping in Vietnam uses UTC+7.
USE [CarMotorbikeRentalDB];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN TRY
 BEGIN TRAN;
 DECLARE @lock int;
 EXEC @lock = sys.sp_getapplock @Resource=N'RentGo.RevenueSeed.V1', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000;
 IF @lock < 0 THROW 51000, 'Could not acquire seed lock.', 1;
 -- A successful previous run is a no-op. Partial/colliding data is rejected.
 IF EXISTS (SELECT 1 FROM dbo.rental_orders WHERE order_code LIKE 'RGREV1-%')
 BEGIN
   IF (SELECT COUNT(*) FROM dbo.rental_orders WHERE order_code LIKE 'RGREV1-%' AND note=N'RENTGO_REVENUE_DEMO_V1') <> 500
      OR (SELECT COUNT(*) FROM dbo.users WHERE email LIKE 'rgrev1-%@example.invalid') <> 50
      OR (SELECT COUNT(*) FROM dbo.payments WHERE transaction_code LIKE 'RGREV1-%' AND status='PAID') <> 400
      OR (SELECT COUNT(*) FROM dbo.TransferRequests t JOIN dbo.rental_orders o ON o.id=t.OrderId WHERE o.order_code LIKE 'RGREV1-%' AND CONVERT(varchar(36),t.SubmissionKey) LIKE '72657631-0000-4000-8000-%') <> 30
      THROW 51001, 'Partial or modified seed detected. Inspect before rerunning.', 1;
   COMMIT;
   PRINT 'Revenue seed already exists; nothing inserted.';
   RETURN;
 END;
 IF EXISTS (SELECT 1 FROM dbo.users WHERE email LIKE 'rgrev1-%@example.invalid' OR phone LIKE '000000%')
    OR EXISTS (SELECT 1 FROM dbo.payments WHERE transaction_code LIKE 'RGREV1-%')
    THROW 51002, 'Seed identifiers already exist. No changes made.', 1;
 DECLARE @admin bigint = (SELECT MIN(id) FROM dbo.users WHERE role='ADMIN' AND status='ACTIVE');
 IF @admin IS NULL THROW 51003, 'Create an active ADMIN before running this seed.', 1;
 DECLARE @now datetime2(7)=SYSUTCDATETIME();
 DECLARE @today datetime2(7)=DATEADD(hour,-7,CONVERT(datetime2(7),CONVERT(date,DATEADD(hour,7,@now))));
 DECLARE @first datetime2(7)='2024-12-31T17:00:00'; -- Jan 1 2025 Vietnam
 DECLARE @historyDays int=DATEDIFF(day,@first,@today)-12;
 IF @historyDays < 31 THROW 51004, 'System date is too early for historical seed.', 1;
 DECLARE @fleet TABLE(id bigint PRIMARY KEY, price decimal(10,2));
 -- Use only existing vehicles from 01_seed_vehicles.sql; no vehicle writes.
 INSERT @fleet SELECT v.id,m.price_per_day FROM dbo.vehicles v JOIN dbo.vehicle_models m ON m.id=v.vehicle_model_id
 WHERE v.status='AVAILABLE' AND m.price_per_day>0 AND m.price_per_day<=14285714.28 AND v.license_plate IN (
 '59-C1 10001',
 '59-C1 98002',
 '59-C1 98003',
 '59-C1 98004',
 '59-C1 98005',
 '59-C1 98006',
 '59-C1 98007',
 '59-C1 98008',
 '59-C1 98009',
 '59-C1 98010',
 '59-C1 98011',
 '59-C1 98012',
 '59-C1 98013',
 '59-C1 98014',
 '59-C1 98015',
 '59-C1 98016',
 '59-C1 98017',
 '59-C1 98018',
 '59-C1 98019',
 '59-C1 98020',
 '59-C1 98021',
 '59-C1 98022',
 '59-C1 98023',
 '59-C1 98024',
 '59-C1 98025',
 '59-C1 98026',
 '59-C1 98027',
 '59-C1 98028',
 '59-C1 98029',
 '59-C1 98030',
 '51A-98031',
 '51A-98032',
 '51A-98033',
 '51A-98034',
 '51A-98035',
 '51A-98036',
 '51A-98037',
 '51A-98038',
 '51A-98039',
 '51A-98040');
 IF (SELECT COUNT(*) FROM @fleet)<20 THROW 51005, 'Run vehicle seed first; need at least 20 available seed vehicles.', 1;
 DECLARE @credentials TABLE(n int PRIMARY KEY, hash varchar(255));
 -- Valid ASP.NET Identity V3 hashes of undisclosed random passwords; not shared login accounts.
 INSERT @credentials VALUES
 (1, 'AQAAAAIAAYagAAAAEI362WYpPWN7HJswfVK/jJOjL6hxsFv/gPiKSb1SnPy75KxlOcCTTh9VrjM0WiaL8Q=='),
 (2, 'AQAAAAIAAYagAAAAEA0HL+n7Qj5AqMiq8MuWFFsIhQewV8NzaAxpc2P7mHkfQ7oqMnILSHRs4th58yxW6g=='),
 (3, 'AQAAAAIAAYagAAAAEGMZSnfWj0C+Tiq+IEjxo9QcGXl+vrLVgCOXQkDWoJ7lYpQS9swhwY15oVV7ZNASQg=='),
 (4, 'AQAAAAIAAYagAAAAEJwu/RpxEt0Bq86s5KhwVcRGadjo8vzaIjjrnSbE1ATsqxYaz9kzKy/AArAypwstew=='),
 (5, 'AQAAAAIAAYagAAAAECr30o7poKRK5DJt5ASaMQ4u31BbogcmwfIOEXnxHrINtPxmS8mf23YYdHDwcqqzYQ=='),
 (6, 'AQAAAAIAAYagAAAAEAnT41EzmccCK062xWC+Wh7/TJyNEbOxAQaW+KkyvVeuhigMZ/f9zzk0i3SQY9gg9A=='),
 (7, 'AQAAAAIAAYagAAAAEPc12uaQGNRDgsdrJ+/sHAfqU6viwL9ziGEYuPpHN/QU9g995z2S9EnuIo6ESPJRsw=='),
 (8, 'AQAAAAIAAYagAAAAEBgMKUjA3fcbTDJaxNpsjArVFtPKUNCbEdBqVwZt2Mi9f8923Q3E/Hx98VoLnYhKKA=='),
 (9, 'AQAAAAIAAYagAAAAEKn6Oo1fF65Vc4K6dxnlpE1wpT87Rsg7j5wTC6cKLm4ZnmqHPqDdpsiDCLavQRHa8g=='),
 (10, 'AQAAAAIAAYagAAAAEBD0ymJJWqA4TsXo0y9l6GTLW00M73C09gmraMsw7S9ZWQvE++NX5X6XWZGuVqzd7Q=='),
 (11, 'AQAAAAIAAYagAAAAEBt6LsGwgQnIAzgYdCAt+64RNRYwwJ3Y4PZOdeaSlWVSXiB5CEcIM3ZMYFtQzaKS2g=='),
 (12, 'AQAAAAIAAYagAAAAEMByZSfVijfs3oHDr2YzrIdLiNGTBvem8Ug70ybst9cGxccPAGDTHiHlyv2LZCNYyQ=='),
 (13, 'AQAAAAIAAYagAAAAELZ/5hBjm/UcRmM9auc/YsXGpp6numocKkFoVMmwZgXHV+p+P2Ul/L/fnbekqy+fhA=='),
 (14, 'AQAAAAIAAYagAAAAEJMfo44YEo9cQ53lGHGJqPyuHqam1a3VeLHpNobfvq8TFUedPMhk1JK9Eye+2agq9Q=='),
 (15, 'AQAAAAIAAYagAAAAEAEntO8pDXEKYdTfojniwYbs9NVsqz997XwQ2q8iE9IqJ74acivdCtAhlqDrzl9fhg=='),
 (16, 'AQAAAAIAAYagAAAAEOsKKZ0IsLkgNRqNRefre881/xk/W2qNLM8JPWlTEnv3cGCwSRYH4SsYpSoM1NQ3dg=='),
 (17, 'AQAAAAIAAYagAAAAENwDC6o4kLAp2JuBtvuuyiC6woiBZ1K5gcUGkePyewCs6koYt+dLlsYetj4WTXg2Ow=='),
 (18, 'AQAAAAIAAYagAAAAEOACEQrvUIczBaw43Np/Z2j1SiW+b2fpckbRoODLYffMkgI+odEOHcqA3DVwEVnnww=='),
 (19, 'AQAAAAIAAYagAAAAEIufdYzcQDJuem/Lpr2Pc0hsJ+Scoa4FjGrBROMbm1CFjXLq1JNtfnoTRilv1naJUg=='),
 (20, 'AQAAAAIAAYagAAAAEGGe06NBnSAGdb//3GZgo89M2/+s+bXN/xx9eAgUqrQHtqbig8CeYdXOHYlY0fPBlw=='),
 (21, 'AQAAAAIAAYagAAAAEOb9CfR2QLzRW/SzE7uql1q4Ma4WdN+2OTVmkKxbKjpvPzyyRzgktQBgv8Oy/DzPbw=='),
 (22, 'AQAAAAIAAYagAAAAENm9oeKgrZqxEmnlOHdhrY1+UPQPe6bv/4A3X5JxDKK+w5gp7oVU82ADCOMGQMRWLQ=='),
 (23, 'AQAAAAIAAYagAAAAEKZkHw+YZasAGhcno0pOloX3SVy+Ofa53ApXZ2Nk5QfowpS/T9zw51JtqUvts9hs7g=='),
 (24, 'AQAAAAIAAYagAAAAEBsJ0CWzxQ3qPrfwe2SqP8PPyN28DXyAzcRGjFIZ0L+jJiPXLWmEulFGIKrVHCSe9Q=='),
 (25, 'AQAAAAIAAYagAAAAEEKC5LUEr/V0GdgHlr+h0TG75FnKgiu5BZoNk/zeumScloRCGtwP8p0A3pXWWTQFpw=='),
 (26, 'AQAAAAIAAYagAAAAEMIlY3+w0MgvMzGaaH5TVG58dMEL6ScpBowWaHaka87mkzXUlZ7sEFcp2I4YKVypTw=='),
 (27, 'AQAAAAIAAYagAAAAEKYZwhxI0SWcrG0CgDh+uw21zXsUwNDCTXPSaMOCP/yj1rSQov6dNQhtCDWCO64xyQ=='),
 (28, 'AQAAAAIAAYagAAAAEPqJx5QBniaePJnNfPc1NT5BfK12719wqn2KYEVnCitCSOO8JRDKaKfi+vYuBoXXpg=='),
 (29, 'AQAAAAIAAYagAAAAEMFjz4UB4az4hOkA0CqzAFPNLMAI1ZEhF8QEm5sf4Bsar3WfwfXklsHCBDEYkphR3g=='),
 (30, 'AQAAAAIAAYagAAAAEDcUKU+jbGmC6yU+16QLbXB6FmDOWJL3tpI9n2w7h6/FrplZ169JQPoaBlhFX4ZRbQ=='),
 (31, 'AQAAAAIAAYagAAAAECtxtnmfp+vhyVwHCZbCLHZNiBu5PWq3tLtjR8/TDpk4JejOqUP8zwndF6IVTAa88A=='),
 (32, 'AQAAAAIAAYagAAAAEIKLFOLx/uj5RfEh1wgGZQvFN/9r8QjPnQQpazjXO81eJQ0A/2jKpd7/HKgefCYlfg=='),
 (33, 'AQAAAAIAAYagAAAAEPoEj0WrIWmonLPeeDVsFxhHZq8wMrpMhD6RX7tfc4pM+juFTtBdkUGS880aZ/7F1A=='),
 (34, 'AQAAAAIAAYagAAAAECK3P9Ik8+ZMWcMGLDzr3JEgYYZam3jTg0U1Wb7oT1eoI5yrcX4Rui/QHiwGKNt3dw=='),
 (35, 'AQAAAAIAAYagAAAAEJ1EOMz34ey3+vxroQ2mnD3gs4axaJVB7g7fdUoXZsqdKx0tLaHtEPkqLwCa8s43TA=='),
 (36, 'AQAAAAIAAYagAAAAEP2bzrlYV2lJtIZCK3H+1Vly8+Uyg0grXqLgTNVL1wFZxLO+uzkwwQla6qezTr+bZw=='),
 (37, 'AQAAAAIAAYagAAAAEEvP8jjnKcct4CAjQ0l1HSEboQEqebqwJEAiLUB2Oo5oX7xeHf5VBNnjw4LBLzLYRw=='),
 (38, 'AQAAAAIAAYagAAAAEA3C6gVVSInnbDV53a7vVY1dE9LNeBuw6Ml+DuVj7/rnuD73TVvuDJPljwklIJ+IYA=='),
 (39, 'AQAAAAIAAYagAAAAEIAFNQkQCu3BbQg6i6vWYk92xbmGlbJrkmoD2IHJshN7B0Iiv5VHXG2DtnBtCZ9/Bg=='),
 (40, 'AQAAAAIAAYagAAAAEBzPqZvGHwbloaCyPJRMbz+cbNoorDKLIVRtSgnXR9S0JcL7Sey4v4BANLZvqBTp5Q=='),
 (41, 'AQAAAAIAAYagAAAAEJowTsKsfLR7MPQ0W6XOWz9TV+zHytzfAn91tU1jbpG+Donfp9D7lJ/perGANevtGA=='),
 (42, 'AQAAAAIAAYagAAAAEHVRNkZrCCWPFAT0mozZU2o+r+Ejx3Mo1wXCAw9BybaQsWvK4mA1vx0fZjC/NPPXLA=='),
 (43, 'AQAAAAIAAYagAAAAENwLAMsMxm9bmO3qNOBqUUSKvFt5rfyZq22snDN61TEsV3kALWPJ0His2h5Kj5tRyg=='),
 (44, 'AQAAAAIAAYagAAAAELSEvqTP9UCByyIX/Wgk+jMKV8Suv1XY0mL1Nvg+8QaMxK/TDaPCYo/SCRJHTo+1Bg=='),
 (45, 'AQAAAAIAAYagAAAAENu7nLah2GHaWyucwM9nguI8YivY9eXy96Y8mhRwzKcJI2CmWkacRjZuKO+uR9p4Uw=='),
 (46, 'AQAAAAIAAYagAAAAEA+d3AnfEWsGrypkS/RMJe0EysBX+IMz+YYCECriN3g9p6tK/wqAYU47ibIhdl4Lzw=='),
 (47, 'AQAAAAIAAYagAAAAEB9pMBswKMTfVRR4/6tS4qA8XEdxV9LHGDMTU1t6+0e5isc+X2Dx+u4hGf8DZ5ia3g=='),
 (48, 'AQAAAAIAAYagAAAAEIVe2JnoeeHWsffYmSodXRDe8FC6zhO4DhI4uVQ3VKsYy0rqKNT14oxL4CQR78bdpQ=='),
 (49, 'AQAAAAIAAYagAAAAEGkUt49bBqH3K3TJBJK+eQD6EQ/5wi3Dwo/v0ulWVXO0/1g1129l0Z8IoD64uEdBnQ=='),
 (50, 'AQAAAAIAAYagAAAAEDDbisi36HRl//QmqN72GcIrK9+TDcwm7TUzsfwR8KXQ3K6GSsVgvRdnJqm9QqALCw==');
 INSERT dbo.users(full_name,email,phone,password_hash,role,status,created_at,updated_at,session_version)
 SELECT N'Khách demo doanh thu '+RIGHT('00'+CONVERT(varchar(2),n),2),
 'rgrev1-'+RIGHT('00'+CONVERT(varchar(2),n),2)+'@example.invalid',
 '000000'+RIGHT('0000'+CONVERT(varchar(4),n),4),hash,'CUSTOMER','ACTIVE',@first,@first,NEWID()
 FROM @credentials;
 DECLARE @i int=1,@customer bigint,@vehicle bigint,@price decimal(10,2),@days int,@start datetime2(7),@finish datetime2(7),@created datetime2(7),@review datetime2(7),@paid datetime2(7),@updated datetime2(7),@status varchar(30),@detail varchar(30),@method varchar(30),@code varchar(30),@order bigint,@amount decimal(10,2),@payment bigint,@attempt int;
 WHILE @i<=500
 BEGIN
   SET @code='RGREV1-'+RIGHT('0000'+CONVERT(varchar(4),@i),4);
   SELECT @customer=id FROM dbo.users WHERE email='rgrev1-'+RIGHT('00'+CONVERT(varchar(2),1+((@i-1)%50)),2)+'@example.invalid';
   SET @status=CASE WHEN @i<=350 THEN 'COMPLETED' WHEN @i<=370 THEN 'RENTING' WHEN @i<=400 THEN 'PAID' WHEN @i<=435 THEN 'CONFIRMED' WHEN @i<=475 THEN 'PENDING' ELSE 'CANCELLED' END;
   SET @days=1+(@i*13%7);
   IF @status='COMPLETED'
   BEGIN
     -- Spread history across the full interval; varied time of day and season.
     SET @start=DATEADD(hour,8+(@i*7%13),DATEADD(day,3+((@i*197+(@i%11)*17)%@historyDays),@first));
   END
   ELSE IF @status='RENTING'
   BEGIN
     SET @start=DATEADD(hour,10,DATEADD(day,-1,@today)); SET @days=3+(@i%4);
   END
   ELSE IF @status='CANCELLED'
     SET @start=DATEADD(hour,9,DATEADD(day,3+(@i%20),@today));
   ELSE
     SET @start=DATEADD(hour,9+(@i%8),DATEADD(day,8+((@i-371)*3%45),@today));
   SET @finish=DATEADD(day,@days,@start);
   SET @vehicle=NULL; SET @attempt=0;
   WHILE @vehicle IS NULL AND @attempt<90
   BEGIN
     SELECT TOP(1) @vehicle=f.id,@price=f.price FROM @fleet f
     WHERE NOT EXISTS (
       SELECT 1 FROM dbo.rental_order_details d JOIN dbo.rental_orders o ON o.id=d.rental_order_id
       WHERE d.vehicle_id=f.id AND o.status<>'CANCELLED' AND d.status<>'CANCELLED'
         AND ((o.start_datetime<@finish AND o.end_datetime>@start)
           OR (@status='RENTING' AND d.status='RENTING')))
     ORDER BY CHECKSUM(@i,f.id),f.id;
     IF @vehicle IS NULL
     BEGIN
       IF @status='RENTING' BREAK; -- never shift current rentals out of current day
       SET @start=DATEADD(day,1,@start); SET @finish=DATEADD(day,@days,@start);
       IF @status='COMPLETED' AND @finish>=@today BREAK;
     END;
     SET @attempt+=1;
   END;
   IF @vehicle IS NULL THROW 51006, 'No conflict-free vehicle slot; entire seed rolled back.', 1;
   SET @amount=@price*@days;
   SET @method=CASE WHEN @i%10 IN (0,1,2) THEN 'CASH' ELSE 'BANK_TRANSFER' END;
   SET @paid=NULL; SET @review=NULL;
   IF @status='COMPLETED'
   BEGIN
     SET @paid=DATEADD(hour,-(2+@i%20),@start); SET @created=DATEADD(day,-1,@paid); SET @review=DATEADD(hour,-1,@paid); SET @updated=@finish;
     IF @finish>=@now THROW 51007, 'Historical rental would finish in future.', 1;
   END
   ELSE IF @status='RENTING'
   BEGIN
     SET @paid=DATEADD(hour,-2,@start); SET @created=DATEADD(day,-1,@paid); SET @review=DATEADD(hour,-1,@paid); SET @updated=@start;
   END
   ELSE IF @status='PAID'
   BEGIN
     SET @paid=DATEADD(minute,-(@i%15)*11,@now);
     -- Explicit midnight boundary samples on adjacent Vietnam calendar days.
     IF @i=371 SET @paid=DATEADD(minute,-1,@today);
     IF @i=372 SET @paid=@today;
     IF @i BETWEEN 373 AND 380 SET @paid=DATEADD(day,-(@i-372),@now);
     SET @created=DATEADD(day,-1,@paid); SET @review=DATEADD(hour,-1,@paid); SET @updated=@paid;
   END
   ELSE
   BEGIN
     SET @created=DATEADD(day,-1,@now); SET @updated=@created;
     IF @status IN ('CONFIRMED','CANCELLED') BEGIN SET @review=DATEADD(hour,1,@created); SET @updated=@review; END;
   END;
   SET @detail=CASE @status WHEN 'COMPLETED' THEN 'RETURNED' WHEN 'RENTING' THEN 'RENTING' WHEN 'CANCELLED' THEN 'CANCELLED' ELSE 'RESERVED' END;
   INSERT dbo.rental_orders(order_code,customer_id,start_datetime,end_datetime,total_amount,status,note,created_at,updated_at,reviewed_by_user_id,reviewed_at,rejection_reason)
   VALUES(@code,@customer,@start,@finish,@amount,@status,N'RENTGO_REVENUE_DEMO_V1',@created,@updated,CASE WHEN @review IS NOT NULL THEN @admin END,@review,CASE WHEN @status='CANCELLED' AND @i>485 THEN N'Đơn demo bị từ chối.' END);
   SET @order=CONVERT(bigint,SCOPE_IDENTITY());
   INSERT dbo.rental_order_details(rental_order_id,vehicle_id,price_per_day,rental_days,subtotal,status,created_at)
   VALUES(@order,@vehicle,@price,@days,@amount,@detail,@created);
   IF @paid IS NOT NULL
   BEGIN
     INSERT dbo.payments(rental_order_id,amount,payment_method,transaction_code,status,paid_at,created_at,confirmed_by_user_id,external_reference)
     VALUES(@order,@amount,@method,@code+'-PAID','PAID',@paid,@paid,@admin,NULL);
   END;
   IF @i BETWEEN 401 AND 430
   BEGIN
     -- Only confirmed, unpaid orders can have pending transfer requests.
     INSERT dbo.TransferRequests(OrderId,CustomerId,SubmissionKey,Status,SubmittedAt,ReviewedAt,ReviewedByUserId,PaymentId,Reason,PaymentMethod)
     VALUES(@order,@customer,CONVERT(uniqueidentifier,'72657631-0000-4000-8000-'+RIGHT('000000000000'+CONVERT(varchar(12),@i),12)),N'PENDING',DATEADD(minute,10,@review),NULL,NULL,NULL,NULL,@method);
   END;
   -- Unpaid intents are not revenue; sample pending rows on five confirmed orders.
   IF @i BETWEEN 431 AND 435
     INSERT dbo.payments(rental_order_id,amount,payment_method,transaction_code,status,paid_at,created_at,confirmed_by_user_id,external_reference)
     VALUES(@order,@amount,@method,@code+'-PENDING','PENDING',NULL,DATEADD(minute,10,@review),NULL,NULL);
   IF @i BETWEEN 476 AND 485
   BEGIN
     -- Confirmed then cancelled, so prior payment intent becomes FAILED.
     UPDATE dbo.rental_orders SET updated_at=DATEADD(minute,30,@review) WHERE id=@order;
     INSERT dbo.payments(rental_order_id,amount,payment_method,transaction_code,status,paid_at,created_at,confirmed_by_user_id,external_reference)
     VALUES(@order,@amount,@method,@code+'-FAILED','FAILED',NULL,DATEADD(minute,10,@review),NULL,NULL);
   END;
   SET @i+=1;
 END;
 IF (SELECT COUNT(*) FROM dbo.rental_orders WHERE order_code LIKE 'RGREV1-%')<>500
    OR (SELECT COUNT(*) FROM dbo.payments WHERE transaction_code LIKE 'RGREV1-%' AND status='PAID')<>400
    THROW 51008, 'Unexpected seed totals.', 1;
 COMMIT;
 PRINT 'Inserted 50 customers, 500 orders/details, 400 PAID payments, 5 pending payments, 10 failed payments and 30 pending requests.';
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK;
 THROW;
END CATCH;
GO
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
GO

-- ===== SECTION 2: READ-ONLY VERIFICATION =====
-- Read-only verification. Amounts are VND, timestamps are grouped in Vietnam time.
USE [CarMotorbikeRentalDB];
GO
SET NOCOUNT ON;
SELECT status, COUNT(*) AS seed_orders FROM dbo.rental_orders
WHERE order_code LIKE 'RGREV1-%' AND note=N'RENTGO_REVENUE_DEMO_V1' GROUP BY status ORDER BY status;
SELECT status,payment_method,COUNT(*) AS seed_payments,SUM(amount) AS amount_vnd FROM dbo.payments
WHERE transaction_code LIKE 'RGREV1-%' GROUP BY status,payment_method;
SELECT t.Status,COUNT(*) AS seed_requests FROM dbo.TransferRequests t JOIN dbo.rental_orders o ON o.id=t.OrderId
WHERE o.order_code LIKE 'RGREV1-%' GROUP BY t.Status;
SELECT COUNT(*) AS seed_customers FROM dbo.users WHERE email LIKE 'rgrev1-%@example.invalid';
-- Compare full database totals against the ADMIN API (includes pre-existing data).
SELECT YEAR(DATEADD(hour,7,paid_at)) AS [year],MONTH(DATEADD(hour,7,paid_at)) AS [month],
 SUM(amount) AS revenue_vnd,COUNT(*) AS successful_payments,COUNT(DISTINCT rental_order_id) AS paid_orders,
 SUM(CASE WHEN payment_method='CASH' THEN amount ELSE 0 END) AS cash_vnd,
 SUM(CASE WHEN payment_method='BANK_TRANSFER' THEN amount ELSE 0 END) AS bank_transfer_vnd
FROM dbo.payments WHERE status='PAID' AND paid_at IS NOT NULL
GROUP BY YEAR(DATEADD(hour,7,paid_at)),MONTH(DATEADD(hour,7,paid_at)) ORDER BY [year],[month];
SELECT YEAR(DATEADD(hour,7,paid_at)) AS [year],SUM(amount) AS revenue_vnd,COUNT(DISTINCT rental_order_id) AS paid_orders
FROM dbo.payments WHERE status='PAID' AND paid_at IS NOT NULL GROUP BY YEAR(DATEADD(hour,7,paid_at)) ORDER BY [year];
DECLARE @from date=CONVERT(date,DATEADD(hour,7,SYSUTCDATETIME())),@to date=CONVERT(date,DATEADD(hour,7,SYSUTCDATETIME()));
-- Edit @from/@to to verify a selected day or custom interval (end date inclusive).
DECLARE @start datetime2=DATEADD(hour,-7,CONVERT(datetime2,@from)),@end datetime2=DATEADD(hour,-7,DATEADD(day,1,CONVERT(datetime2,@to)));
SELECT @from AS from_vietnam,@to AS to_vietnam,COALESCE(SUM(amount),0) AS revenue_vnd,COUNT(*) AS successful_payments,COUNT(DISTINCT rental_order_id) AS paid_orders,
 COALESCE(SUM(CASE WHEN payment_method='CASH' THEN amount ELSE 0 END),0) AS cash_vnd,
 COALESCE(SUM(CASE WHEN payment_method='BANK_TRANSFER' THEN amount ELSE 0 END),0) AS bank_transfer_vnd
FROM dbo.payments WHERE status='PAID' AND paid_at>=@start AND paid_at<@end;
SELECT CONVERT(date,DATEADD(hour,7,paid_at)) AS vietnam_day,SUM(amount) AS revenue_vnd FROM dbo.payments
WHERE status='PAID' AND paid_at>=@start AND paid_at<@end GROUP BY CONVERT(date,DATEADD(hour,7,paid_at)) ORDER BY vietnam_day;
-- Any rows returned below require investigation; empty result is expected for an untouched seed.
SELECT o.order_code,o.status,o.total_amount,p.amount,p.paid_at FROM dbo.rental_orders o LEFT JOIN dbo.payments p ON p.rental_order_id=o.id AND p.status='PAID'
WHERE o.order_code LIKE 'RGREV1-%' AND ((o.status IN ('PAID','RENTING','COMPLETED') AND (p.id IS NULL OR p.amount<>o.total_amount OR p.paid_at IS NULL OR p.paid_at>SYSUTCDATETIME())) OR (o.status IN ('PENDING','CONFIRMED','CANCELLED') AND p.id IS NOT NULL));
SELECT a.order_code AS seed_order,b.order_code AS overlapping_order,d.vehicle_id
FROM dbo.rental_orders a JOIN dbo.rental_order_details d ON d.rental_order_id=a.id
JOIN dbo.rental_order_details e ON e.vehicle_id=d.vehicle_id AND e.rental_order_id<>a.id
JOIN dbo.rental_orders b ON b.id=e.rental_order_id
WHERE a.order_code LIKE 'RGREV1-%' AND a.status<>'CANCELLED' AND b.status<>'CANCELLED' AND d.status<>'CANCELLED' AND e.status<>'CANCELLED' AND a.start_datetime<b.end_datetime AND a.end_datetime>b.start_datetime;
GO

-- ===== SECTION 3: OPTIONAL CLEANUP (DISABLED) =====
-- -- OPTIONAL, DESTRUCTIVE ONLY FOR DEMO ROWS. Back up DB; stop application.
-- -- Change @confirm to 1 only if you want to remove the entire revenue seed.
-- USE [CarMotorbikeRentalDB];
-- GO
-- SET NOCOUNT ON;
-- SET XACT_ABORT ON;
-- SET ANSI_NULLS ON;
-- SET QUOTED_IDENTIFIER ON;
-- SET ANSI_PADDING ON;
-- SET ANSI_WARNINGS ON;
-- SET CONCAT_NULL_YIELDS_NULL ON;
-- SET ARITHABORT ON;
-- SET NUMERIC_ROUNDABORT OFF;
-- SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
-- DECLARE @confirm bit=0;
-- IF @confirm<>1 BEGIN PRINT 'Cleanup disabled. Set @confirm=1 to delete demo data.'; RETURN; END;
-- BEGIN TRY
--  BEGIN TRAN;
--  DECLARE @lock int;
--  EXEC @lock=sys.sp_getapplock @Resource=N'RentGo.RevenueSeed.V1',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
--  IF @lock<0 THROW 51100,'Could not acquire seed lock.',1;
--  DECLARE @users TABLE(id bigint PRIMARY KEY);
--  DECLARE @orders TABLE(id bigint PRIMARY KEY);
--  INSERT @users SELECT id FROM dbo.users WHERE email LIKE 'rgrev1-%@example.invalid' AND full_name LIKE N'Khách demo doanh thu %' AND role='CUSTOMER' AND phone LIKE '000000%';
--  INSERT @orders SELECT o.id FROM dbo.rental_orders o JOIN @users u ON u.id=o.customer_id WHERE o.order_code LIKE 'RGREV1-%' AND o.note=N'RENTGO_REVENUE_DEMO_V1';
--  IF EXISTS (SELECT 1 FROM dbo.rental_orders o JOIN @users u ON u.id=o.customer_id WHERE NOT EXISTS(SELECT 1 FROM @orders s WHERE s.id=o.id))
--  OR EXISTS (SELECT 1 FROM @orders s CROSS APPLY (SELECT COUNT(*) AS n FROM dbo.rental_order_details d WHERE d.rental_order_id=s.id) q WHERE q.n<>1)
--  OR EXISTS (SELECT 1 FROM dbo.payments p JOIN @orders s ON s.id=p.rental_order_id WHERE p.transaction_code NOT LIKE 'RGREV1-%')
--  OR EXISTS (SELECT 1 FROM dbo.TransferRequests t JOIN @orders s ON s.id=t.OrderId WHERE CONVERT(varchar(36),t.SubmissionKey) NOT LIKE '72657631-0000-4000-8000-%')
--  THROW 51101,'Non-seed activity detected. Cleanup refused; inspect manually.',1;
--  DELETE t FROM dbo.TransferRequests t JOIN @orders s ON s.id=t.OrderId;
--  DELETE p FROM dbo.payments p JOIN @orders s ON s.id=p.rental_order_id WHERE p.transaction_code LIKE 'RGREV1-%';
--  DELETE d FROM dbo.rental_order_details d JOIN @orders s ON s.id=d.rental_order_id;
--  DELETE o FROM dbo.rental_orders o JOIN @orders s ON s.id=o.id;
--  DELETE u FROM dbo.users u JOIN @users s ON s.id=u.id;
--  COMMIT;
--  PRINT 'Revenue demo rows removed; no vehicle rows changed.';
-- END TRY
-- BEGIN CATCH
--  IF XACT_STATE()<>0 ROLLBACK;
--  THROW;
-- END CATCH;
-- GO
-- SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
-- GO
