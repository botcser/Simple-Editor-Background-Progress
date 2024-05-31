using Assets.SimpleBackgroundProgress.Scripts;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
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

            StabJob(newCommandInfo, 2);
        }

        public void CancelSomeProgress()
        {
            _progressHelper.Progresses.FirstOrDefault()?.TaskProgressInfo.Cancel();
        }

        public void StartProgress()
        {
            var commandsInfo = new List<TaskProgressInfo>();

            for (var i = 0; i < 3; i++) commandsInfo.Add(new TaskProgressInfo($"name_{i}", $"command_{i}", new CancellationTokenSource()));

            _progressHelper = new ProgressHelper(commandsInfo);
            
            StabJob(commandsInfo[0], 1);
            StabJob(commandsInfo[1], 2);
            StabJob(commandsInfo[2], 3);

            var anotherProgressHelper = new ProgressHelper("another uncancellable Progress");

            StabJob(anotherProgressHelper.Progresses[0].TaskProgressInfo, 1);
        }

        private static async Task StabJob(TaskProgressInfo taskProgressesInfo, int delay)
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
    }

    [CustomEditor(typeof(Example))]
    public class ExampleEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var ttsSettings = (Example)target;
            var buttonStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, fixedHeight = 30 };
            
            if (GUILayout.Button("Start progress", buttonStyle))
            {
                ttsSettings.StartProgress();
            }

            if (GUILayout.Button("Add some progress", buttonStyle))
            {
                ttsSettings.AddProgress();
            }

            if (GUILayout.Button("Cancel some created progress", buttonStyle))
            {
                ttsSettings.CancelSomeProgress();
            }
        }
    }
}
