-- Shipment Tracker Database Seed Data
-- Execute this script to populate initial roles and sample data

-- Insert default roles
INSERT INTO Role (Name, RoleType, CreatedAt, UpdatedAt) VALUES
('DataEntry', 1, SYSDATETIME(), SYSDATETIME()),
('BranchAdmin', 2, SYSDATETIME(), SYSDATETIME()),
('WarehouseOperator', 3, SYSDATETIME(), SYSDATETIME()),
('PortOperator', 4, SYSDATETIME(), SYSDATETIME()),
('CarrierOperator', 5, SYSDATETIME(), SYSDATETIME()),
('Client', 6, SYSDATETIME(), SYSDATETIME()),
('Admin', 7, SYSDATETIME(), SYSDATETIME());

-- Insert sample branches
INSERT INTO Branch (Name, Address, CreatedAt, UpdatedAt) VALUES
('Main Branch', '123 Main Street, City, Country', SYSDATETIME(), SYSDATETIME()),
('North Branch', '456 North Avenue, City, Country', SYSDATETIME(), SYSDATETIME()),
('South Branch', '789 South Road, City, Country', SYSDATETIME(), SYSDATETIME());

-- Insert sample warehouses
INSERT INTO Warehouse (Name, Address, CreatedAt, UpdatedAt) VALUES
('Central Warehouse', '100 Warehouse Lane, City, Country', SYSDATETIME(), SYSDATETIME()),
('Distribution Center', '200 Distribution Drive, City, Country', SYSDATETIME(), SYSDATETIME());

-- Insert sample ports
INSERT INTO Port (Name, Address, Country, CreatedAt, UpdatedAt) VALUES
('Port of Origin', '300 Port Street, Origin City, Origin Country', 'Origin Country', SYSDATETIME(), SYSDATETIME()),
('Port of Destination', '400 Destination Port, Dest City, Dest Country', 'Destination Country', SYSDATETIME(), SYSDATETIME());

-- Insert sample carriers
INSERT INTO Carrier (Name, ContactInfo, CreatedAt, UpdatedAt) VALUES
('Fast Delivery Co.', 'Phone: +1-555-0101, Email: contact@fastdelivery.com', SYSDATETIME(), SYSDATETIME()),
('Reliable Transport', 'Phone: +1-555-0102, Email: info@reliabletransport.com', SYSDATETIME(), SYSDATETIME()),
('Express Logistics', 'Phone: +1-555-0103, Email: support@expresslogistics.com', SYSDATETIME(), SYSDATETIME());
