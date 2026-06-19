export const LEARNING_MODULE_AUTHORING_LIMITS = {
  maxModuleTitleLength: 200,
  maxModuleDescriptionLength: 2000,
  maxMarkdownFileBytes: 2 * 1024 * 1024,
  maxBulkMarkdownUploadBytes: 10 * 1024 * 1024,
  maxBulkLessonCount: 25,
  maxLessonTitleLength: 200,
  maxLessonSummaryLength: 1000,
  maxEstimatedHours: 999.99,
};

export function formatFileSize(bytes) {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${Math.round(bytes / 1024)} KB`;

  return `${Math.round((bytes / (1024 * 1024)) * 10) / 10} MB`;
}
