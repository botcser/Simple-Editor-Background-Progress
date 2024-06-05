using System;
using System.Threading;
using JetBrains.Annotations;
using UnityEditor;

namespace Assets.SimpleBackgroundProgress.Scripts
{
    public class TaskProgressInfo : IProgressProperty
    {
        public string Name;
        public string Cmd;
        public int Progress;
        public Progress.Status Status;

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

        public void Set(int value)
        {
            Progress = value;
        }

        public int Get()
        {
            return Progress;
        }
    }

    public interface IProgressProperty
    {
        public void Set(int value);
        public int Get();
    }
}
