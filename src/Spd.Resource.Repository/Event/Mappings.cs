using AutoMapper;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;

namespace Spd.Resource.Repository.Event
{
    internal class Mappings : Profile
    {
        public Mappings()
        {
            _ = CreateMap<Spd_EventQueue, EventResp>()
             .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Spd_EventQueueId ?? Guid.Empty))
             .ForMember(d => d.EventTypeEnum, opt => opt.MapFrom(s => SharedMappingFuncs.GetEnum<EventTypeOptionSet, EventTypeEnum>((int?)s.Spd_EventType)))
             .ForMember(d => d.RegardingObjectId, opt => opt.MapFrom(s => Guid.TryParse(s.Spd_RegardingObjectId, out Guid id) ? id : (Guid?)null))
             .ForMember(d => d.JobId, opt => opt.MapFrom(s => s.Spd_JobId))
             .ForMember(d => d.LastExeTime, opt => opt.MapFrom(s => s.Spd_Timestamp))
             .ForMember(d => d.RegardingObjectName, opt => opt.MapFrom(s => s.Spd_RegardingObjectLogicalName));

            _ = CreateMap<EventUpdateCmd, Spd_EventQueue>()
             .ForMember(d => d.Spd_EventQueueId, opt => opt.Ignore())
             .ForMember(d => d.Spd_Timestamp, opt => opt.MapFrom(s => s.LastExeTime))
             .ForMember(d => d.Spd_ErrorDescription, opt => opt.MapFrom(s => s.ErrorDescription))
             .ForMember(d => d.Spd_JobId, opt => opt.MapFrom(s => s.JobId))
             .ForMember(d => d.StateCode, opt => opt.MapFrom(s => (Spd_EventQueue_StateCode?)s.StateCode))
             .ForMember(d => d.StatusCode, opt => opt.MapFrom(s => GetStatusCode(s.EventStatusReasonEnum)));
        }

        private static Spd_EventQueue_StatusCode? GetStatusCode(EventStatusReasonEnum status)
        {
            int? option = (int?)SharedMappingFuncs.GetOptionset<EventStatusReasonEnum, EventStatusReasonOptionSet>(status);
            return option == null ? null : (Spd_EventQueue_StatusCode?)option;
        }
    }
}
