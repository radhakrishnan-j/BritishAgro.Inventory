# StoreProductLot Enhancement - Changes Summary

## Overview
Added two new properties to track stock addition type (New vs Return) and link returned stock to product usage records.

## Changes Made

### 1. **Data Entity - StoreProductLot.cs**
Added two new properties:
- `AdditionType` (string, required, max length 50): Indicates how stock was added ("New" or "Return")
- `UsageId` (int?, optional): Foreign key to ProductUsage for tracking returned items

### 2. **Data Entity - ProductUsage.cs**
Added collection property:
- `StoreProductLots`: Navigation property for the one-to-many relationship with StoreProductLot

### 3. **DbContext - ApplicationDbContext.cs**
Added relationship configuration:
- Configured one-to-many relationship between ProductUsage and StoreProductLot
- Set cascade delete behavior to SetNull (orphaned stock lots if usage is deleted)

### 4. **Service - InventoryTransactionService.cs**
Updated two methods:

#### AddStockAsync
- Now sets `AdditionType = "New"` for all new stock additions

#### RecordReturnAsync
- Now creates StoreProductLot with:
  - `AdditionType = "Return"`
  - `UsageId` = the return's associated usage ID (if any)

### 5. **Razor Component - ProductStocks.razor**
UI Enhancement:
- Added "Addition Type" column to the stock display table
- Badge styling:
  - Green badge for "New" stock
  - Blue badge for "Return" stock
- Updated table headers to include the new column

### 6. **Database Migration**
Created migration: `20260508173335_AddStoreProductLotAdditionTypeAndUsageId`
- Adds `AdditionType` column with default value "New"
- Adds nullable `UsageId` foreign key column
- Creates index on UsageId for query performance
- Configures proper foreign key constraint

## Business Logic
- When stock is added directly: `AdditionType = "New"`, `UsageId = null`
- When stock is added through returns: `AdditionType = "Return"`, `UsageId = {RelatedUsageId}`

## Database Changes
- New column: `StoreProductLots.AdditionType` (nvarchar(50), NOT NULL, default 'New')
- New column: `StoreProductLots.UsageId` (int, nullable)
- New foreign key relationship to ProductUsages table

## Backward Compatibility
- Existing stock records automatically get `AdditionType = "New"`
- `UsageId` defaults to null for existing records

## Build Status
✅ All changes compile successfully
✅ Migration created and ready to apply
