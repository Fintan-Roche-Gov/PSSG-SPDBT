using AutoMapper;
using Spd.Utilities.Dataverse.EarlyBound;
using Spd.Utilities.Dynamics;

namespace Spd.Resource.Repository.Tasks;

internal class Mappings : Profile
{
    public Mappings()
    {
        _ = CreateMap<CreateTaskCmd, Task>()
         .ForMember(d => d.ActivityId, opt => opt.MapFrom(s => Guid.NewGuid()))
         .ForMember(d => d.Subject, opt => opt.MapFrom(s => s.Subject))
         .ForMember(d => d.Description, opt => opt.MapFrom(s => s.Description))
         .ForMember(d => d.PriorityCode, opt => opt.MapFrom(s => GetPriority(s.TaskPriorityEnum)))
         .ForMember(d => d.ScheduledEnd, opt => opt.MapFrom(s => s.DueDateTime));
    }

    internal static Task_PriorityCode? GetPriority(TaskPriorityEnum? code)
    {
        if (code == null) return Task_PriorityCode.Normal;
        TaskPriorityOptionSet optionSet = Enum.Parse<TaskPriorityOptionSet>(code.ToString());
        return (Task_PriorityCode)(int)optionSet;
    }

    internal static TaskPriorityEnum? GetTaskPriorityEnum(Task_PriorityCode? optionset)
    {
        if (optionset == null) return null;
        return Enum.Parse<TaskPriorityEnum>(optionset.ToString());
    }
}

