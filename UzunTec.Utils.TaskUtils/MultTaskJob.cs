using System;
using System.Threading;
using System.Threading.Tasks;

namespace UzunTec.Utils.TaskUtils
{
    public abstract class MultTaskJob
    {
        private volatile bool jobFinished = false;

        protected abstract Task Run();

        protected void FinishJob() => this.jobFinished = true;


        public async Task Execute()
        {
            await this.Run();

            while (!this.jobFinished)
            {
                Console.Write(this.ReportStatus() + "\r");
                Thread.Sleep(500);
            }
        }

        protected virtual string ReportStatus() => "";
    }
}
