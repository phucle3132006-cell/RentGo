USE [CarMotorbikeRentalDB];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
DECLARE @Seed TABLE(category varchar(30), brand varchar(50), model varchar(100), [year] int, seats int, cc int NULL, price decimal(10,2), plate varchar(20) PRIMARY KEY, color nvarchar(30), status varchar(30), img1 varchar(255), img2 varchar(255));
INSERT INTO @Seed VALUES
(N'XE_SO', N'Honda', N'Wave Alpha', 2024, 2, 110, 100000, N'59-C1 10001', N'Đỏ', N'AVAILABLE', N'honda_wave_alpha_red_01.jpg', N'honda_wave_alpha_red_02.jpg'),
(N'XE_SO', N'Honda', N'Wave Alpha', 2024, 2, 110, 100000, N'59-C1 98002', N'Xanh dương', N'AVAILABLE', N'honda_wave_alpha_blue_01.jpg', N'honda_wave_alpha_blue_02.jpg'),
(N'XE_SO', N'Honda', N'Future', 2024, 2, 125, 120000, N'59-C1 98003', N'Đen', N'AVAILABLE', N'honda_future_black_01.jpg', N'honda_future_black_02.jpg'),
(N'XE_SO', N'Honda', N'Future', 2024, 2, 125, 120000, N'59-C1 98004', N'Trắng', N'AVAILABLE', N'honda_future_white_01.jpg', N'honda_future_white_02.jpg'),
(N'XE_SO', N'Honda', N'Blade', 2024, 2, 110, 100000, N'59-C1 98005', N'Đỏ', N'AVAILABLE', N'honda_blade_red_01.jpg', N'honda_blade_red_02.jpg'),
(N'XE_SO', N'Honda', N'Blade', 2024, 2, 110, 100000, N'59-C1 98006', N'Đen', N'AVAILABLE', N'honda_blade_black_01.jpg', N'honda_blade_black_02.jpg'),
(N'XE_SO', N'Yamaha', N'Sirius', 2024, 2, 110, 100000, N'59-C1 98007', N'Đỏ', N'AVAILABLE', N'yamaha_sirius_red_01.jpg', N'yamaha_sirius_red_02.jpg'),
(N'XE_SO', N'Yamaha', N'Sirius', 2024, 2, 110, 100000, N'59-C1 98008', N'Xanh dương', N'AVAILABLE', N'yamaha_sirius_blue_01.jpg', N'yamaha_sirius_blue_02.jpg'),
(N'XE_SO', N'Yamaha', N'Jupiter', 2024, 2, 115, 120000, N'59-C1 98009', N'Đỏ', N'AVAILABLE', N'yamaha_jupiter_red_01.jpg', N'yamaha_jupiter_red_02.jpg'),
(N'XE_SO', N'Yamaha', N'Jupiter', 2024, 2, 115, 120000, N'59-C1 98010', N'Đen', N'MAINTENANCE', N'yamaha_jupiter_black_01.jpg', N'yamaha_jupiter_black_02.jpg'),
(N'XE_GA', N'Honda', N'Vision', 2024, 2, 110, 150000, N'59-C1 98011', N'Trắng', N'AVAILABLE', N'honda_vision_white_01.jpg', N'honda_vision_white_02.jpg'),
(N'XE_GA', N'Honda', N'Vision', 2024, 2, 110, 150000, N'59-C1 98012', N'Đen', N'AVAILABLE', N'honda_vision_black_01.jpg', N'honda_vision_black_02.jpg'),
(N'XE_GA', N'Honda', N'Air Blade', 2024, 2, 125, 180000, N'59-C1 98013', N'Đỏ', N'AVAILABLE', N'honda_air_blade_red_01.jpg', N'honda_air_blade_red_02.jpg'),
(N'XE_GA', N'Honda', N'Air Blade', 2024, 2, 125, 180000, N'59-C1 98014', N'Đen', N'AVAILABLE', N'honda_air_blade_black_01.jpg', N'honda_air_blade_black_02.jpg'),
(N'XE_GA', N'Honda', N'Lead', 2024, 2, 125, 170000, N'59-C1 98015', N'Trắng', N'AVAILABLE', N'honda_lead_white_01.jpg', N'honda_lead_white_02.jpg'),
(N'XE_GA', N'Honda', N'Lead', 2024, 2, 125, 170000, N'59-C1 98016', N'Đỏ', N'AVAILABLE', N'honda_lead_red_01.jpg', N'honda_lead_red_02.jpg'),
(N'XE_GA', N'Honda', N'SH Mode', 2024, 2, 125, 250000, N'59-C1 98017', N'Trắng', N'AVAILABLE', N'honda_sh_mode_white_01.jpg', N'honda_sh_mode_white_02.jpg'),
(N'XE_GA', N'Honda', N'SH Mode', 2024, 2, 125, 250000, N'59-C1 98018', N'Đen', N'AVAILABLE', N'honda_sh_mode_black_01.jpg', N'honda_sh_mode_black_02.jpg'),
(N'XE_GA', N'Yamaha', N'Grande', 2024, 2, 125, 180000, N'59-C1 98019', N'Trắng', N'AVAILABLE', N'yamaha_grande_white_01.jpg', N'yamaha_grande_white_02.jpg'),
(N'XE_GA', N'Yamaha', N'Grande', 2024, 2, 125, 180000, N'59-C1 98020', N'Xanh dương', N'MAINTENANCE', N'yamaha_grande_blue_01.jpg', N'yamaha_grande_blue_02.jpg'),
(N'XE_DIEN', N'VinFast', N'Evo200', 2024, 2, NULL, 120000, N'59-C1 98021', N'Trắng', N'AVAILABLE', N'vinfast_evo200_white_01.jpg', N'vinfast_evo200_white_02.jpg'),
(N'XE_DIEN', N'VinFast', N'Evo200', 2024, 2, NULL, 120000, N'59-C1 98022', N'Đỏ', N'AVAILABLE', N'vinfast_evo200_red_01.jpg', N'vinfast_evo200_red_02.jpg'),
(N'XE_DIEN', N'VinFast', N'Feliz S', 2024, 2, NULL, 150000, N'59-C1 98023', N'Trắng', N'AVAILABLE', N'vinfast_feliz_s_white_01.jpg', N'vinfast_feliz_s_white_02.jpg'),
(N'XE_DIEN', N'VinFast', N'Feliz S', 2024, 2, NULL, 150000, N'59-C1 98024', N'Đen', N'AVAILABLE', N'vinfast_feliz_s_black_01.jpg', N'vinfast_feliz_s_black_02.jpg'),
(N'XE_DIEN', N'VinFast', N'Klara S', 2024, 2, NULL, 160000, N'59-C1 98025', N'Trắng', N'AVAILABLE', N'vinfast_klara_s_white_01.jpg', N'vinfast_klara_s_white_02.jpg'),
(N'XE_DIEN', N'VinFast', N'Klara S', 2024, 2, NULL, 160000, N'59-C1 98026', N'Đỏ', N'AVAILABLE', N'vinfast_klara_s_red_01.jpg', N'vinfast_klara_s_red_02.jpg'),
(N'XE_DIEN', N'VinFast', N'Vento S', 2024, 2, NULL, 200000, N'59-C1 98027', N'Trắng', N'AVAILABLE', N'vinfast_vento_s_white_01.jpg', N'vinfast_vento_s_white_02.jpg'),
(N'XE_DIEN', N'VinFast', N'Vento S', 2024, 2, NULL, 200000, N'59-C1 98028', N'Đen', N'AVAILABLE', N'vinfast_vento_s_black_01.jpg', N'vinfast_vento_s_black_02.jpg'),
(N'XE_DIEN', N'VinFast', N'Theon S', 2024, 2, NULL, 250000, N'59-C1 98029', N'Trắng', N'AVAILABLE', N'vinfast_theon_s_white_01.jpg', N'vinfast_theon_s_white_02.jpg'),
(N'XE_DIEN', N'VinFast', N'Theon S', 2024, 2, NULL, 250000, N'59-C1 98030', N'Đen', N'MAINTENANCE', N'vinfast_theon_s_black_01.jpg', N'vinfast_theon_s_black_02.jpg'),
(N'OTO', N'Toyota', N'Vios', 2024, 5, 1500, 600000, N'51A-98031', N'Trắng', N'AVAILABLE', N'toyota_vios_white_01.jpg', N'toyota_vios_white_02.jpg'),
(N'OTO', N'Toyota', N'Vios', 2024, 5, 1500, 600000, N'51A-98032', N'Đen', N'AVAILABLE', N'toyota_vios_black_01.jpg', N'toyota_vios_black_02.jpg'),
(N'OTO', N'Hyundai', N'Accent', 2024, 5, 1500, 650000, N'51A-98033', N'Trắng', N'AVAILABLE', N'hyundai_accent_white_01.jpg', N'hyundai_accent_white_02.jpg'),
(N'OTO', N'Hyundai', N'Accent', 2024, 5, 1500, 650000, N'51A-98034', N'Đỏ', N'AVAILABLE', N'hyundai_accent_red_01.jpg', N'hyundai_accent_red_02.jpg'),
(N'OTO', N'Mitsubishi', N'Xpander', 2024, 7, 1500, 900000, N'51A-98035', N'Trắng', N'AVAILABLE', N'mitsubishi_xpander_white_01.jpg', N'mitsubishi_xpander_white_02.jpg'),
(N'OTO', N'Mitsubishi', N'Xpander', 2024, 7, 1500, 900000, N'51A-98036', N'Đen', N'AVAILABLE', N'mitsubishi_xpander_black_01.jpg', N'mitsubishi_xpander_black_02.jpg'),
(N'OTO', N'Kia', N'Morning', 2024, 5, 1250, 500000, N'51A-98037', N'Trắng', N'AVAILABLE', N'kia_morning_white_01.jpg', N'kia_morning_white_02.jpg'),
(N'OTO', N'Kia', N'Morning', 2024, 5, 1250, 500000, N'51A-98038', N'Đỏ', N'AVAILABLE', N'kia_morning_red_01.jpg', N'kia_morning_red_02.jpg'),
(N'OTO', N'Toyota', N'Fortuner', 2024, 7, 2400, 1300000, N'51A-98039', N'Trắng', N'AVAILABLE', N'toyota_fortuner_white_01.jpg', N'toyota_fortuner_white_02.jpg'),
(N'OTO', N'Toyota', N'Fortuner', 2024, 7, 2400, 1300000, N'51A-98040', N'Đen', N'MAINTENANCE', N'toyota_fortuner_black_01.jpg', N'toyota_fortuner_black_02.jpg');
BEGIN TRY
 BEGIN TRANSACTION;
 INSERT dbo.vehicle_models(category,brand,model,[year],seats,engine_capacity,description,price_per_day,created_at,updated_at)
 SELECT DISTINCT s.category,s.brand,s.model,s.year,s.seats,s.cc,N'Xe cho thuê - dữ liệu minh họa RentGo.',s.price,SYSDATETIME(),SYSDATETIME()
 FROM @Seed s WHERE NOT EXISTS (SELECT 1 FROM dbo.vehicle_models m WITH (UPDLOCK,HOLDLOCK) WHERE m.brand=s.brand AND m.model=s.model AND m.year=s.year);
 IF EXISTS (SELECT 1 FROM @Seed s JOIN dbo.vehicle_models m ON m.brand=s.brand AND m.model=s.model AND m.year=s.year WHERE m.category<>s.category)
  THROW 50001, N'Model trùng tên nhưng khác loại xe.', 1;
 IF EXISTS (SELECT 1 FROM @Seed s JOIN dbo.vehicles v ON v.license_plate=s.plate JOIN dbo.vehicle_models m ON m.brand=s.brand AND m.model=s.model AND m.year=s.year WHERE v.vehicle_model_id<>m.id)
  THROW 50002, N'Biển số seed đã thuộc model khác. Kiểm tra trước khi chạy lại.', 1;
 INSERT dbo.vehicles(vehicle_model_id,license_plate,color,[condition],status,created_at,updated_at)
 SELECT m.id,s.plate,s.color,CASE WHEN s.status='MAINTENANCE' THEN N'Đang bảo trì' ELSE N'Hoạt động tốt' END,s.status,SYSDATETIME(),SYSDATETIME()
 FROM @Seed s JOIN dbo.vehicle_models m ON m.brand=s.brand AND m.model=s.model AND m.year=s.year
 WHERE NOT EXISTS (SELECT 1 FROM dbo.vehicles v WITH (UPDLOCK,HOLDLOCK) WHERE v.license_plate=s.plate);
 INSERT dbo.vehicle_images(vehicle_id,image_url,is_primary,created_at)
 SELECT v.id,'/uploads/vehicles/'+s.img1,CASE WHEN EXISTS (SELECT 1 FROM dbo.vehicle_images i WHERE i.vehicle_id=v.id AND i.is_primary=1) THEN 0 ELSE 1 END,SYSDATETIME()
 FROM @Seed s JOIN dbo.vehicles v ON v.license_plate=s.plate
 WHERE NOT EXISTS (SELECT 1 FROM dbo.vehicle_images i WHERE i.vehicle_id=v.id AND i.image_url='/uploads/vehicles/'+s.img1);
 INSERT dbo.vehicle_images(vehicle_id,image_url,is_primary,created_at)
 SELECT v.id,'/uploads/vehicles/'+s.img2,0,SYSDATETIME()
 FROM @Seed s JOIN dbo.vehicles v ON v.license_plate=s.plate
 WHERE NOT EXISTS (SELECT 1 FROM dbo.vehicle_images i WHERE i.vehicle_id=v.id AND i.image_url='/uploads/vehicles/'+s.img2);
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
SELECT s.brand,s.model,s.color,s.plate,v.id AS vehicle_id,v.status,
       (SELECT COUNT(*) FROM dbo.vehicle_images i WHERE i.vehicle_id=v.id AND i.image_url IN ('/uploads/vehicles/'+s.img1,'/uploads/vehicles/'+s.img2)) AS expected_images
FROM @Seed s LEFT JOIN dbo.vehicles v ON v.license_plate=s.plate
ORDER BY s.category,s.brand,s.model,s.color;
SELECT COUNT(DISTINCT m.id) AS seed_models,COUNT(DISTINCT v.id) AS seed_vehicles,COUNT(i.id) AS seed_images
FROM @Seed s JOIN dbo.vehicle_models m ON m.brand=s.brand AND m.model=s.model AND m.year=s.year
JOIN dbo.vehicles v ON v.vehicle_model_id=m.id AND v.license_plate=s.plate
LEFT JOIN dbo.vehicle_images i ON i.vehicle_id=v.id AND i.image_url IN ('/uploads/vehicles/'+s.img1,'/uploads/vehicles/'+s.img2);
