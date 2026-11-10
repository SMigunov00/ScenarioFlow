using UnityEngine; 
public sealed class ScenarioDebugControls : MonoBehaviour
{
    [SerializeField] private ScenarioRunner _runner;

    private void OnEnable()
    {
        if (_runner == null)
        {
            Debug.LogError($"Не назначен ScenarioRunner {this}");
            return;
        }

        _runner.RunStarted += OnRunStarted;
        _runner.RunFinished += OnRunFinished;
        _runner.AnalysisRequested += OnAnalysisRequested;
    }

    private void OnDisable()
    {
        if (_runner == null)
            return;

        _runner.RunStarted -= OnRunStarted;
        _runner.RunFinished -= OnRunFinished;
        _runner.AnalysisRequested -= OnAnalysisRequested;
    }

    [ContextMenu("Тест/1. Запустить сценарий")]
    public void StartRun()
    {
        if (CanExecute())
            _runner.StartScenario();
    }

    [ContextMenu("Тест/2. Выбрать тренировочный анализатор")]
    public void SelectTrainingAnalyzer()
    {
        if (CanExecute())
            _runner.SelectAnalyzer(AnalyzerType.TrainigScore);
    }
    
    [ContextMenu("Тест/3. Выбрать экзаменационный анализатор")]
    public void SelectExamAnalyzer()
    {
        if (CanExecute())
            _runner.SelectAnalyzer(AnalyzerType.ExamScore);
    }
    
    [ContextMenu("Тест/4. Завершить сценарий")]
    public void FinishRun()
    {
        if (CanExecute())
            _runner.FinishScenario();
    }

    private bool CanExecute()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning($"Необходимо включить Play Mode {this}");
            return false;
        }

        if (_runner == null)
        {
            Debug.LogError($"Не назначен ScenarioRunner {this}");
            return false;
        }

        return true;
    }

    private void OnRunStarted(
        ScenarioData scenario,
        RunSettings settings)
    {
        Debug.Log($"[Старт] {scenario.ScenarioName}\n" +
                  $"Режим: {settings.Mode}\n" +
                  $"Логгер: {settings.LoggerType}\n" +
                  $"Анализатор: {settings.AnalyzerType}",
            this);
    }

    private void OnRunFinished()
    {
        Debug.Log($"[Завершение] Сценарий остановлен {this}");
    }

    private void OnAnalysisRequested(AnalyzerType analyzer)
    {
        Debug.Log($"[Анализ] Запрошен пересчёт: {analyzer} {this}");
    } 
}
