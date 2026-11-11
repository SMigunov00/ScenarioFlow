using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ExamModePolicy : IModePolicy
    {
        private sealed class GroupRule
        {
            public string[] StepIds;
            public int Limit;
        }

        private readonly List<GroupRule> groups =
            new List<GroupRule>();

        public ExamModePolicy(
            IReadOnlyList<ExamGroupData> definitions,
            IReadOnlyList<ActivationBatchData> plan,
            RunState run)
        {
            if (definitions == null || definitions.Count == 0)
            {
                throw new ArgumentException(
                    "Не настроены экзаменационные группы.");
            }

            var groupByStep = new Dictionary<string, int>();

            for (int i = 0; i < definitions.Count; i++)
            {
                ExamGroupData definition = definitions[i];

                if (definition == null ||
                    definition.StepIds == null ||
                    definition.StepIds.Count == 0 ||
                    definition.ViolationLimit < 1)
                {
                    throw new ArgumentException(
                        $"Некорректные настройки группы {i + 1}.");
                }

                foreach (string stepId in definition.StepIds)
                {
                    if (string.IsNullOrWhiteSpace(stepId) ||
                        !run.Steps.ContainsKey(stepId))
                    {
                        throw new ArgumentException(
                            $"В группе указан неизвестный шаг: {stepId}.");
                    }

                    if (groupByStep.ContainsKey(stepId))
                    {
                        throw new ArgumentException(
                            $"Шаг {stepId} входит в несколько групп.");
                    }

                    groupByStep.Add(stepId, i);
                }

                groups.Add(new GroupRule
                {
                    StepIds = definition.StepIds.ToArray(),
                    Limit = definition.ViolationLimit
                });
            }

            if (groupByStep.Count != run.Steps.Count)
            {
                throw new ArgumentException(
                    "Каждый шаг должен входить " +
                    "в одну экзаменационную группу.");
            }

            ValidatePlan(plan, groupByStep);
        }

        private static void ValidatePlan(
            IReadOnlyList<ActivationBatchData> plan,
            Dictionary<string, int> groupByStep)
        {
            if (plan == null)
                throw new ArgumentNullException(nameof(plan));

            int previousGroup = -1;

            foreach (ActivationBatchData batch in plan)
            {
                if (batch == null ||
                    batch.StepIds == null ||
                    batch.StepIds.Count == 0)
                {
                    throw new ArgumentException("Пустой пакет активации.");
                }

                int batchGroup = -1;

                foreach (string stepId in batch.StepIds)
                {
                    if (string.IsNullOrWhiteSpace(stepId) ||
                        !groupByStep.TryGetValue(stepId, out int group))
                    {
                        throw new ArgumentException(
                            $"Для шага {stepId} не определена группа.");
                    }

                    if (batchGroup >= 0 && batchGroup != group)
                    {
                        throw new ArgumentException(
                            "Один пакет не может содержать " +
                            "шаги разных экзаменационных групп.");
                    }

                    batchGroup = group;
                }

                if (batchGroup < previousGroup)
                {
                    throw new ArgumentException(
                        "Порядок пакетов противоречит порядку групп.");
                }

                previousGroup = batchGroup;
            }
        }

        public StepResolution Resolve(
            StepState step,
            ConditionOutcome outcome)
        {
            switch (outcome)
            {
                case ConditionOutcome.Success:
                    return new StepResolution(StepStatus.Completed);

                case ConditionOutcome.Violation:
                    return new StepResolution(StepStatus.Failed);

                default:
                    throw new ArgumentOutOfRangeException(nameof(outcome));
            }
        }

        public IReadOnlyList<string> GetStepsToSkip(RunState run)
        {
            foreach (GroupRule group in groups)
            {
                int failedCount = 0;
                var unfinished = new List<string>();

                foreach (string stepId in group.StepIds)
                {
                    StepState step = run.Steps[stepId];

                    if (step.Status == StepStatus.Failed)
                        failedCount++;

                    if (!step.IsTerminal)
                        unfinished.Add(stepId);
                }

                // Группа уже завершена — рассматриваем следующую.
                if (unfinished.Count == 0)
                    continue;

                // Это первая незавершённая, то есть текущая группа.
                if (failedCount >= group.Limit)
                    return unfinished;

                return Array.Empty<string>();
            }

            return Array.Empty<string>();
        }
    }
