using System;
using UnityEngine;

 public sealed class ScenarioAnalysisController : MonoBehaviour
    {
        [SerializeField] private ScenarioRunner _runner;
        public AnalysisResult LastResult { get; private set; }

        public event Action<AnalysisResult> AnalysisCompleted;

        private void OnEnable()
        {
            if (_runner == null)
            {
                Debug.LogError("$Не назначен ScenarioRunner {this}");
                return;
            }
            
            _runner.AnalysisRequested += OnAnalysisRequested;
        }

        private void OnDisable()
        {
            if (_runner != null)
                _runner.AnalysisRequested -= OnAnalysisRequested;
        }

        private void OnAnalysisRequested(AnalyzerType analyzerType)
        {
            AnalyzeJournal(_runner.CurrentJournal, analyzerType);
        }

        public AnalysisResult AnalyzeJournal(RunJournal journal, AnalyzerType analyzerType)
        {
            try
            {
                IResultAnalyzer analyzer = CreateAnalyzer(analyzerType);
                RunJournal newJournal = journal?.CreateJournal();
                LastResult = analyzer.Analyze(newJournal);
            }
            catch (Exception exception)
            {
                LastResult = new AnalysisResult
                {
                    AnalyzerName = analyzerType.ToString(),
                    RunId = journal?.RunId
                };

                LastResult.Errors.Add($"Не удалось выполнить анализ: {exception.Message}");
            }

            WriteResult(LastResult);
            AnalysisCompleted?.Invoke(LastResult);

            return LastResult;
        }

        private static IResultAnalyzer CreateAnalyzer(
            AnalyzerType analyzerType)
        {
            switch (analyzerType)
            {
                case AnalyzerType.TrainigScore:
                    return new TrainingAnalyzer();

                case AnalyzerType.ExamScore:
                    return new ExamAnalyzer();

                default:
                    throw new ArgumentOutOfRangeException(nameof(analyzerType));
            }
        }

        private void WriteResult(AnalysisResult result)
        {
            if (result.Score.HasValue)
            {
                string label = result.IsFinal ? "Итог" : "Предварительно";
                Debug.Log($"{result.AnalyzerName}. {label}: {result.Score.Value}/{result.MaximumScore} {this}");
            }
            else
            {
                Debug.LogWarning($"Оценка не рассчитана, необходимо проверить диагностику {this}");
            }

            foreach (string warning in result.Warnings)
                Debug.LogWarning(warning, this);

            foreach (string error in result.Errors)
                Debug.LogError(error, this);
        }
    }
