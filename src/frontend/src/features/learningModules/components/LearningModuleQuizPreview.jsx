import { ModuleBadge, ModuleCard, ModuleEmptyState } from "./learningModuleUi";

function getSortedQuestions(quiz) {
  return (quiz?.questions || [])
    .slice()
    .sort((a, b) => (a.orderIndex || 0) - (b.orderIndex || 0));
}

function getSortedOptions(question) {
  return (question?.options || [])
    .slice()
    .sort((a, b) => (a.orderIndex || 0) - (b.orderIndex || 0));
}

export default function LearningModuleQuizPreview({ quiz }) {
  if (!quiz) {
    return (
      <ModuleEmptyState title="No quiz yet">
        Create a quiz to preview the final assessment.
      </ModuleEmptyState>
    );
  }

  const questions = getSortedQuestions(quiz);

  return (
    <div className="space-y-4">
      <div className="rounded-xl border border-[#B9D8CC] bg-[#F7F1E8]/70 p-4">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <ModuleBadge tone="green">Final quiz</ModuleBadge>
            <h2 className="mt-2 text-xl font-extrabold text-[#18332D]">
              {quiz.title || "Untitled quiz"}
            </h2>
            {quiz.description && (
              <p className="mt-2 text-sm font-semibold leading-6 text-slate-700">
                {quiz.description}
              </p>
            )}
          </div>

          <div className="flex flex-wrap gap-2">
            <ModuleBadge tone="slate">
              {questions.length} {questions.length === 1 ? "question" : "questions"}
            </ModuleBadge>
            <ModuleBadge tone="amber">
              Pass {quiz.passingScorePercent ?? 0}%
            </ModuleBadge>
            <ModuleBadge tone="slate">
              {quiz.maxAttempts ? `${quiz.maxAttempts} attempts/day` : "Unlimited attempts"}
            </ModuleBadge>
          </div>
        </div>
      </div>

      {questions.length === 0 ? (
        <ModuleEmptyState title="No questions yet">
          Add questions in the Quiz tab to preview them here.
        </ModuleEmptyState>
      ) : (
        <div className="space-y-3">
          {questions.map((question, index) => {
            const options = getSortedOptions(question);

            return (
              <ModuleCard key={question.skillModuleQuizQuestionId || index} className="p-4">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div className="min-w-0 flex-1">
                    <div className="text-xs font-extrabold uppercase tracking-[0.12em] text-[#1F6F5F]">
                      Question {index + 1}
                    </div>
                    <h3 className="mt-1 text-base font-extrabold leading-6 text-[#18332D]">
                      {question.questionText || "Untitled question"}
                    </h3>
                  </div>

                  <div className="flex flex-wrap gap-2">
                    <ModuleBadge tone="slate">
                      {question.points || 1} {(question.points || 1) === 1 ? "point" : "points"}
                    </ModuleBadge>
                    <ModuleBadge tone="slate">
                      {question.questionType === "multiple_choice" ? "Multiple choice" : "Single choice"}
                    </ModuleBadge>
                  </div>
                </div>

                <div className="mt-4 space-y-2">
                  {options.map((option, optionIndex) => (
                    <div
                      key={option.skillModuleQuizOptionId || optionIndex}
                      className={`flex items-start gap-3 rounded-lg border px-3 py-2.5 text-sm font-semibold leading-6 ${
                        option.isCorrect
                          ? "border-[#6FCF97] bg-[#6FCF97]/15 text-[#18332D]"
                          : "border-[#B9D8CC] bg-white text-slate-700"
                      }`}
                    >
                      <span
                        className={`mt-0.5 inline-grid h-6 w-6 shrink-0 place-items-center rounded-full text-xs font-extrabold ${
                          option.isCorrect
                            ? "bg-[#1F6F5F] text-white"
                            : "bg-[#F7F1E8] text-slate-600"
                        }`}
                      >
                        {String.fromCharCode(65 + optionIndex)}
                      </span>

                      <div className="min-w-0 flex-1">
                        <div>{option.optionText || "Untitled option"}</div>
                        {option.explanation && (
                          <div className="mt-1 text-xs font-semibold leading-5 text-slate-600">
                            {option.explanation}
                          </div>
                        )}
                      </div>

                      {option.isCorrect && (
                        <ModuleBadge tone="green" className="shrink-0">
                          Correct
                        </ModuleBadge>
                      )}
                    </div>
                  ))}
                </div>

                {question.explanation && (
                  <div className="mt-3 rounded-lg border border-[#B9D8CC] bg-[#F7F1E8] px-3 py-2 text-sm font-semibold leading-6 text-slate-700">
                    <span className="font-extrabold text-[#18332D]">Explanation:</span>{" "}
                    {question.explanation}
                  </div>
                )}
              </ModuleCard>
            );
          })}
        </div>
      )}
    </div>
  );
}
