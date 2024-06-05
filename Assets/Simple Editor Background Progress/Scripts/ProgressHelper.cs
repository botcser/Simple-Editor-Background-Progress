using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;

namespace Assets.SimpleBackgroundProgress.Scripts
{
    public class ProgressHelper
    {
        public List<ProgressInfo> Progresses = new();

        private int _parentProgressId;
        private IEnumerator _updateProgress;
        private Progress.Status _parentStatus = Progress.Status.Running;
        private bool _childCancelled;

        public ProgressHelper(List<TaskProgressInfo> commandsInfo)
        {
            EditorApplication.update -= Start;
            EditorApplication.update += Start;
            _updateProgress = UpdateProgressMain();

            CreateProgress(commandsInfo);
        }

        public ProgressHelper(TaskProgressInfo taskProgressesInfo) : this(new List<TaskProgressInfo>() { taskProgressesInfo }) {}
        
        public ProgressHelper(string title, string description = "", CancellationTokenSource cancellationTokenSource = null) : this(new TaskProgressInfo(title, description, cancellationTokenSource)) {}
        
        public bool AddChild(TaskProgressInfo taskProgressInfo)
        {
            if (!Progress.Exists(_parentProgressId)) return false;

            CreateChildProgress(taskProgressInfo);

            return true;
        }

        private void CreateProgress(List<TaskProgressInfo> commandsInfo)
        {
            _parentProgressId = commandsInfo.Count > 1 ? Progress.Start("Overall progress", $"{DateTime.Now}", Progress.Options.Sticky) : -1;

            commandsInfo.ForEach(CreateChildProgress);
            
            if (_parentProgressId == -1) return;

            Progress.RegisterCancelCallback(_parentProgressId, () =>
            {
                for (var i = Progresses.Count - 1; i >= 0; i--)
                {
                    Progress.Cancel(Progresses[i].ProgressId);
                }

                _parentStatus = _parentStatus == Progress.Status.Failed ? _parentStatus : Progress.Status.Canceled;

                return true;
            });
        }
        
        private void CreateChildProgress(TaskProgressInfo taskProgressInfo)
        {
            var progressInfo = new ProgressInfo(taskProgressInfo, Progress.Start(taskProgressInfo.Name, taskProgressInfo.Cmd, _parentProgressId == -1 ? Progress.Options.None : Progress.Options.Sticky, _parentProgressId));

            if (progressInfo.TaskProgressInfo.CancellationTokenSource != null) Progress.RegisterCancelCallback(progressInfo.ProgressId, () => progressInfo.TaskProgressInfo.Cancel());

            Progresses.Add(progressInfo);

            taskProgressInfo.CancellationTokenSource?.Token.Register(() =>
            {
                _parentStatus = _parentStatus == Progress.Status.Failed ? _parentStatus : Progress.Status.Canceled;
                CloseProgress(progressInfo, Progress.Status.Canceled);
            });
        }

        private void Start()
        {
            _updateProgress?.MoveNext();
        }

        private IEnumerator UpdateProgressMain()
        {
            while (Progresses.Count > 0)
            {
                UpdateChildProgress();

                yield return null;
            }

            if (_parentStatus != Progress.Status.Canceled && _parentStatus != Progress.Status.Failed) _parentStatus = Progress.Status.Succeeded;

            End();
        }

        private void End()
        {
            Progress.Finish(_parentProgressId, _parentStatus); // For some reason: "Exists/Cancel can only be called from the main thread".
            
            Progresses = null;
            _updateProgress = null;
            EditorApplication.update -= Start;
        }

        private void UpdateChildProgress()
        {
            for (var i = Progresses.Count - 1; i >= 0; i--)
            {
                Progresses[i].ReportProgress(); 
                
                if (Progresses[i].TaskProgressInfo.Progress != 100) continue;

                CloseProgress(Progresses[i], Progress.Status.Succeeded);
            }
        }

        private void CloseProgress(ProgressInfo progressInfo, Progress.Status status)
        {
            if (progressInfo.TaskProgressInfo.Status == Progress.Status.Failed) _parentStatus = Progress.Status.Failed;
            else progressInfo.TaskProgressInfo.Status = status;

            progressInfo.Close();
            Progresses.Remove(progressInfo);
        }

        public class ProgressInfo
        {
            public readonly int ProgressId;
            public readonly TaskProgressInfo TaskProgressInfo;

            public ProgressInfo(TaskProgressInfo taskProgressInfo, int progressId)
            {
                TaskProgressInfo = taskProgressInfo;
                ProgressId = progressId;
            }

            public void Close()
            {
                Progress.Finish(ProgressId, TaskProgressInfo.Status);
            }

            public void ReportProgress()
            {
                Progress.Report(ProgressId, TaskProgressInfo.Progress / 100f, TaskProgressInfo.Name);
            }
        }
    }
}
