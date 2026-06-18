export const HEADER_HEIGHT = 72;
export const PAGE_GAP = 20;
export const SIDEBAR_MIN_WIDTH = 240;
export const SIDEBAR_MAX_WIDTH = 420;
export const CHAT_MIN_WIDTH = 320;
export const CHAT_MAX_WIDTH = 640;

export function areAllLessonsCompleted(module) {
  const lessons = module?.lessons || [];
  const lessonProgress = module?.enrollment?.lessonProgress || {};

  if (lessons.length === 0) {
    return true;
  }

  return lessons.every(
    (lesson) => lessonProgress[lesson.skillModuleLessonId] === "completed",
  );
}

export function isSubmittedToday(attempt) {
  if (attempt?.status !== "submitted" || !attempt.submittedAt) {
    return false;
  }

  const submittedAt = new Date(attempt.submittedAt);
  const now = new Date();

  return (
    submittedAt.getFullYear() === now.getFullYear()
    && submittedAt.getMonth() === now.getMonth()
    && submittedAt.getDate() === now.getDate()
  );
}
