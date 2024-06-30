using Assets.SimpleBackgroundProgress.Scripts;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Assets.SimpleBackgroundProgress
{
    [CreateAssetMenu(fileName = "ExampleSBP", menuName = "ExampleSBP/CreateSO")]
    public class Example : ScriptableObject
    {
        private ProgressHelper _progressHelper;
        
        public void AddProgress()
        {
            var newCommandInfo = new TaskProgressInfo("addedJob", "addedCommand", new CancellationTokenSource());

            _progressHelper.AddChild(newCommandInfo);

#pragma warning disable CS4014
            StabJob(newCommandInfo, 2);
#pragma warning restore CS4014
        }

        public void StartMultiProgress()
        {
            var commandsInfo = new List<TaskProgressInfo>();

            for (var i = 0; i < 3; i++) commandsInfo.Add(new TaskProgressInfo($"name_{i}", $"command_{i}", new CancellationTokenSource()));

            _progressHelper = new ProgressHelper(commandsInfo);
            
#pragma warning disable CS4014
            StabJob(commandsInfo[0]);
            StabJob(commandsInfo[1], 2);
            StabJob(commandsInfo[2], 3);
#pragma warning restore CS4014
        }

        public void StartUncancellableProgress()
        {
#pragma warning disable CS4014
            var anotherProgressHelper = new ProgressHelper("Uncancellable Progress");

            StabJob(anotherProgressHelper.Progresses[0].TaskProgressInfo, 4);
#pragma warning restore CS4014
        }

        public void StartCancellableProgress()
        {
#pragma warning disable CS4014
            var anotherProgressHelper = new ProgressHelper(new TaskProgressInfo("Cancellable Progress", "", new CancellationTokenSource()));

            StabJob(anotherProgressHelper.Progresses[0].TaskProgressInfo, 4);
#pragma warning restore CS4014
        }

        private static async Task StabJob(TaskProgressInfo taskProgressesInfo, int delay = 1)
        {
            await Task.Run(() =>
            {
                for (var j = 1; j <= 100; j++)
                {
                    if (taskProgressesInfo.CancellationTokenSource != null && taskProgressesInfo.CancellationTokenSource.IsCancellationRequested) break;

                    Task.Delay(delay * 50).Wait();

                    taskProgressesInfo.Progress = j;
                }
            }, taskProgressesInfo.CancellationTokenSource?.Token ?? new CancellationToken());
        }

        public void CancelSomeProgress()
        {
            var progressInfo = _progressHelper.Progresses.FirstOrDefault();

            if (progressInfo != null)
            {
                _progressHelper.Progresses.FirstOrDefault()?.TaskProgressInfo.Cancel();
            }
            else
            {
                ShowUsage();
            }
        }

        public void FailSomeProgress()
        {
            var progressInfo = _progressHelper.Progresses.FirstOrDefault();

            if (progressInfo != null)
            {
                progressInfo.TaskProgressInfo.Status = Progress.Status.Failed;
                progressInfo.TaskProgressInfo.Cancel();
            }
            else
            {
                ShowUsage();
            }
        }

        private static void ShowUsage()
        {
            Debug.LogWarning("To do this start multi Progress first!");
        }
    }

    [CustomEditor(typeof(Example))]
    public class ExampleEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var ttsSettings = (Example)target;
            var buttonStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, fixedHeight = 30 };
            
            if (GUILayout.Button("Start multi progresses", buttonStyle))
            {
                ttsSettings.StartMultiProgress();
            }

            if (GUILayout.Button("Start uncancellable progresses", buttonStyle))
            {
                ttsSettings.StartUncancellableProgress();
            }

            if (GUILayout.Button("Start cancellable progresses", buttonStyle))
            {
                ttsSettings.StartCancellableProgress();
            }

            if (GUILayout.Button("Add some progress", buttonStyle))
            {
                ttsSettings.AddProgress();
            }

            if (GUILayout.Button("Cancel some created progress", buttonStyle))
            {
                ttsSettings.CancelSomeProgress();
            }

            if (GUILayout.Button("Fail some created progress", buttonStyle))
            {
                ttsSettings.FailSomeProgress();
            }
        }
    }
}
