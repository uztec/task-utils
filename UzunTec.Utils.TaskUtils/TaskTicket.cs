using System;
using System.Threading.Tasks;

namespace UzunTec.Utils.TaskUtils
{
    public class TaskTicket<T> : ITaskTicket<T>
    {
        private readonly Func<Task<T>> taskFactoryMethod;
        public object Reference { get; }
        public string Id { get; }
        public TaskStatus Status { get; private set; }

        internal Task<T> InternalTask { get; private set; }

        private TaskCompletedMethod<T> _onFinish;

        public TaskCompletedMethod<T> OnFinish
        {
            get => _onFinish;
            set
            {
                _onFinish = value;
                if (this.Status == TaskStatus.RanToCompletion || this.Status == TaskStatus.Faulted)
                {
                    _onFinish(this, this.InternalTask, this.Reference);
                }
            }
        }
        private TaskCompletedMethod<T> _onTaskCompleted;
        internal TaskCompletedMethod<T> OnTaskCompleted
        {
            get => _onTaskCompleted;
            set
            {
                _onTaskCompleted = value;
                if (this.Status == TaskStatus.RanToCompletion || this.Status == TaskStatus.Faulted)
                {
                    _onTaskCompleted(this, this.InternalTask, this.Reference);
                }
            }
        }

        internal TaskTicket(Func<Task<T>> taskFactoryMethod, object reference = null)
        {
            this.Id = TaskId.GetNextId();
            this.Reference = reference;
            this.taskFactoryMethod = taskFactoryMethod;

            this._onFinish = (ti, ta, o) => { };
            this._onTaskCompleted = (ti, ta, o) => { };
            this.Status = TaskStatus.WaitingForActivation;
        }

        internal Task<T> RunAsync()
        {
            try
            {
                this.InternalTask = Task<T>.Run(this.taskFactoryMethod);
                this.Status = TaskStatus.Running;
                this.InternalTask.ContinueWith(this.TaskContinueWith);
            }
            catch (Exception ex)
            {
                this.InternalTask = Task.FromException<T>(ex);
                this.Status = TaskStatus.Faulted;
                this.OnTaskCompleted(this, this.InternalTask, this.Reference);
            }
            return this.InternalTask;
        }

        private async void TaskContinueWith(Task<T> t)
        {
            try
            {
                T result = await t.ConfigureAwait(false);
                this.Status = TaskStatus.RanToCompletion;   // Success
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex);
                this.Status = TaskStatus.Faulted;
            }
            finally
            {
                this._onTaskCompleted(this, t, this.Reference);
                this._onFinish(this, t, this.Reference);
            }
        }

        public T GetTaskResultOrDefault(T defaultOnException = default)
        {
            try
            {
                return this.GetTaskResult();
            }
            catch (InvalidOperationException ex)
            {
                System.Diagnostics.Debug.Write(ex);
                return defaultOnException;
            }
        }

        public T GetTaskResult()
        {
            if (this.Status == TaskStatus.RanToCompletion)
            {
                return this.InternalTask.Result;
            }
            else
            {
                throw new InvalidOperationException(this.InternalTask?.Exception?.Message, this.InternalTask?.Exception);
            }
        }

        public override string ToString()
        {
            return $"Id: {this.Id} - {this.Status} - Ref: {this.Reference}";
        }
    }
}
