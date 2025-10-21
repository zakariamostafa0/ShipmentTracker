using AutoMapper;
using ShipmentTracker.API.DTOs.Announcement;
using ShipmentTracker.API.DTOs.Batch;
using ShipmentTracker.API.DTOs.Branch;
using ShipmentTracker.API.DTOs.Carrier;
using ShipmentTracker.API.DTOs.Client;
using ShipmentTracker.API.DTOs.Port;
using ShipmentTracker.API.DTOs.Role;
using ShipmentTracker.API.DTOs.Shipment;
using ShipmentTracker.API.DTOs.User;
using ShipmentTracker.API.DTOs.Warehouse;
using ShipmentTracker.Core.Entities;

namespace ShipmentTracker.API.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Batch mappings
        CreateMap<Batch, BatchResponse>()
            .ForMember(dest => dest.BranchName, opt => opt.MapFrom(src => src.Branch.Name))
            .ForMember(dest => dest.SourceWarehouseName, opt => opt.MapFrom(src => src.SourceWarehouse != null ? src.SourceWarehouse.Name : null))
            .ForMember(dest => dest.DestinationWarehouseName, opt => opt.MapFrom(src => src.DestinationWarehouse != null ? src.DestinationWarehouse.Name : null))
            .ForMember(dest => dest.SourcePortName, opt => opt.MapFrom(src => src.SourcePort != null ? src.SourcePort.Name : null))
            .ForMember(dest => dest.DestinationPortName, opt => opt.MapFrom(src => src.DestinationPort != null ? src.DestinationPort.Name : null));

        CreateMap<Batch, BatchDetailResponse>()
            .IncludeBase<Batch, BatchResponse>()
            .ForMember(dest => dest.Shipments, opt => opt.MapFrom(src => src.Shipments));

        CreateMap<CreateBatchRequest, Batch>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Core.Enums.BatchStatus.Draft))
            .ForMember(dest => dest.ShipmentCount, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.TotalWeight, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Branch, opt => opt.Ignore())
            .ForMember(dest => dest.SourceWarehouse, opt => opt.Ignore())
            .ForMember(dest => dest.DestinationWarehouse, opt => opt.Ignore())
            .ForMember(dest => dest.SourcePort, opt => opt.Ignore())
            .ForMember(dest => dest.DestinationPort, opt => opt.Ignore())
            .ForMember(dest => dest.Shipments, opt => opt.Ignore());

        // Shipment mappings
        CreateMap<Shipment, ShipmentResponse>()
            .ForMember(dest => dest.ClientName, opt => opt.MapFrom(src => src.Client.User.DisplayName))
            .ForMember(dest => dest.BatchName, opt => opt.MapFrom(src => src.Batch != null ? src.Batch.Name : null))
            .ForMember(dest => dest.CarrierName, opt => opt.MapFrom(src => src.Carrier != null ? src.Carrier.Name : null));

        CreateMap<Shipment, ShipmentDetailResponse>()
            .IncludeBase<Shipment, ShipmentResponse>()
            .ForMember(dest => dest.Events, opt => opt.MapFrom(src => src.Events));

        CreateMap<Shipment, ClientShipmentResponse>()
            .ForMember(dest => dest.BatchName, opt => opt.MapFrom(src => src.Batch != null ? src.Batch.Name : null))
            .ForMember(dest => dest.CarrierName, opt => opt.MapFrom(src => src.Carrier != null ? src.Carrier.Name : null));

        CreateMap<CreateShipmentRequest, Shipment>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Core.Enums.ShipmentStatus.Created))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Client, opt => opt.Ignore())
            .ForMember(dest => dest.Batch, opt => opt.Ignore())
            .ForMember(dest => dest.Carrier, opt => opt.Ignore())
            .ForMember(dest => dest.Events, opt => opt.Ignore());

        // ShipmentEvent mappings
        CreateMap<ShipmentEvent, ShipmentEventDto>()
            .ForMember(dest => dest.ActorUserName, opt => opt.MapFrom(src => src.ActorUser != null ? src.ActorUser.DisplayName : string.Empty));

        // Announcement mappings
        CreateMap<Announcement, AnnouncementResponse>()
            .ForMember(dest => dest.CreatedByUserName, opt => opt.MapFrom(src => src.CreatedByUser.DisplayName));

        CreateMap<CreateAnnouncementRequest, Announcement>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.Targets, opt => opt.Ignore());

        CreateMap<UpdateAnnouncementRequest, Announcement>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
            .ForMember(dest => dest.Targets, opt => opt.Ignore());

        // AnnouncementTarget mappings
        CreateMap<AnnouncementTarget, AnnouncementTargetDto>();
        CreateMap<AnnouncementTargetDto, AnnouncementTarget>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.AnnouncementId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Announcement, opt => opt.Ignore())
            .ForMember(dest => dest.Branch, opt => opt.Ignore())
            .ForMember(dest => dest.Client, opt => opt.Ignore());

        // Client mappings
        CreateMap<Client, ClientResponse>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.UserName))
            .ForMember(dest => dest.DisplayName, opt => opt.MapFrom(src => src.User.DisplayName))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email));

        CreateMap<Client, TopClientResponse>()
            .ForMember(dest => dest.ClientName, opt => opt.MapFrom(src => src.User.DisplayName))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
            .ForMember(dest => dest.ShipmentCount, opt => opt.MapFrom(src => src.Shipments.Count))
            .ForMember(dest => dest.TotalWeight, opt => opt.MapFrom(src => src.Shipments.Sum(s => s.Weight)))
            .ForMember(dest => dest.LastShipmentDate, opt => opt.MapFrom(src => src.Shipments.Any() ? src.Shipments.Max(s => s.CreatedAt) : DateTime.MinValue));

        // Master Data mappings
        // Branch mappings
        CreateMap<Branch, BranchResponse>()
            .ForMember(dest => dest.BatchCount, opt => opt.MapFrom(src => src.Batches.Count));

        CreateMap<CreateBranchRequest, Branch>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Batches, opt => opt.Ignore())
            .ForMember(dest => dest.AnnouncementTargets, opt => opt.Ignore());

        CreateMap<UpdateBranchRequest, Branch>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Batches, opt => opt.Ignore())
            .ForMember(dest => dest.AnnouncementTargets, opt => opt.Ignore());

        // Warehouse mappings
        CreateMap<Warehouse, WarehouseResponse>()
            .ForMember(dest => dest.SourceBatchCount, opt => opt.MapFrom(src => src.Batches.Count(b => b.SourceWarehouseId == src.Id)))
            .ForMember(dest => dest.DestinationBatchCount, opt => opt.MapFrom(src => src.Batches.Count(b => b.DestinationWarehouseId == src.Id)))
            .ForMember(dest => dest.ActiveBatchCount, opt => opt.MapFrom(src => 
                src.Batches.Count(b => (b.SourceWarehouseId == src.Id || b.DestinationWarehouseId == src.Id) && 
                    b.Status != Core.Enums.BatchStatus.Delivered && b.Status != Core.Enums.BatchStatus.Cancelled)));

        CreateMap<CreateWarehouseRequest, Warehouse>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Batches, opt => opt.Ignore());

        CreateMap<UpdateWarehouseRequest, Warehouse>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Batches, opt => opt.Ignore());

        // Port mappings
        CreateMap<Port, PortResponse>()
            .ForMember(dest => dest.ActiveBatchCount, opt => opt.MapFrom(src => 
                src.SourceBatches.Count(b => b.Status != Core.Enums.BatchStatus.Delivered && b.Status != Core.Enums.BatchStatus.Cancelled) +
                src.DestinationBatches.Count(b => b.Status != Core.Enums.BatchStatus.Delivered && b.Status != Core.Enums.BatchStatus.Cancelled)));

        CreateMap<CreatePortRequest, Port>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.SourceBatches, opt => opt.Ignore())
            .ForMember(dest => dest.DestinationBatches, opt => opt.Ignore());

        CreateMap<UpdatePortRequest, Port>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.SourceBatches, opt => opt.Ignore())
            .ForMember(dest => dest.DestinationBatches, opt => opt.Ignore());

        // Carrier mappings
        CreateMap<Carrier, CarrierResponse>()
            .ForMember(dest => dest.ActiveShipmentCount, opt => opt.MapFrom(src => src.Shipments.Count(s => s.Status != Core.Enums.ShipmentStatus.Delivered && s.Status != Core.Enums.ShipmentStatus.Cancelled)));

        CreateMap<CreateCarrierRequest, Carrier>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Shipments, opt => opt.Ignore());

        CreateMap<UpdateCarrierRequest, Carrier>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Shipments, opt => opt.Ignore());

        // Role mappings
        CreateMap<Role, RoleResponse>()
            .ForMember(dest => dest.UserCount, opt => opt.MapFrom(src => src.UserRoles.Count));

        CreateMap<CreateRoleRequest, Role>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UserRoles, opt => opt.Ignore());

        CreateMap<UpdateRoleRequest, Role>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UserRoles, opt => opt.Ignore());

        // User mappings
        CreateMap<User, UserResponse>()
            .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.UserRoles.Select(ur => ur.Role)));

        CreateMap<UpdateUserRequest, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserName, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.EmailVerified, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UserRoles, opt => opt.Ignore())
            .ForMember(dest => dest.RefreshTokens, opt => opt.Ignore())
            .ForMember(dest => dest.EmailVerificationTokens, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordResetTokens, opt => opt.Ignore())
            .ForMember(dest => dest.Client, opt => opt.Ignore())
            .ForMember(dest => dest.ShipmentEvents, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAnnouncements, opt => opt.Ignore());
    }
}
