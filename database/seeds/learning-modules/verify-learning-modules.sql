-- Learning Module seed verification queries.

SELECT COUNT(*) AS skill_module_count FROM public.skill_module;
SELECT COUNT(*) AS skill_module_lesson_count FROM public.skill_module_lesson;
SELECT COUNT(*) AS skill_module_quiz_count FROM public.skill_module_quiz;
SELECT COUNT(*) AS skill_module_quiz_question_count FROM public.skill_module_quiz_question;
SELECT COUNT(*) AS skill_module_quiz_option_count FROM public.skill_module_quiz_option;

SELECT status, COUNT(*) AS count
FROM public.skill_module
GROUP BY status
ORDER BY status;

SELECT indexing_status, COUNT(*) AS count
FROM public.skill_module_lesson
GROUP BY indexing_status
ORDER BY indexing_status;

SELECT module.skill_module_id, module.title, COUNT(lesson.skill_module_lesson_id) AS lesson_count
FROM public.skill_module module
LEFT JOIN public.skill_module_lesson lesson
    ON lesson.skill_module_id = module.skill_module_id
GROUP BY module.skill_module_id, module.title
HAVING COUNT(lesson.skill_module_lesson_id) = 0;

DO $$
DECLARE
    invalid_modules text;
BEGIN
    SELECT string_agg(invalid.title, ', ' ORDER BY invalid.title)
    INTO invalid_modules
    FROM (
        SELECT module.title
        FROM public.skill_module module
        LEFT JOIN public.skill_module_quiz quiz
            ON quiz.skill_module_id = module.skill_module_id
        LEFT JOIN public.skill_module_quiz_question question
            ON question.skill_module_quiz_id = quiz.skill_module_quiz_id
        WHERE module.status = 'published'
        GROUP BY module.skill_module_id, module.title
        HAVING COUNT(question.skill_module_quiz_question_id) < 10
    ) invalid;

    IF invalid_modules IS NOT NULL THEN
        RAISE EXCEPTION 'Published learning modules need at least 10 quiz questions: %', invalid_modules;
    END IF;
END $$;
