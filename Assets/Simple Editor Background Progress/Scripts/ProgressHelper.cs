using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Assets.SimpleBackgroundProgress.Scripts
{
    public class ProgressHelper
    {
        public List<ProgressInfo> Progresses = new();

        private int _parentProgressId;
        private IEnumerator _updateProgress;
        private bool _parentCanceled;

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
            _parentProgressId = commandsInfo.Count > 1 ? Progress.Start("Overall progress", $"{DateTime.Now}") : -1;

            commandsInfo.ForEach(CreateChildProgress);
            
            if (_parentProgressId == -1) return;

            Progress.RegisterCancelCallback(_parentProgressId, () =>
            {
                Progresses?.ForEach(commandInfo => Progress.Cancel(commandInfo.ProgressId));

                return _parentCanceled = true;
            });
        }
        
        private void CreateChildProgress(TaskProgressInfo taskProgressInfo)
        {
            var progressInfo = new ProgressInfo(taskProgressInfo, Progress.Start(taskProgressInfo.Name, taskProgressInfo.Cmd, _parentProgressId == -1 ? Progress.Options.None : Progress.Options.Sticky, _parentProgressId));

            if (progressInfo.TaskProgressInfo.CancellationTokenSource != null) Progress.RegisterCancelCallback(progressInfo.ProgressId, () => progressInfo.TaskProgressInfo.Cancel());

            Progresses.Add(progressInfo);
        }

        private void Start()
        {
            _updateProgress?.MoveNext();
        }

        private IEnumerator UpdateProgressMain()
        {
            while (Progresses.Count > 0 && !_parentCanceled)
            {
                UpdateChildProgress();

                yield return null;
            }

            End();
        }

        private void End()
        {
            if (Progress.Exists(_parentProgressId)) Progress.Cancel(_parentProgressId);

            Progresses = null;
            _updateProgress = null;
            EditorApplication.update -= Start;
        }

        private void UpdateChildProgress()
        {
            for (var i = Progresses.Count - 1; i >= 0; i--)
            {
                Progresses[i].ReportProgress();

                if (Progresses[i].TaskProgressInfo.CancellationTokenSource != null && Progresses[i].TaskProgressInfo.CancellationTokenSource.IsCancellationRequested)
                {
                    CloseProgress(Progresses[i]);
                    break;
                }
                
                if (Progresses[i].TaskProgressInfo.Progress != 100) continue;
                
                CloseProgress(Progresses[i]);
            }

            void CloseProgress(ProgressInfo progressInfo)
            {
                if (progressInfo.TaskProgressInfo.Progress == 100)
                {
                    Progress.Finish(progressInfo.ProgressId);
                }
                else if (Progress.Exists(progressInfo.ProgressId) && Progress.GetStatus(progressInfo.ProgressId) == Progress.Status.Running)
                {
                    Progress.Cancel(progressInfo.ProgressId);
                }

                Progresses.Remove(progressInfo);
            }
        }

        public class ProgressInfo
        {
            public readonly int ProgressId;
            public readonly TaskProgressInfo TaskProgressInfo;

            public ProgressInfo(TaskProgressInfo taskProgressInfo, int progressId)
            {
                this.TaskProgressInfo = taskProgressInfo;
                ProgressId = progressId;
            }

            public void ReportProgress()
            {
                Progress.Report(ProgressId, TaskProgressInfo.Progress / 100f, TaskProgressInfo.Name);
            }
        }
    }
}
