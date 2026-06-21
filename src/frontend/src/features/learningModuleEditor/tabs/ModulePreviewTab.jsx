/* eslint-disable react-hooks/set-state-in-effect, react-hooks/exhaustive-deps */
import { useEffect, useState } from "react";
import { contentManagerLearningModuleApi } from "../../../api/learningModuleApi";
import LearningModuleLessonReader from "../../../components/learningModules/LearningModuleLessonReader";
import LearningModuleQuizPreview from "../../../components/learningModules/LearningModuleQuizPreview";
import { inputClass, ModuleBadge, ModuleButton, ModuleCard } from "../../../components/learningModules/learningModuleUi";

export default function ModulePreviewTab({ moduleId, detail }) {
  return <PreviewShell moduleId={moduleId} detail={detail} />;
}


function PreviewShell({ moduleId, detail }) {
  const lessons = detail.lessons || [];
  const [activeLessonId, setActiveLessonId] = useState(lessons[0]?.skillModuleLessonId || (detail.quiz ? "quiz" : null));
  const [preview, setPreview] = useState(null);

  useEffect(() => {
    setActiveLessonId((current) => {
      if (lessons.some((lesson) => lesson.skillModuleLessonId === current)) {
        return current;
      }

      if (current === "quiz" && detail.quiz) {
        return current;
      }

      return lessons[0]?.skillModuleLessonId || (detail.quiz ? "quiz" : null);
    });
  }, [lessons, detail.quiz]);

  useEffect(() => {
    let ignore = false;

    async function loadPreview() {
      if (!moduleId || !activeLessonId || activeLessonId === "quiz") {
        setPreview(null);
        return;
      }

      try {
        const data = await contentManagerLearningModuleApi.getLessonPreview(moduleId, activeLessonId);
        if (!ignore) setPreview(data);
      } catch {
        if (!ignore) setPreview(null);
      }
    }

    loadPreview();

    return () => {
      ignore = true;
    };
  }, [moduleId, activeLessonId]);

  const module = detail.module;
  const isQuizActive = activeLessonId === "quiz";

  return (
    <div className="grid min-h-[680px] gap-4 lg:grid-cols-[260px_minmax(0,1fr)_320px]">
      <ModuleCard className="overflow-hidden">
        <div className="border-b border-[#B9D8CC] p-4">
          <h1 className="text-lg font-extrabold text-[#18332D]">{module.title}</h1>
          <div className="mt-2 text-sm font-bold text-[#1F6F5F]">{module.skillName}</div>
        </div>
        <div className="space-y-1 p-2">
          {detail.lessons.length === 0 && (
            <div className="rounded-lg bg-[#F7F1E8] px-3 py-3 text-sm font-semibold text-slate-700">
              No lessons uploaded yet.
            </div>
          )}
          {detail.lessons.map((lesson) => (
            <button
              key={lesson.skillModuleLessonId}
              type="button"
              onClick={() => setActiveLessonId(lesson.skillModuleLessonId)}
              className={`flex w-full items-center gap-2 rounded-lg px-3 py-2.5 text-left text-sm font-bold ${
                activeLessonId === lesson.skillModuleLessonId
                  ? "bg-[#6FCF97]/20 text-[#1F6F5F]"
                  : "text-slate-700 hover:bg-[#F7F1E8]"
              }`}
            >
              <span>{lesson.orderIndex}</span>
              {lesson.title}
            </button>
          ))}

          {detail.quiz && (
            <button
              type="button"
              onClick={() => setActiveLessonId("quiz")}
              className={`mt-2 flex w-full items-center justify-between gap-2 rounded-lg px-3 py-2.5 text-left text-sm font-bold ${
                isQuizActive
                  ? "bg-[#6FCF97]/20 text-[#1F6F5F]"
                  : "text-slate-700 hover:bg-[#F7F1E8]"
              }`}
            >
              <span>Final quiz</span>
              <ModuleBadge tone="slate">
                {detail.quiz.questions?.length || 0} questions
              </ModuleBadge>
            </button>
          )}
        </div>
      </ModuleCard>

      <ModuleCard className="overflow-hidden">
        <div className="border-b border-[#B9D8CC] px-5 py-4">
          <h2 className="text-lg font-extrabold text-[#18332D]">
            {isQuizActive ? "Final quiz preview" : preview?.title || "Module preview"}
          </h2>
        </div>
        <div className="max-h-[620px] overflow-auto p-6">
          {isQuizActive ? (
            <LearningModuleQuizPreview quiz={detail.quiz} />
          ) : (
            <LearningModuleLessonReader
              markdown={preview?.markdown}
              embedded
              emptyTitle="Empty preview"
              emptyMessage="Upload lessons to preview the learner reading experience."
            />
          )}
        </div>
      </ModuleCard>

      <ModuleCard className="flex min-h-0 flex-col overflow-hidden">
        <div className="border-b border-[#B9D8CC] px-4 py-3">
          <h2 className="text-sm font-extrabold text-[#18332D]">Module chat preview</h2>
        </div>
        <div className="flex-1 space-y-3 overflow-auto p-4">
          <div className="rounded-lg bg-[#F7F1E8] p-3 text-sm font-semibold leading-6 text-slate-700">
            Chat becomes available when learners start the published module.
          </div>
        </div>
        <div className="border-t border-[#B9D8CC] p-3">
          <textarea disabled className={`${inputClass} min-h-20 resize-none bg-slate-50`} placeholder="Preview only" />
          <ModuleButton className="mt-2 w-full" disabled>Send</ModuleButton>
        </div>
      </ModuleCard>
    </div>
  );
}


