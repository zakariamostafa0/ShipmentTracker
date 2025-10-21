namespace ShipmentTracker.Core.Enums;

public enum ShipmentStatus
{
    Created = 0,
    InBatch = 1,
    InWarehouse = 2,
    AtSourcePort = 3,
    InTransit = 4,
    AtDestinationPort = 5,
    WithCarrier = 6,
    OutForDelivery = 7,
    Delivered = 8,
    Returned = 9,
    Cancelled = 10
}
