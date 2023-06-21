using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UzunTec.Utils.TaskUtils
{
    public interface ITaskPool
    {
        int TotalTasks { get; }
        int RunningTasks { get; }
        int FinishedTasks { get; }
        int WaitingTasks { get; }
        int ErrorTasks { get; }

        object Tag { get; set; }

        Task WaitAllTasksFinished();
    }

    public interface ITaskPool<T> : ITaskPool
    {
        ITaskTicket<T> Add(Func<Task<T>> taskFunction, object reference = null);
        IList<ITaskTicket<T>> Tickets { get; }
        List<ITaskTicket<T>> FinishedTaskList { get; }
    }
}
