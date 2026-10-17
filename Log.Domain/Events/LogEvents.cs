namespace Log.Domain.Events;

public interface ILogEventOccurred
{
    string ServiceName { get; }
    string EventType { get; }
    string ActorUserId { get; }
    string EntityName { get; }
    string EntityId { get; }
    string DetailsJson { get; }
    string LogLevel { get; }
}
