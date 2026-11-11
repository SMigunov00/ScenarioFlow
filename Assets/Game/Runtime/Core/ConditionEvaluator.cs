using System;

public sealed class ConditionEvaluator
    {
        public StepEvaluationResult Evaluate(StepConditionData condition, int currentIndex, ScenarioAction action, IConditionContext context)
        {
            if (condition == null || condition.ActionSequence == null || condition.ActionSequence.Count == 0)
            {
                return 
                    new StepEvaluationResult(ConditionOutcome.ConfigurationError, 
                        currentIndex, "Не настроена последовательность действий");
            }

            foreach (string actionId in condition.ActionSequence)
            {
                if (string.IsNullOrWhiteSpace(actionId))
                {
                    return new StepEvaluationResult(ConditionOutcome.ConfigurationError, 
                        currentIndex, $"В последовательности есть пустой ActionId {actionId}");
                }
            }

            if (action == null || currentIndex < 0 || currentIndex > condition.ActionSequence.Count)
            {
                return new StepEvaluationResult(ConditionOutcome.ConfigurationError, 
                    currentIndex, "Некорректное действие или прогресс условия");
            }

            if (currentIndex == condition.ActionSequence.Count)
            {
                return new StepEvaluationResult(ConditionOutcome.Ignore, 
                    currentIndex, "Условие уже выполнено");
            }

            string expectedAction = condition.ActionSequence[currentIndex];

            if (!string.Equals(action.ActionId, expectedAction, StringComparison.Ordinal))
            {
                ConditionOutcome outcome = condition.UnexpectedActionIsViolation ? ConditionOutcome.Violation : ConditionOutcome.Ignore;

                return new StepEvaluationResult(outcome,
                    currentIndex, $"Ожидаемое действие: {expectedAction} | Получено: {action.ActionId}");
            }

            if (condition.RequireState)
            {
                if (string.IsNullOrWhiteSpace(condition.RequiredStateId))
                {
                    return new StepEvaluationResult(ConditionOutcome.ConfigurationError, 
                        currentIndex, "Не задан ключ требуемого состояния");
                }

                if (context == null || !context.TryGetBool(condition.RequiredStateId, out bool actualValue))
                {
                    return new StepEvaluationResult(ConditionOutcome.ConfigurationError,
                        currentIndex, $"Состояние {condition.RequiredStateId} не найдено или его ключ повторяется");
                }

                if (actualValue != condition.RequiredStateValue)
                {
                    return new StepEvaluationResult(ConditionOutcome.Violation,
                        currentIndex, $"Состояние {condition.RequiredStateId}: ожидаемое {condition.RequiredStateValue}, получено {actualValue}");
                }
            }

            int nextIndex = currentIndex + 1;
            ConditionOutcome successOutcome = nextIndex == condition.ActionSequence.Count ? ConditionOutcome.Success : ConditionOutcome.Progress;

            return new StepEvaluationResult(successOutcome, nextIndex);
        }
    }