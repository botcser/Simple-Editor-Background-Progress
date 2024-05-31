using System.Threading;
using JetBrains.Annotations;

namespace Assets.SimpleBackgroundProgress.Scripts
{
    public class TaskProgressInfo
    {
        public string Name;
        public string Cmd;
        public int Progress;

        [CanBeNull] public CancellationTokenSource CancellationTokenSource;

        public TaskProgressInfo() {}

        public TaskProgressInfo(string name, string cmd, CancellationTokenSource cancellationTokenSource)
        {
            Name = name;
            Cmd = cmd;
            CancellationTokenSource = cancellationTokenSource;
        }

        public bool Cancel()
        {
            if (CancellationTokenSource != null && !CancellationTokenSource.IsCancellationRequested) CancellationTokenSource.Cancel();

            return true;
        }
    }
}
