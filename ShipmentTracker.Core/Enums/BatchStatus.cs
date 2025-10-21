namespace ShipmentTracker.Core.Enums;

public enum BatchStatus
{
    Draft = 0,
    Open = 1,
    Closed = 2,
    InWarehouse = 3,
    AtSourcePort = 4,
    ClearedSourcePort = 5,
    InTransit = 6,
    ArrivedDestinationPort = 7,
    InDestinationWarehouse = 8,
    AssignedToCarriers = 9,
    Delivered = 10,
    PartiallyDelivered = 11,
    Cancelled = 12,
    Archived = 13
}
