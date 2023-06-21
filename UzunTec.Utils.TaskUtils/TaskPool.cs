using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UzunTec.Utils.TaskUtils
{
    public class TaskPool<T> : ITaskPool<T>
    {
        private readonly int runningTasksPoolSize;

        private readonly SortedList<string, ITaskTicket<T>> ticketList;
        private readonly ConcurrentDictionary<string, ITaskTicket<T>> runningTaskTickets;
        private readonly ConcurrentQueue<ITaskTicket<T>> newTasksQueue;

        private readonly object mainLock = new object();

        private bool _running;
        private bool _cancelled;

        public IList<ITaskTicket<T>> Tickets => this.ticketList.Values;
        public ICollection<ITaskTicket<T>> RunnungTasklist => this.runningTaskTickets.Values;
        public int TotalTasks => this.ticketList.Count;
        public int RunningTasks => this.runningTaskTickets.Count;
        public int WaitingTasks => this.newTasksQueue.Count;
        public int FinishedTasks => this.FinishedTaskList.Count;
        public int ErrorTasks => this.ErrorTaskList.Count;

        public List<ITaskTicket<T>> FinishedTaskList { get; }
        public List<ITaskTicket<T>> ErrorTaskList { get; }


        public Action<List<ITaskTicket<T>>> OnAllTasksFinished;
        public object Tag { get; set; }
        public bool AutoStart { get; set; }

        public TaskPool(int size = 20, bool autoStart = true)
        {
            this.OnAllTasksFinished = (l) => { };
            this.ticketList = new SortedList<string, ITaskTicket<T>>();
            this.newTasksQueue = new ConcurrentQueue<ITaskTicket<T>>();
            this.runningTaskTickets = new ConcurrentDictionary<string, ITaskTicket<T>>();
            this.FinishedTaskList = new List<ITaskTicket<T>>();
            this.ErrorTaskList = new List<ITaskTicket<T>>();
            this.runningTasksPoolSize = size;
            this.AutoStart = autoStart;
        }

        public async Task Clear()
        {
            this._cancelled = true;
            await Task.Delay(100);
            this.ticketList.Clear();
            this.runningTaskTickets.Clear();
        }

        public ITaskTicket<T> Add(Func<Task<T>> taskFunction, object reference = null)
        {
            TaskTicket<T> ticket = new TaskTicket<T>(taskFunction, reference);
            this.newTasksQueue.Enqueue(ticket);
            this.ticketList.Add(ticket.Id, ticket);

            if (this.AutoStart)
            {
                this.Start();
            }

            return ticket;
        }

        public void Start()
        {
            if (!this._running)
            {
                _ = Task.Run(() => this.TaskMainLoop());
            }
            this._cancelled = false;
        }

        private void TaskMainLoop()
        {
            this._running = true;

            lock (mainLock)
            {
                while (this.RunningTasks < this.runningTasksPoolSize && this.newTasksQueue.TryDequeue(out ITaskTicket<T> queueticket))
                {
                    if (this._cancelled)
                    {
                        break;
                    }
                    TaskTicket<T> ticket = (TaskTicket<T>)queueticket ?? throw new Exception("Ticket cannot be null");

                    if (this.runningTaskTickets.TryAdd(ticket.Id, ticket))
                    {
                        ticket.RunAsync();
                        ticket.OnTaskCompleted = (tickey, task, r) => this.TaskFinished(ticket);
                    }
                    else
                    {

                    }
                }
            }
            VerifyIfProcessIsFinalized();
        }

        private void TaskFinished(ITaskTicket<T> ticket)
        {
            FinishTicket(ticket.Id);
            this.TaskMainLoop();
        }

        private void VerifyIfProcessIsFinalized()
        {
            if (this._cancelled)
            {
                this._running = false;
            }

            VerifyTicketsIfFinished();

            if (this.runningTaskTickets.Count == 0 && this.newTasksQueue.Count == 0)
            {
                this.OnAllTasksFinished(new List<ITaskTicket<T>>(this.ticketList.Values));
                this._running = false;
            }
        }

        private void VerifyTicketsIfFinished()
        {
            List<string> ticketsIdsToFinish = new List<string>();
            foreach (string id in this.runningTaskTickets.Keys)
            {
                if (this.runningTaskTickets.TryGetValue(id, out ITaskTicket<T> ticket))
                {
                    if (ticket.Status == TaskStatus.RanToCompletion || ticket.Status == TaskStatus.Faulted)
                    {
                        ticketsIdsToFinish.Add(id);
                    }
                }
            }
            ticketsIdsToFinish.ForEach(id => this.FinishTicket(id));
        }

        private void FinishTicket(string id)
        {
            if (this.runningTaskTickets.TryRemove(id, out ITaskTicket<T> ticket))
            {
                if (ticket.Status == TaskStatus.RanToCompletion)
                {
                    this.FinishedTaskList.Add(ticket);
                }
                else
                {
                    this.ErrorTaskList.Add(ticket);
                }
            }
        }

        public async Task WaitAllTasksFinished()
        {
            int cnt = 0;
            while (_running || this.runningTaskTickets.Count > 0 || this.newTasksQueue.Count > 0)
            {
                await Task.Delay(10);
                if (cnt == 100)
                {
                    this.VerifyIfProcessIsFinalized();
                    cnt = 0;
                }
            }
        }

        public override string ToString()
        {
            return $"Tasks: {this.TotalTasks} - Running: {this.RunningTasks} - Finished: {this.FinishedTasks}  - Error: {this.ErrorTasks}";
        }
    }
}
