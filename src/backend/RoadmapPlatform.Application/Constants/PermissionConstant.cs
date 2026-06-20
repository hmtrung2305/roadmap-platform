namespace RoadmapPlatform.Application.Constants;

public static class PermissionConstant
{
    // =====================================================
    // SHARED AUTHENTICATED ACCOUNT PERMISSIONS
    // =====================================================
    public const string ACCOUNT_VIEW_SELF = "account.view.self";
    public const string ACCOUNT_UPDATE_SELF = "account.update.self";
    public const string AUTH_PROVIDER_VIEW_SELF = "auth_provider.view.self";
    public const string AUTH_PROVIDER_LINK_SELF = "auth_provider.link.self";
    public const string AUTH_PROVIDER_UNLINK_SELF = "auth_provider.unlink.self";
    public const string AUTH_PROVIDER_UPDATE_SELF = "auth_provider.update.self";
    public const string PROFILE_VIEW_SELF = "profile.view.self";
    public const string PROFILE_UPDATE_SELF = "profile.update.self";

    // =====================================================
    // LEARNER ACCOUNT AND PORTFOLIO PERMISSIONS
    // =====================================================
    public const string ACCOUNT_DELETE_SELF = "account.delete.self";
    public const string PORTFOLIO_VIEW_SELF = "portfolio.view.self";
    public const string PORTFOLIO_UPDATE_SELF = "portfolio.update.self";

    // =====================================================
    // LEARNER GITHUB / REPOSITORY PERMISSIONS
    // =====================================================
    public const string REPOSITORY_VIEW_SELF = "repository.view.self";
    public const string REPOSITORY_SYNC_SELF = "repository.sync.self";
    public const string REPO_INSIGHT_VIEW_SELF = "repo_insight.view.self";
    public const string REPO_INSIGHT_GENERATE_SELF = "repo_insight.generate.self";

    // =====================================================
    // LEARNER AI CREDIT AND STREAK PERMISSIONS
    // =====================================================
    public const string AI_CREDIT_VIEW_SELF = "ai_credit.view.self";
    public const string STREAK_VIEW_SELF = "streak.view.self";
    public const string STREAK_TRACK_SELF = "streak.track.self";

    // =====================================================
    // LEARNER ROADMAP PERMISSIONS
    // =====================================================
    public const string ROADMAP_VIEW_PUBLISHED = "roadmap.view.published";
    public const string ROADMAP_NODE_VIEW_PUBLISHED = "roadmap_node.view.published";
    public const string ROADMAP_ENROLLMENT_VIEW_SELF = "roadmap_enrollment.view.self";
    public const string ROADMAP_ENROLLMENT_CREATE_SELF = "roadmap_enrollment.create.self";
    public const string ROADMAP_PROGRESS_UPDATE_SELF = "roadmap_progress.update.self";

    // =====================================================
    // LEARNER LEARNING MODULE PERMISSIONS
    // =====================================================
    public const string LEARNING_MODULE_VIEW_PUBLISHED = "learning_module.view.published";
    public const string LEARNING_MODULE_ENROLLMENT_VIEW_SELF = "learning_module_enrollment.view.self";
    public const string LEARNING_MODULE_ENROLLMENT_CREATE_SELF = "learning_module_enrollment.create.self";
    public const string LEARNING_MODULE_LESSON_VIEW_ENROLLED = "learning_module_lesson.view.enrolled";
    public const string LEARNING_MODULE_PROGRESS_UPDATE_SELF = "learning_module_progress.update.self";
    public const string LEARNING_MODULE_QUIZ_ATTEMPT_VIEW_SELF = "learning_module_quiz_attempt.view.self";
    public const string LEARNING_MODULE_QUIZ_ATTEMPT_CREATE_SELF = "learning_module_quiz_attempt.create.self";
    public const string LEARNING_MODULE_QUIZ_ATTEMPT_SUBMIT_SELF = "learning_module_quiz_attempt.submit.self";
    public const string LEARNING_MODULE_CHAT_USE_ENROLLED = "learning_module_chat.use.enrolled";

    // =====================================================
    // LEARNER DISCOVERY AND ANALYSIS PERMISSIONS
    // =====================================================
    public const string CAREER_ROLE_VIEW_CATALOG = "career_role.view.catalog";
    public const string SKILL_GAP_ANALYSIS_CREATE_SELF = "skill_gap_analysis.create.self";
    public const string MARKET_PULSE_VIEW_CATALOG = "market_pulse.view.catalog";
    public const string SKILL_VIEW_CATALOG = "skill.view.catalog";

    public const string SKILL_GAP_ANALYSIS_HISTORY_VIEW_SELF = "skill_gap_analysis_history.view.self";
    public const string SKILL_GAP_ANALYSIS_HISTORY_DELETE_SELF = "skill_gap_analysis_history.delete.self";

    // =====================================================
    // ADMIN CONFIG SKILL GAP ANALYSIS PERMISSIONS
    // =====================================================
    public const string SKILL_GAP_CONFIG_VIEW_ANY = "skill_gap_config.view.any";

    public const string SKILL_GAP_CONFIG_UPDATE_ANY = "skill_gap_config.update.any";




    // =====================================================
    // COUNSELOR LEARNING MODULE OWNERSHIP PERMISSIONS
    // =====================================================
    public const string LEARNING_MODULE_VIEW_OWN = "learning_module.view.own";
    public const string LEARNING_MODULE_CREATE_OWN = "learning_module.create.own";
    public const string LEARNING_MODULE_UPDATE_OWN = "learning_module.update.own";
    public const string LEARNING_MODULE_DELETE_OWN = "learning_module.delete.own";
    public const string LEARNING_MODULE_PUBLISH_OWN = "learning_module.publish.own";
    public const string LEARNING_MODULE_ARCHIVE_OWN = "learning_module.archive.own";
    public const string LEARNING_MODULE_PREVIEW_OWN = "learning_module.preview.own";

    // =====================================================
    // COUNSELOR LESSON OWNERSHIP PERMISSIONS
    // =====================================================
    public const string LEARNING_MODULE_LESSON_CREATE_OWN = "learning_module_lesson.create.own";
    public const string LEARNING_MODULE_LESSON_UPDATE_OWN = "learning_module_lesson.update.own";
    public const string LEARNING_MODULE_LESSON_DELETE_OWN = "learning_module_lesson.delete.own";
    public const string LEARNING_MODULE_LESSON_REORDER_OWN = "learning_module_lesson.reorder.own";
    public const string LEARNING_MODULE_LESSON_REINDEX_OWN = "learning_module_lesson.reindex.own";

    // =====================================================
    // COUNSELOR QUIZ OWNERSHIP PERMISSIONS
    // =====================================================
    public const string LEARNING_MODULE_QUIZ_VIEW_OWN = "learning_module_quiz.view.own";
    public const string LEARNING_MODULE_QUIZ_UPSERT_OWN = "learning_module_quiz.upsert.own";
    public const string LEARNING_MODULE_QUIZ_QUESTION_CREATE_OWN = "learning_module_quiz_question.create.own";
    public const string LEARNING_MODULE_QUIZ_QUESTION_UPDATE_OWN = "learning_module_quiz_question.update.own";
    public const string LEARNING_MODULE_QUIZ_QUESTION_DELETE_OWN = "learning_module_quiz_question.delete.own";
    public const string LEARNING_MODULE_QUIZ_QUESTION_REORDER_OWN = "learning_module_quiz_question.reorder.own";

    // =====================================================
    // ADMIN USER GOVERNANCE PERMISSIONS
    // =====================================================
    public const string USER_VIEW_ANY = "user.view.any";
    public const string USER_UPDATE_ANY = "user.update.any";
    public const string USER_SUSPEND_ANY = "user.suspend.any";
    public const string USER_RESTORE_ANY = "user.restore.any";
    public const string USER_DELETE_ANY = "user.delete.any";

    // =====================================================
    // ADMIN ROLE GOVERNANCE PERMISSIONS
    // =====================================================
    public const string ROLE_VIEW_ANY = "role.view.any";
    public const string ROLE_CREATE_ANY = "role.create.any";
    public const string ROLE_UPDATE_ANY = "role.update.any";
    public const string ROLE_DELETE_ANY = "role.delete.any";

    // =====================================================
    // ADMIN PERMISSION GOVERNANCE PERMISSIONS
    // =====================================================
    public const string PERMISSION_VIEW_ANY = "permission.view.any";
    public const string PERMISSION_CREATE_ANY = "permission.create.any";
    public const string PERMISSION_UPDATE_ANY = "permission.update.any";
    public const string PERMISSION_DELETE_ANY = "permission.delete.any";

    // =====================================================
    // ADMIN ROLE-PERMISSION AND USER-ROLE GOVERNANCE PERMISSIONS
    // =====================================================
    public const string ROLE_PERMISSION_VIEW_ANY = "role_permission.view.any";
    public const string ROLE_PERMISSION_ASSIGN_ANY = "role_permission.assign.any";
    public const string ROLE_PERMISSION_REVOKE_ANY = "role_permission.revoke.any";
    public const string USER_ROLE_VIEW_ANY = "user_role.view.any";
    public const string USER_ROLE_ASSIGN_ANY = "user_role.assign.any";
    public const string USER_ROLE_REVOKE_ANY = "user_role.revoke.any";

    // =====================================================
    // ADMIN SYSTEM GOVERNANCE PERMISSIONS
    // =====================================================
    public const string SYSTEM_HEALTH_VIEW_ANY = "system_health.view.any";

    // =====================================================
    // SKILL PERMISSIONS
    // =====================================================
    public const string SKILL_VIEW_ANY = "skill.view.any";
    public const string SKILL_CREATE_ANY = "skill.create.any";
    public const string SKILL_UPDATE_ANY = "skill.update.any";
    public const string SKILL_DELETE_ANY = "skill.delete.any";

    // =====================================================
    // ANALYSIS PERMISSIONS
    // =====================================================


    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        ACCOUNT_VIEW_SELF,
        ACCOUNT_UPDATE_SELF,
        AUTH_PROVIDER_VIEW_SELF,
        AUTH_PROVIDER_LINK_SELF,
        AUTH_PROVIDER_UNLINK_SELF,
        AUTH_PROVIDER_UPDATE_SELF,
        PROFILE_VIEW_SELF,
        PROFILE_UPDATE_SELF,
        ACCOUNT_DELETE_SELF,
        PORTFOLIO_VIEW_SELF,
        PORTFOLIO_UPDATE_SELF,
        REPOSITORY_VIEW_SELF,
        REPOSITORY_SYNC_SELF,
        REPO_INSIGHT_VIEW_SELF,
        REPO_INSIGHT_GENERATE_SELF,
        AI_CREDIT_VIEW_SELF,
        STREAK_VIEW_SELF,
        STREAK_TRACK_SELF,
        ROADMAP_VIEW_PUBLISHED,
        ROADMAP_NODE_VIEW_PUBLISHED,
        ROADMAP_ENROLLMENT_VIEW_SELF,
        ROADMAP_ENROLLMENT_CREATE_SELF,
        ROADMAP_PROGRESS_UPDATE_SELF,
        LEARNING_MODULE_VIEW_PUBLISHED,
        LEARNING_MODULE_ENROLLMENT_VIEW_SELF,
        LEARNING_MODULE_ENROLLMENT_CREATE_SELF,
        LEARNING_MODULE_LESSON_VIEW_ENROLLED,
        LEARNING_MODULE_PROGRESS_UPDATE_SELF,
        LEARNING_MODULE_QUIZ_ATTEMPT_VIEW_SELF,
        LEARNING_MODULE_QUIZ_ATTEMPT_CREATE_SELF,
        LEARNING_MODULE_QUIZ_ATTEMPT_SUBMIT_SELF,
        LEARNING_MODULE_CHAT_USE_ENROLLED,
        CAREER_ROLE_VIEW_CATALOG,
        SKILL_GAP_ANALYSIS_CREATE_SELF,
        MARKET_PULSE_VIEW_CATALOG,
        SKILL_VIEW_CATALOG,
        SKILL_VIEW_ANY,
        LEARNING_MODULE_VIEW_OWN,
        LEARNING_MODULE_CREATE_OWN,
        LEARNING_MODULE_UPDATE_OWN,
        LEARNING_MODULE_DELETE_OWN,
        LEARNING_MODULE_PUBLISH_OWN,
        LEARNING_MODULE_ARCHIVE_OWN,
        LEARNING_MODULE_PREVIEW_OWN,
        LEARNING_MODULE_LESSON_CREATE_OWN,
        LEARNING_MODULE_LESSON_UPDATE_OWN,
        LEARNING_MODULE_LESSON_DELETE_OWN,
        LEARNING_MODULE_LESSON_REORDER_OWN,
        LEARNING_MODULE_LESSON_REINDEX_OWN,
        LEARNING_MODULE_QUIZ_VIEW_OWN,
        LEARNING_MODULE_QUIZ_UPSERT_OWN,
        LEARNING_MODULE_QUIZ_QUESTION_CREATE_OWN,
        LEARNING_MODULE_QUIZ_QUESTION_UPDATE_OWN,
        LEARNING_MODULE_QUIZ_QUESTION_DELETE_OWN,
        LEARNING_MODULE_QUIZ_QUESTION_REORDER_OWN,
        USER_VIEW_ANY,
        USER_UPDATE_ANY,
        USER_SUSPEND_ANY,
        USER_RESTORE_ANY,
        USER_DELETE_ANY,
        ROLE_VIEW_ANY,
        ROLE_CREATE_ANY,
        ROLE_UPDATE_ANY,
        ROLE_DELETE_ANY,
        PERMISSION_VIEW_ANY,
        PERMISSION_CREATE_ANY,
        PERMISSION_UPDATE_ANY,
        PERMISSION_DELETE_ANY,
        ROLE_PERMISSION_VIEW_ANY,
        ROLE_PERMISSION_ASSIGN_ANY,
        ROLE_PERMISSION_REVOKE_ANY,
        USER_ROLE_VIEW_ANY,
        USER_ROLE_ASSIGN_ANY,
        USER_ROLE_REVOKE_ANY,
        SYSTEM_HEALTH_VIEW_ANY,
        SKILL_CREATE_ANY,
        SKILL_UPDATE_ANY,
        SKILL_DELETE_ANY
    };
}
