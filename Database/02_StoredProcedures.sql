-- ============================================================
-- SmartCafe Pro - Stored Procedures
-- ============================================================
USE SmartCafeProDB;
GO
USE SmartCafeProDB;
GO

DECLARE @ConstraintName NVARCHAR(200);

SELECT @ConstraintName = kc.name
FROM sys.key_constraints kc
WHERE kc.parent_object_id = OBJECT_ID('Products')
AND kc.type = 'UQ';

IF @ConstraintName IS NOT NULL
BEGIN
    EXEC('ALTER TABLE Products DROP CONSTRAINT ' + @ConstraintName);
END
GO

IF NOT EXISTS (
    SELECT 1 
    FROM sys.indexes 
    WHERE name = 'UX_Products_Barcode_NotNull'
)
BEGIN
    CREATE UNIQUE INDEX UX_Products_Barcode_NotNull
    ON Products(Barcode)
    WHERE Barcode IS NOT NULL;
END
GO

-- ============================================================
-- SP: User Login (secure, returns role info)
-- ============================================================
CREATE OR ALTER PROCEDURE sp_UserLogin
    @Username   NVARCHAR(50),
    @ResultCode INT OUTPUT,
    @Message    NVARCHAR(200) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        DECLARE @UserID INT, @IsActive BIT, @RoleID INT;

        SELECT @UserID = UserID, @IsActive = IsActive, @RoleID = RoleID
        FROM Users
        WHERE Username = @Username;

        IF @UserID IS NULL
        BEGIN
            SET @ResultCode = -1; SET @Message = 'User not found.'; RETURN;
        END
        IF @IsActive = 0
        BEGIN
            SET @ResultCode = -2; SET @Message = 'Account is disabled.'; RETURN;
        END

        -- Update last login
        UPDATE Users SET LastLogin = GETDATE() WHERE UserID = @UserID;

        -- Log the login
        INSERT INTO AuditLogs(TableName, Operation, RecordID, UserID)
        VALUES ('Users', 'LOGIN', @UserID, @UserID);

        SET @ResultCode = 1; SET @Message = 'Success';

        -- Return user details
        SELECT u.UserID, u.Username, u.PasswordHash, u.PasswordSalt,
               r.RoleName, u.Email, u.IsActive
        FROM Users u
        INNER JOIN Roles r ON u.RoleID = r.RoleID
        WHERE u.UserID = @UserID;
    END TRY
    BEGIN CATCH
        SET @ResultCode = -99;
        SET @Message = ERROR_MESSAGE();
    END CATCH
END
GO

-- ============================================================
-- SP: Add / Update Customer
-- ============================================================
CREATE OR ALTER PROCEDURE sp_UpsertCustomer
    @CustomerID   INT = NULL,
    @FirstName    NVARCHAR(50),
    @LastName     NVARCHAR(50),
    @Phone        NVARCHAR(20) = NULL,
    @Email        NVARCHAR(100) = NULL,
    @Address      NVARCHAR(300) = NULL,
    @DateOfBirth  DATE = NULL,
    @Notes        NVARCHAR(500) = NULL,
    @NewID        INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;
        IF @CustomerID IS NULL OR @CustomerID = 0
        BEGIN
            INSERT INTO Customers (FirstName, LastName, Phone, Email, Address, DateOfBirth, Notes)
            VALUES (@FirstName, @LastName, @Phone, @Email, @Address, @DateOfBirth, @Notes);
            SET @NewID = SCOPE_IDENTITY();
        END
        ELSE
        BEGIN
            UPDATE Customers SET
                FirstName = @FirstName, LastName = @LastName,
                Phone = @Phone, Email = @Email, Address = @Address,
                DateOfBirth = @DateOfBirth, Notes = @Notes,
                UpdatedAt = GETDATE()
            WHERE CustomerID = @CustomerID;
            SET @NewID = @CustomerID;
        END
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        SET @NewID = -1;
        THROW;
    END CATCH
END
GO

-- ============================================================
-- SP: Place Order (full transaction)
-- ============================================================
CREATE OR ALTER PROCEDURE sp_PlaceOrder
    @CustomerID  INT = NULL,
    @TableID     INT = NULL,
    @EmployeeID  INT = NULL,
    @OrderType   NVARCHAR(20) = 'Dine-In',
    @DiscountAmt DECIMAL(10,2) = 0,
    @TaxRate     DECIMAL(5,2) = 0,
    @Notes       NVARCHAR(500) = NULL,
    @OrderItems  NVARCHAR(MAX),   -- JSON: [{ProductID,Quantity,UnitPrice,Discount,Notes}]
    @NewOrderID  INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        -- Generate order number
        DECLARE @OrderNum NVARCHAR(20) = 'ORD-' + FORMAT(GETDATE(),'yyyyMMdd') +
                                          '-' + RIGHT('0000' + CAST(NEXT VALUE FOR sq_OrderNumber AS NVARCHAR), 4);
        DECLARE @SubTotal DECIMAL(12,2) = 0;

        INSERT INTO Orders (OrderNumber, CustomerID, TableID, EmployeeID, OrderType,
                            DiscountAmt, Notes)
        VALUES (@OrderNum, @CustomerID, @TableID, @EmployeeID, @OrderType,
                @DiscountAmt, @Notes);

        SET @NewOrderID = SCOPE_IDENTITY();

        -- Insert order details from JSON
        INSERT INTO OrderDetails (OrderID, ProductID, Quantity, UnitPrice, Discount, Notes)
        SELECT @NewOrderID,
               JSON_VALUE(item.value,'$.ProductID'),
               JSON_VALUE(item.value,'$.Quantity'),
               JSON_VALUE(item.value,'$.UnitPrice'),
               ISNULL(JSON_VALUE(item.value,'$.Discount'), 0),
               JSON_VALUE(item.value,'$.Notes')
        FROM OPENJSON(@OrderItems) item;

        -- Calculate subtotal
        SELECT @SubTotal = SUM(LineTotal) FROM OrderDetails WHERE OrderID = @NewOrderID;

        DECLARE @Tax DECIMAL(10,2) = (@SubTotal - @DiscountAmt) * @TaxRate / 100;
        DECLARE @Total DECIMAL(12,2) = @SubTotal - @DiscountAmt + @Tax;

        UPDATE Orders SET SubTotal = @SubTotal, TaxAmt = @Tax, TotalAmount = @Total
        WHERE OrderID = @NewOrderID;

        -- Update table status
        IF @TableID IS NOT NULL
            UPDATE CafeTables SET Status = 'Occupied' WHERE TableID = @TableID;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        SET @NewOrderID = -1;
        THROW;
    END CATCH
END
GO

-- ============================================================
-- Sequence for Order Numbers
-- ============================================================
CREATE SEQUENCE sq_OrderNumber START WITH 1 INCREMENT BY 1;
GO

-- ============================================================
-- SP: Complete Order & Process Payment
-- ============================================================
CREATE OR ALTER PROCEDURE sp_CompleteOrder
    @OrderID       INT,
    @PaymentMethod NVARCHAR(30),
    @AmountPaid    DECIMAL(12,2),
    @Reference     NVARCHAR(100) = NULL,
    @ProcessedBy   INT = NULL,
    @UsePoints     INT = 0,    -- loyalty points to redeem
    @Success       BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @TotalAmt DECIMAL(12,2), @CustomerID INT;
        SELECT @TotalAmt = TotalAmount, @CustomerID = CustomerID
        FROM Orders WHERE OrderID = @OrderID;

        -- Insert payment record
        INSERT INTO Payments (OrderID, PaymentMethod, Amount, Reference, ProcessedBy)
        VALUES (@OrderID, @PaymentMethod, @AmountPaid, @Reference, @ProcessedBy);

        -- Update order status
        UPDATE Orders SET Status = 'Completed', CompletedAt = GETDATE(), UpdatedAt = GETDATE()
        WHERE OrderID = @OrderID;

        -- Free the table
        DECLARE @TableID INT;
        SELECT @TableID = TableID FROM Orders WHERE OrderID = @OrderID;
        IF @TableID IS NOT NULL
            UPDATE CafeTables SET Status = 'Cleaning' WHERE TableID = @TableID;

        -- Loyalty points (1 point per 10 PKR)
        IF @CustomerID IS NOT NULL
        BEGIN
            DECLARE @EarnedPoints INT = CAST(@TotalAmt / 10 AS INT);
            UPDATE Customers
            SET LoyaltyPoints = LoyaltyPoints + @EarnedPoints - @UsePoints,
                TotalSpent     = TotalSpent + @TotalAmt,
                UpdatedAt      = GETDATE()
            WHERE CustomerID = @CustomerID;

            IF @EarnedPoints > 0
                INSERT INTO LoyaltyPoints (CustomerID, OrderID, Points, TransType, Description)
                VALUES (@CustomerID, @OrderID, @EarnedPoints, 'Earn', 'Points from order #' + CAST(@OrderID AS NVARCHAR));

            IF @UsePoints > 0
                INSERT INTO LoyaltyPoints (CustomerID, OrderID, Points, TransType, Description)
                VALUES (@CustomerID, @OrderID, -@UsePoints, 'Redeem', 'Points redeemed on order #' + CAST(@OrderID AS NVARCHAR));
        END

        SET @Success = 1;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        SET @Success = 0;
        THROW;
    END CATCH
END
GO

-- ============================================================
-- SP: Daily Sales Report
-- ============================================================
CREATE OR ALTER PROCEDURE sp_DailySalesReport
    @Date DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @Date IS NULL SET @Date = CAST(GETDATE() AS DATE);

    SELECT
        COUNT(*)                  AS TotalOrders,
        SUM(TotalAmount)          AS TotalRevenue,
        SUM(TaxAmt)               AS TotalTax,
        SUM(DiscountAmt)          AS TotalDiscounts,
        AVG(TotalAmount)          AS AvgOrderValue,
        SUM(CASE WHEN OrderType='Dine-In'  THEN 1 ELSE 0 END) AS DineInCount,
        SUM(CASE WHEN OrderType='Takeaway' THEN 1 ELSE 0 END) AS TakeawayCount,
        SUM(CASE WHEN OrderType='Delivery' THEN 1 ELSE 0 END) AS DeliveryCount
    FROM Orders
    WHERE CAST(OrderDate AS DATE) = @Date AND Status = 'Completed';

    -- Best selling products of the day
    SELECT TOP 10
        p.ProductName,
        SUM(od.Quantity)  AS QuantitySold,
        SUM(od.LineTotal) AS Revenue
    FROM OrderDetails od
    INNER JOIN Orders o   ON od.OrderID   = o.OrderID
    INNER JOIN Products p ON od.ProductID = p.ProductID
    WHERE CAST(o.OrderDate AS DATE) = @Date AND o.Status = 'Completed'
    GROUP BY p.ProductName
    ORDER BY QuantitySold DESC;
END
GO

-- ============================================================
-- SP: Monthly Revenue Report
-- ============================================================
CREATE OR ALTER PROCEDURE sp_MonthlyReport
    @Year  INT,
    @Month INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @Month IS NULL
    BEGIN
        -- Full year breakdown by month
        SELECT
            MONTH(OrderDate)     AS MonthNum,
            DATENAME(MONTH, OrderDate) AS MonthName,
            COUNT(*)             AS Orders,
            SUM(TotalAmount)     AS Revenue,
            SUM(TaxAmt)          AS Tax
        FROM Orders
        WHERE YEAR(OrderDate) = @Year AND Status = 'Completed'
        GROUP BY MONTH(OrderDate), DATENAME(MONTH, OrderDate)
        ORDER BY MonthNum;
    END
    ELSE
    BEGIN
        -- Day-by-day for given month
        SELECT
            CAST(OrderDate AS DATE) AS OrderDay,
            COUNT(*)                AS Orders,
            SUM(TotalAmount)        AS Revenue
        FROM Orders
        WHERE YEAR(OrderDate) = @Year AND MONTH(OrderDate) = @Month
              AND Status = 'Completed'
        GROUP BY CAST(OrderDate AS DATE)
        ORDER BY OrderDay;
    END
END
GO

-- ============================================================
-- SP: Customer Purchase History
-- ============================================================
CREATE OR ALTER PROCEDURE sp_CustomerHistory
    @CustomerID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT o.OrderID, o.OrderNumber, o.OrderDate, o.OrderType,
           o.TotalAmount, o.Status,
           STRING_AGG(p.ProductName + ' x' + CAST(od.Quantity AS NVARCHAR), ', ')
               WITHIN GROUP (ORDER BY od.OrderDetailID) AS Items
    FROM Orders o
    INNER JOIN OrderDetails od ON o.OrderID   = od.OrderID
    INNER JOIN Products p      ON od.ProductID = p.ProductID
    WHERE o.CustomerID = @CustomerID
    GROUP BY o.OrderID, o.OrderNumber, o.OrderDate, o.OrderType, o.TotalAmount, o.Status
    ORDER BY o.OrderDate DESC;
END
GO

-- ============================================================
-- SP: Inventory Update
-- ============================================================
CREATE OR ALTER PROCEDURE sp_UpdateInventory
    @InventoryID INT,
    @Quantity    DECIMAL(10,2),
    @Operation   NVARCHAR(10),   -- 'ADD' or 'SUBTRACT'
    @UpdatedBy   INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRY
        BEGIN TRANSACTION;

        IF @Operation = 'ADD'
            UPDATE Inventory SET CurrentStock = CurrentStock + @Quantity, LastUpdated = GETDATE()
            WHERE InventoryID = @InventoryID;
        ELSE IF @Operation = 'SUBTRACT'
        BEGIN
            DECLARE @Current DECIMAL(10,2);
            SELECT @Current = CurrentStock FROM Inventory WHERE InventoryID = @InventoryID;
            IF @Current < @Quantity
            BEGIN
                ROLLBACK TRANSACTION;
                RAISERROR('Insufficient stock.', 16, 1);
                RETURN;
            END
            UPDATE Inventory SET CurrentStock = CurrentStock - @Quantity, LastUpdated = GETDATE()
            WHERE InventoryID = @InventoryID;
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- ============================================================
-- SP: Employee Salary Calculation
-- ============================================================
CREATE OR ALTER PROCEDURE sp_CalculateSalary
    @EmployeeID INT,
    @Month      INT,
    @Year       INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @BaseSalary DECIMAL(10,2), @WorkingDays INT,
            @PresentDays INT, @OvertimeHrs DECIMAL(6,2);

    SELECT @BaseSalary = Salary FROM Employees WHERE EmployeeID = @EmployeeID;

    SELECT @PresentDays = COUNT(*),
           @OvertimeHrs = SUM(OvertimeHrs)
    FROM Attendance
    WHERE EmployeeID = @EmployeeID
      AND MONTH(AttendDate) = @Month AND YEAR(AttendDate) = @Year
      AND Status IN ('Present','Late');

    SET @WorkingDays = 26;  -- Standard working days
    DECLARE @PerDaySalary DECIMAL(10,2) = @BaseSalary / @WorkingDays;
    DECLARE @EarnedSalary DECIMAL(10,2) = @PerDaySalary * @PresentDays;
    DECLARE @OvertimePay  DECIMAL(10,2) = (@PerDaySalary / 8) * 1.5 * @OvertimeHrs;
    DECLARE @Deductions   DECIMAL(10,2) = @BaseSalary - @EarnedSalary;

    -- Upsert salary record
    MERGE Salaries AS target
    USING (SELECT @EmployeeID, @Month, @Year) AS src(EID, M, Y)
    ON target.EmployeeID = src.EID AND target.Month = src.M AND target.Year = src.Y
    WHEN MATCHED THEN
        UPDATE SET BaseSalary = @EarnedSalary, OvertimePay = @OvertimePay,
                   Deductions = @Deductions
    WHEN NOT MATCHED THEN
        INSERT (EmployeeID, Month, Year, BaseSalary, OvertimePay, Deductions)
        VALUES (@EmployeeID, @Month, @Year, @EarnedSalary, @OvertimePay, @Deductions);

    SELECT s.*, e.FirstName + ' ' + e.LastName AS EmployeeName
    FROM Salaries s
    INNER JOIN Employees e ON s.EmployeeID = e.EmployeeID
    WHERE s.EmployeeID = @EmployeeID AND s.Month = @Month AND s.Year = @Year;
END
GO

PRINT 'Stored procedures created successfully.';
GO
