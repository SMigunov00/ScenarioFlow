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
    private readonly string _directory;

    public JsonRunLogger(string directory = null)
    {
        _directory = ResolveDirectory(directory);
    }
    
    public static string ResolveDirectory(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            directory = "ScenarioFlow/Logs";

        return Path.GetFullPath(Path.IsPathRooted(directory) ? directory : Path.Combine(Application.persistentDataPath, directory));
    }

    public static string GetFilePath(string directory, string runId)
    {
        return Path.Combine(directory, runId + ".json");
    }
    
    public void Write(RunJournal journal, RunEvent runEvent)
    {
        Directory.CreateDirectory(_directory);

        string filePath = GetFilePath(_directory, journal.RunId);
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

            if (loaded == null || loaded.SchemaVersion != 1 || string.IsNullOrWhiteSpace(loaded.RunId) ||
                loaded.StepIds == null || loaded.Events == null)
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