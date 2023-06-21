using System.Threading.Tasks;

namespace UzunTec.Utils.TaskUtils
{
    public delegate void TaskCompletedMethod<T>(ITaskTicket<T> ticket, Task<T> task, object reference);

    public interface ITaskTicket
    {
        object Reference { get; }
        string Id { get; }
        TaskStatus Status { get; }
    }

    public interface ITaskTicket<T> : ITaskTicket
    {
        TaskCompletedMethod<T> OnFinish { get; set; }
        T GetTaskResultOrDefault(T defaultOnException = default);
        T GetTaskResult();
    }
}
