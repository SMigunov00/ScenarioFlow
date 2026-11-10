using System;
using System.IO;
using UnityEngine;


public interface IRunLogger
{
    void Write(RunJournal journal, RunEvent runEvent);
}

public sealed class ConsoleRunLogger : IRunLogger
{
    public void Write(RunJournal journal, RunEvent runEvent)
    {
        Debug.Log(JsonUtility.ToJson(runEvent));
    }
}

public sealed class JsonRunLogger : IRunLogger
{
    public void Write(RunJournal journal, RunEvent runEvent)
    {
        string directory = Path.Combine(Application.persistentDataPath, "ScenarioFlow", "Logs");
        Directory.CreateDirectory(directory);

        string filePath = Path.Combine(directory, journal.RunId + ".json");
        
        string json = JsonUtility.ToJson(journal, true);

        File.WriteAllText(filePath, json);
    }

    public static bool TryLoad(string filePath, out RunJournal journal, out string error)
    {
        journal = null;
        error = null;

        try
        {
            string json = File.ReadAllText(filePath);

            var loaded =
                JsonUtility.FromJson<RunJournal>(json);

            if (loaded == null ||
                loaded.SchemaVersion != 1 ||
                string.IsNullOrWhiteSpace(loaded.RunId) ||
                loaded.StepIds == null ||
                loaded.Events == null)
            {
                error = "Неподдерживаемый или повреждённый журнал";
                return false;
            }

            journal = loaded;
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }
    }
}