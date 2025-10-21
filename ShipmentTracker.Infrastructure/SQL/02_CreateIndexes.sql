-- Shipment Tracker Database Indexes
-- Execute this script after creating tables to add performance indexes

-- User table indexes
CREATE INDEX IX_User_Email ON [User] (Email);
CREATE INDEX IX_User_UserName ON [User] (UserName);
CREATE INDEX IX_User_IsActive ON [User] (IsActive);
CREATE INDEX IX_User_EmailVerified ON [User] (EmailVerified);

-- RefreshToken table indexes
CREATE INDEX IX_RefreshToken_UserId ON RefreshToken (UserId);
CREATE INDEX IX_RefreshToken_Token ON RefreshToken (Token);
CREATE INDEX IX_RefreshToken_ExpiresAt ON RefreshToken (ExpiresAt);
CREATE INDEX IX_RefreshToken_IsRevoked ON RefreshToken (IsRevoked);

-- EmailVerificationToken table indexes
CREATE INDEX IX_EmailVerificationToken_UserId ON EmailVerificationToken (UserId);
CREATE INDEX IX_EmailVerificationToken_Token ON EmailVerificationToken (Token);
CREATE INDEX IX_EmailVerificationToken_ExpiresAt ON EmailVerificationToken (ExpiresAt);
CREATE INDEX IX_EmailVerificationToken_IsUsed ON EmailVerificationToken (IsUsed);

-- PasswordResetToken table indexes
CREATE INDEX IX_PasswordResetToken_UserId ON PasswordResetToken (UserId);
CREATE INDEX IX_PasswordResetToken_Token ON PasswordResetToken (Token);
CREATE INDEX IX_PasswordResetToken_ExpiresAt ON PasswordResetToken (ExpiresAt);
CREATE INDEX IX_PasswordResetToken_IsUsed ON PasswordResetToken (IsUsed);

-- Batch table indexes
CREATE INDEX IX_Batch_BranchId_Status ON Batch (BranchId, Status);
CREATE INDEX IX_Batch_Status ON Batch (Status);
CREATE INDEX IX_Batch_WarehouseId ON Batch (WarehouseId);
CREATE INDEX IX_Batch_SourcePortId ON Batch (SourcePortId);
CREATE INDEX IX_Batch_DestinationPortId ON Batch (DestinationPortId);
CREATE INDEX IX_Batch_UpdatedAt ON Batch (UpdatedAt);
CREATE INDEX IX_Batch_CreatedAt ON Batch (CreatedAt);

-- Shipment table indexes
CREATE INDEX IX_Shipment_BatchId ON Shipment (BatchId);
CREATE INDEX IX_Shipment_ClientId_Status ON Shipment (ClientId, Status);
CREATE INDEX IX_Shipment_CarrierId_Status ON Shipment (CarrierId, Status);
CREATE INDEX IX_Shipment_Status ON Shipment (Status);
CREATE INDEX IX_Shipment_CreatedAt ON Shipment (CreatedAt);
CREATE INDEX IX_Shipment_UpdatedAt ON Shipment (UpdatedAt);

-- ShipmentEvent table indexes
CREATE INDEX IX_ShipmentEvent_ShipmentId_CreatedAt ON ShipmentEvent (ShipmentId, CreatedAt);
CREATE INDEX IX_ShipmentEvent_ActorUserId ON ShipmentEvent (ActorUserId);
CREATE INDEX IX_ShipmentEvent_EventType ON ShipmentEvent (EventType);
CREATE INDEX IX_ShipmentEvent_CreatedAt ON ShipmentEvent (CreatedAt);

-- Announcement table indexes
CREATE INDEX IX_Announcement_StartDate_EndDate ON Announcement (StartDate, EndDate);
CREATE INDEX IX_Announcement_CreatedByUserId ON Announcement (CreatedByUserId);
CREATE INDEX IX_Announcement_CreatedAt ON Announcement (CreatedAt);

-- AnnouncementTarget table indexes
CREATE INDEX IX_AnnouncementTarget_AnnouncementId ON AnnouncementTarget (AnnouncementId);
CREATE INDEX IX_AnnouncementTarget_BranchId ON AnnouncementTarget (BranchId);
CREATE INDEX IX_AnnouncementTarget_ClientId ON AnnouncementTarget (ClientId);
CREATE INDEX IX_AnnouncementTarget_Tag ON AnnouncementTarget (Tag);

-- AuditHeader table indexes
CREATE INDEX IX_AuditHeader_TableName ON AuditHeader (TableName);
CREATE INDEX IX_AuditHeader_UserId ON AuditHeader (UserId);
CREATE INDEX IX_AuditHeader_RecordDate ON AuditHeader (RecordDate);
CREATE INDEX IX_AuditHeader_Operation ON AuditHeader (Operation);

-- AuditDetail table indexes
CREATE INDEX IX_AuditDetail_AuditHeaderId ON AuditDetail (AuditHeaderId);
CREATE INDEX IX_AuditDetail_ColumnName ON AuditDetail (ColumnName);

-- OutboxEvent table indexes
CREATE INDEX IX_OutboxEvent_EventType ON OutboxEvent (EventType);
CREATE INDEX IX_OutboxEvent_OccurredAt ON OutboxEvent (OccurredAt);
CREATE INDEX IX_OutboxEvent_ProcessedAt ON OutboxEvent (ProcessedAt);
