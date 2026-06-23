export const PERMISSIONS = Object.freeze({
  ACCOUNT_VIEW_SELF: "account.view.self",
  ACCOUNT_UPDATE_SELF: "account.update.self",

  AUTH_PROVIDER_VIEW_SELF: "auth_provider.view.self",
  AUTH_PROVIDER_LINK_SELF: "auth_provider.link.self",
  AUTH_PROVIDER_UNLINK_SELF: "auth_provider.unlink.self",
  AUTH_PROVIDER_UPDATE_SELF: "auth_provider.update.self",

  PROFILE_VIEW_SELF: "profile.view.self",
  PROFILE_UPDATE_SELF: "profile.update.self",

  PORTFOLIO_VIEW_SELF: "portfolio.view.self",
  PORTFOLIO_UPDATE_SELF: "portfolio.update.self",

  REPOSITORY_VIEW_SELF: "repository.view.self",
  REPOSITORY_SYNC_SELF: "repository.sync.self",
  REPO_INSIGHT_GENERATE_SELF: "repo_insight.generate.self",

  AI_CREDIT_VIEW_SELF: "ai_credit.view.self",

  STREAK_VIEW_SELF: "streak.view.self",
  STREAK_TRACK_SELF: "streak.track.self",

  CAREER_ROLE_VIEW_CATALOG: "career_role.view.catalog",
  MARKET_PULSE_VIEW_CATALOG: "market_pulse.view.catalog",
  SKILL_VIEW_CATALOG: "skill.view.catalog",
  SKILL_GAP_ANALYSIS_CREATE_SELF: "skill_gap_analysis.create.self",
  SKILL_GAP_ANALYSIS_HISTORY_VIEW_SELF: "skill_gap_analysis_history.view.self",
  SKILL_GAP_ANALYSIS_HISTORY_DELETE_SELF: "skill_gap_analysis_history.delete.self",
  SKILL_GAP_CONFIG_VIEW_ANY: "skill_gap_config.view.any",
  SKILL_GAP_CONFIG_UPDATE_ANY: "skill_gap_config.update.any",

  ROADMAP_VIEW_PUBLISHED: "roadmap.view.published",
  ROADMAP_NODE_VIEW_PUBLISHED: "roadmap_node.view.published",
  ROADMAP_ENROLLMENT_VIEW_SELF: "roadmap_enrollment.view.self",
  ROADMAP_ENROLLMENT_CREATE_SELF: "roadmap_enrollment.create.self",
  ROADMAP_PROGRESS_UPDATE_SELF: "roadmap_progress.update.self",

  LEARNING_MODULE_VIEW_PUBLISHED: "learning_module.view.published",
  LEARNING_MODULE_ENROLLMENT_VIEW_SELF: "learning_module_enrollment.view.self",
  LEARNING_MODULE_ENROLLMENT_CREATE_SELF: "learning_module_enrollment.create.self",
  LEARNING_MODULE_LESSON_VIEW_ENROLLED: "learning_module_lesson.view.enrolled",
  LEARNING_MODULE_PROGRESS_UPDATE_SELF: "learning_module_progress.update.self",
  LEARNING_MODULE_QUIZ_ATTEMPT_VIEW_SELF: "learning_module_quiz_attempt.view.self",
  LEARNING_MODULE_QUIZ_ATTEMPT_CREATE_SELF: "learning_module_quiz_attempt.create.self",
  LEARNING_MODULE_QUIZ_ATTEMPT_SUBMIT_SELF: "learning_module_quiz_attempt.submit.self",
  LEARNING_MODULE_CHAT_USE_ENROLLED: "learning_module_chat.use.enrolled",

  LEARNING_MODULE_VIEW_OWN: "learning_module.view.own",
  LEARNING_MODULE_CREATE_OWN: "learning_module.create.own",
  LEARNING_MODULE_UPDATE_OWN: "learning_module.update.own",
  LEARNING_MODULE_DELETE_OWN: "learning_module.delete.own",
  LEARNING_MODULE_PUBLISH_OWN: "learning_module.publish.own",
  LEARNING_MODULE_ARCHIVE_OWN: "learning_module.archive.own",
  LEARNING_MODULE_PREVIEW_OWN: "learning_module.preview.own",

  LEARNING_MODULE_LESSON_CREATE_OWN: "learning_module_lesson.create.own",
  LEARNING_MODULE_LESSON_UPDATE_OWN: "learning_module_lesson.update.own",
  LEARNING_MODULE_LESSON_DELETE_OWN: "learning_module_lesson.delete.own",
  LEARNING_MODULE_LESSON_REORDER_OWN: "learning_module_lesson.reorder.own",
  LEARNING_MODULE_LESSON_REINDEX_OWN: "learning_module_lesson.reindex.own",

  LEARNING_MODULE_QUIZ_UPSERT_OWN: "learning_module_quiz.upsert.own",
  LEARNING_MODULE_QUIZ_QUESTION_CREATE_OWN: "learning_module_quiz_question.create.own",
  LEARNING_MODULE_QUIZ_QUESTION_UPDATE_OWN: "learning_module_quiz_question.update.own",
  LEARNING_MODULE_QUIZ_QUESTION_DELETE_OWN: "learning_module_quiz_question.delete.own",
  LEARNING_MODULE_QUIZ_QUESTION_REORDER_OWN: "learning_module_quiz_question.reorder.own",

  USER_VIEW_ANY: "user.view.any",
  USER_UPDATE_ANY: "user.update.any",
  USER_SUSPEND_ANY: "user.suspend.any",
  USER_RESTORE_ANY: "user.restore.any",
  USER_DELETE_ANY: "user.delete.any",

  USER_ROLE_VIEW_ANY: "user_role.view.any",
  USER_ROLE_ASSIGN_ANY: "user_role.assign.any",
  USER_ROLE_REVOKE_ANY: "user_role.revoke.any",

  ROLE_VIEW_ANY: "role.view.any",
  ROLE_CREATE_ANY: "role.create.any",
  ROLE_UPDATE_ANY: "role.update.any",
  ROLE_DELETE_ANY: "role.delete.any",

  ROLE_PERMISSION_VIEW_ANY: "role_permission.view.any",
  ROLE_PERMISSION_ASSIGN_ANY: "role_permission.assign.any",
  ROLE_PERMISSION_REVOKE_ANY: "role_permission.revoke.any",

  PERMISSION_VIEW_ANY: "permission.view.any",
  PERMISSION_CREATE_ANY: "permission.create.any",
  PERMISSION_UPDATE_ANY: "permission.update.any",
  PERMISSION_DELETE_ANY: "permission.delete.any",

  SKILL_VIEW_ANY: "skill.view.any",
  SKILL_CREATE_ANY: "skill.create.any",
  SKILL_UPDATE_ANY: "skill.update.any",
  SKILL_DELETE_ANY: "skill.delete.any",

  SYSTEM_HEALTH_VIEW_ANY: "system_health.view.any",
});

export const LEARNER_SURFACE_PERMISSIONS = Object.freeze([
  PERMISSIONS.ROADMAP_VIEW_PUBLISHED,
  PERMISSIONS.LEARNING_MODULE_VIEW_PUBLISHED,
  PERMISSIONS.PORTFOLIO_VIEW_SELF,
]);

export const CONTENT_MANAGER_SURFACE_PERMISSIONS = Object.freeze([
  PERMISSIONS.LEARNING_MODULE_VIEW_OWN,
  PERMISSIONS.LEARNING_MODULE_CREATE_OWN,
  PERMISSIONS.SKILL_GAP_CONFIG_VIEW_ANY,
]);

export const ADMIN_SURFACE_PERMISSIONS = Object.freeze([
  PERMISSIONS.USER_VIEW_ANY,
  PERMISSIONS.ROLE_VIEW_ANY,
  PERMISSIONS.PERMISSION_VIEW_ANY,
  PERMISSIONS.SKILL_VIEW_ANY,
  PERMISSIONS.SKILL_CREATE_ANY,
  PERMISSIONS.SYSTEM_HEALTH_VIEW_ANY,
]);
