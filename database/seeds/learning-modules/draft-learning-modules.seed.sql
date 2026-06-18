-- =========================================================
-- Draft Learning Module Seed: Complete Programming Drafts
-- =========================================================
-- Seeds four complete draft modules with lessons and quizzes:
-- TypeScript Basics, SQL Basics, Git Basics, C# Basics.
-- These modules intentionally remain status = 'draft' so content managers can test
-- draft management, editing, previewing, publishing, and RAG indexing workflows.
-- Lesson markdown files must also be copied/uploaded to storage using the object keys below.

BEGIN;

DO $$
DECLARE
    missing_skills text;
BEGIN
    SELECT string_agg(required.slug, ', ' ORDER BY required.slug)
    INTO missing_skills
    FROM (VALUES ('typescript'), ('sql'), ('git'), ('csharp')) AS required(slug)
    WHERE NOT EXISTS (
        SELECT 1 FROM public.skill s WHERE s.slug = required.slug
    );

    IF missing_skills IS NOT NULL THEN
        RAISE EXCEPTION 'Missing required skills for draft learning module seed: %. Run shared-skills.seed.sql first.', missing_skills;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM public."user" WHERE username_normalized = 'contentmanager') THEN
        RAISE EXCEPTION 'Missing contentmanager user for draft learning module seed. Run dev-users.seed.sql first or update created_by_user_id in this seed.';
    END IF;
END $$;

-- Converge these draft demo modules to the canonical seed state.
-- Intended for seed/dev data only. Do not rerun on production data with real authoring history.
DELETE FROM public.skill_module
WHERE slug IN ('typescript-basics', 'sql-basics', 'git-basics', 'csharp-basics');

INSERT INTO public.skill_module
(skill_module_id, skill_id, title, slug, description, difficulty_level, estimated_hours, status, created_by_user_id, published_at, archived_at, metadata, created_at, updated_at)
VALUES
(
    '8ce0ddf3-172f-5fe6-b6a6-220a64f34a82',
    (SELECT skill_id FROM public.skill WHERE slug = 'typescript'),
    'TypeScript Basics',
    'typescript-basics',
    'Learn TypeScript fundamentals for safer JavaScript, including annotations, interfaces, narrowing, generics, and practical compiler feedback.',
    'beginner',
    6.50,
    'draft',
    (SELECT user_id FROM public."user" WHERE username_normalized = 'contentmanager' LIMIT 1),
    NULL,
    NULL,
    '{"seed":true,"seedVersion":"draft-learning-modules-v1-detailed","draftComplete":true}'::jsonb,
    now(),
    now()
),
(
    '2c6b46a1-e0c8-51e3-9717-b8d823bc6229',
    (SELECT skill_id FROM public.skill WHERE slug = 'sql'),
    'SQL Basics',
    'sql-basics',
    'Learn practical SQL querying with selection, filtering, joins, aggregation, grouping, and safe result validation.',
    'beginner',
    6.50,
    'draft',
    (SELECT user_id FROM public."user" WHERE username_normalized = 'contentmanager' LIMIT 1),
    NULL,
    NULL,
    '{"seed":true,"seedVersion":"draft-learning-modules-v1-detailed","draftComplete":true}'::jsonb,
    now(),
    now()
),
(
    '86d77061-3414-500b-bc93-f6469dce806b',
    (SELECT skill_id FROM public.skill WHERE slug = 'git'),
    'Git Basics',
    'git-basics',
    'Learn essential Git workflows: commits, branches, status checks, diffs, merging, and safe collaboration habits.',
    'beginner',
    5.50,
    'draft',
    (SELECT user_id FROM public."user" WHERE username_normalized = 'contentmanager' LIMIT 1),
    NULL,
    NULL,
    '{"seed":true,"seedVersion":"draft-learning-modules-v1-detailed","draftComplete":true}'::jsonb,
    now(),
    now()
),
(
    '773ad089-8e61-5850-afa4-8cafd371acbf',
    (SELECT skill_id FROM public.skill WHERE slug = 'csharp'),
    'C# Basics',
    'csharp-basics',
    'Learn C# fundamentals including types, control flow, classes, collections, exceptions, and asynchronous method basics.',
    'beginner',
    7.00,
    'draft',
    (SELECT user_id FROM public."user" WHERE username_normalized = 'contentmanager' LIMIT 1),
    NULL,
    NULL,
    '{"seed":true,"seedVersion":"draft-learning-modules-v1-detailed","draftComplete":true}'::jsonb,
    now(),
    now()
);

INSERT INTO public.skill_module_lesson
(skill_module_lesson_id, skill_module_id, title, slug, summary, order_index, estimated_hours, markdown_file_key, markdown_file_name, content_hash, content_size_bytes, content_version, indexing_status, indexed_at, indexing_error, created_at, updated_at)
VALUES
(
    'fee07e4e-a75b-5f65-970b-f86e978328a0',
    '8ce0ddf3-172f-5fe6-b6a6-220a64f34a82',
    'TypeScript Setup and Type Annotations',
    'typescript-setup-and-type-annotations',
    'Set up TypeScript and use annotations to describe values clearly.',
    1,
    0.90,
    'learning-modules/8ce0ddf3-172f-5fe6-b6a6-220a64f34a82/lessons/fee07e4e-a75b-5f65-970b-f86e978328a0-01-typescript-setup-and-type-annotations.md',
    '01-typescript-setup-and-type-annotations.md',
    'e78b457d719cceae6da60519fe709c43d0b90dacf7b9fa3430a1e89a98c2fb52',
    3531,
    1,
    'pending',
    NULL,
    NULL,
    now(),
    now()
),
(
    '6e3e6765-036b-503d-8564-a74c6a089267',
    '8ce0ddf3-172f-5fe6-b6a6-220a64f34a82',
    'Objects, Interfaces, and Type Aliases',
    'objects-interfaces-and-type-aliases',
    'Model object shapes with interfaces and type aliases.',
    2,
    1.10,
    'learning-modules/8ce0ddf3-172f-5fe6-b6a6-220a64f34a82/lessons/6e3e6765-036b-503d-8564-a74c6a089267-02-objects-interfaces-and-type-aliases.md',
    '02-objects-interfaces-and-type-aliases.md',
    '3f9b372ea24d0928d90400589fab9fe2229ce33d1f41ad14a31b1f192268668d',
    3499,
    1,
    'pending',
    NULL,
    NULL,
    now(),
    now()
),
(
    'f32b0d20-5ed4-5928-a602-c3cdb9749dd4',
    '8ce0ddf3-172f-5fe6-b6a6-220a64f34a82',
    'Union Types and Narrowing',
    'union-types-and-narrowing',
    'Use union types safely with guards and narrowing.',
    3,
    1.00,
    'learning-modules/8ce0ddf3-172f-5fe6-b6a6-220a64f34a82/lessons/f32b0d20-5ed4-5928-a602-c3cdb9749dd4-03-union-types-and-narrowing.md',
    '03-union-types-and-narrowing.md',
    'e43759bef7a63ddcaef54e90cdf0a0c0c31542e0bc38784c52bf0cf02043e70e',
    3455,
    1,
    'pending',
    NULL,
    NULL,
    now(),
    now()
),
(
    '7b3ab154-1ff7-5eab-976a-1a45ebb1acc3',
    '8ce0ddf3-172f-5fe6-b6a6-220a64f34a82',
    'Generics and Reusable Functions',
    'generics-and-reusable-functions',
    'Write reusable functions while preserving type information.',
    4,
    1.20,
    'learning-modules/8ce0ddf3-172f-5fe6-b6a6-220a64f34a82/lessons/7b3ab154-1ff7-5eab-976a-1a45ebb1acc3-04-generics-and-reusable-functions.md',
    '04-generics-and-reusable-functions.md',
    '9cab3743cd7a1d844be9c9bb6ca9fbfc0e4999dfec6f10ec31ccc6ceb74a077a',
    3484,
    1,
    'pending',
    NULL,
    NULL,
    now(),
    now()
),
(
    '746ad43b-030d-5b2c-b361-b46aff1348db',
    '2c6b46a1-e0c8-51e3-9717-b8d823bc6229',
    'Selecting and Filtering Data',
    'selecting-and-filtering-data',
    'Use SELECT, WHERE, ORDER BY, and LIMIT to inspect data.',
    1,
    0.90,
    'learning-modules/2c6b46a1-e0c8-51e3-9717-b8d823bc6229/lessons/746ad43b-030d-5b2c-b361-b46aff1348db-01-selecting-and-filtering-data.md',
    '01-selecting-and-filtering-data.md',
    'd300ad8fc306a3772f77947942f9418e36bee194e0a3dd78d987414a96f8b941',
    3400,
    1,
    'pending',
    NULL,
    NULL,
    now(),
    now()
),
(
    'd70b5c9d-6b12-5141-b45d-1ce00a0e3c19',
    '2c6b46a1-e0c8-51e3-9717-b8d823bc6229',
    'Joining Related Tables',
    'joining-related-tables',
    'Combine rows from related tables with joins and key relationships.',
    2,
    1.10,
    'learning-modules/2c6b46a1-e0c8-51e3-9717-b8d823bc6229/lessons/d70b5c9d-6b12-5141-b45d-1ce00a0e3c19-02-joining-related-tables.md',
    '02-joining-related-tables.md',
    '90c7704d63ca3dc7e578d09dca345fc0530909efb25fb8aac305eb8ca9c4d434',
    3395,
    1,
    'pending',
    NULL,
    NULL,
    now(),
    now()
),
(
    '9ef8e156-cd31-5239-9383-047df32fc7b9',
    '2c6b46a1-e0c8-51e3-9717-b8d823bc6229',
    'Grouping and Aggregation',
    'grouping-and-aggregation',
    'Summarize data with GROUP BY, aggregates, and HAVING.',
    3,
    1.10,
    'learning-modules/2c6b46a1-e0c8-51e3-9717-b8d823bc6229/lessons/9ef8e156-cd31-5239-9383-047df32fc7b9-03-grouping-and-aggregation.md',
    '03-grouping-and-aggregation.md',
    '6c64a590a3dc9daee3ac0fae8181bb099ee830d487f80a9ec2e0a18e405be9bf',
    3365,
    1,
    'pending',
    NULL,
    NULL,
    now(),
    now()
),
(
    '4b3dfb49-3265-5109-aab4-0a7095efb844',
    '2c6b46a1-e0c8-51e3-9717-b8d823bc6229',
    'Subqueries and Result Validation',
    'subqueries-and-result-validation',
    'Use subqueries carefully and validate query results.',
    4,
    1.00,
    'learning-modules/2c6b46a1-e0c8-51e3-9717-b8d823bc6229/lessons/4b3dfb49-3265-5109-aab4-0a7095efb844-04-subqueries-and-result-validation.md',
    '04-subqueries-and-result-validation.md',
    '0a57e601f1e52114334800e5144a25a29dc0eeb883d460c5399ef8c3fbcdab64',
    3421,
    1,
    'pending',
    NULL,
    NULL,
    now(),
    now()
),
(
    'd5673516-f4e3-5e8c-b6d5-5c85dba56d18',
    '86d77061-3414-500b-bc93-f6469dce806b',
    'Repository Basics and Status',
    'repository-basics-and-status',
    'Understand repositories, tracked files, staging, and working tree status.',
    1,
    0.80,
    'learning-modules/86d77061-3414-500b-bc93-f6469dce806b/lessons/d5673516-f4e3-5e8c-b6d5-5c85dba56d18-01-repository-basics-and-status.md',
    '01-repository-basics-and-status.md',
    '88f3ee46a64120cb443a8d206d943e839752391ee8683f69c476cb30ef80afec',
    3275,
    1,
    'pending',
    NULL,
    NULL,
    now(),
    now()
),
(
    '88f03861-e7b3-5a01-850b-904915096817',
    '86d77061-3414-500b-bc93-f6469dce806b',
    'Commits and Meaningful History',
    'commits-and-meaningful-history',
    'Create focused commits with useful messages and reviewable changes.',
    2,
    0.90,
    'learning-modules/86d77061-3414-500b-bc93-f6469dce806b/lessons/88f03861-e7b3-5a01-850b-904915096817-02-commits-and-meaningful-history.md',
    '02-commits-and-meaningful-history.md',
    '0e57602359fc28a37946daad520dba1eac0d23f09affb9cd1f03fe1d7574d973',
    3277,
    1,
    'pending',
    NULL,
    NULL,
    now(),
    now()
),
(
    'b5b41884-408e-5432-807f-29998afd635c',
    '86d77061-3414-500b-bc93-f6469dce806b',
    'Branches and Merging',
    'branches-and-merging',
    'Use branches to isolate work and merge completed changes.',
    3,
    1.10,
    'learning-modules/86d77061-3414-500b-bc93-f6469dce806b/lessons/b5b41884-408e-5432-807f-29998afd635c-03-branches-and-merging.md',
    '03-branches-and-merging.md',
    'e2b609da1d060b5c34017ade368cd6994e0e8d101f18e23423b9399e22c5d501',
    3228,
    1,
    'pending',
    NULL,
    NULL,
    now(),
    now()
),
(
    'e1877a34-0037-5c0c-8ae0-ba4ae9e4f653',
    '86d77061-3414-500b-bc93-f6469dce806b',
    'Diffs, Restore, and Safe Fixups',
    'diffs-restore-and-safe-fixups',
    'Inspect changes and recover safely without losing work.',
    4,
    1.00,
    'learning-modules/86d77061-3414-500b-bc93-f6469dce806b/lessons/e1877a34-0037-5c0c-8ae0-ba4ae9e4f653-04-diffs-restore-and-safe-fixups.md',
    '04-diffs-restore-and-safe-fixups.md',
    '02b3990a94c24754a5965b14c13234ec07d96f0408dc63d27d433a1c6e7f676c',
    3256,
    1,
    'pending',
    NULL,
    NULL,
    now(),
    now()
),
(
    '3f02ae4e-3fec-5bf2-b302-ca4b256d387c',
    '773ad089-8e61-5850-afa4-8cafd371acbf',
    'C# Program Structure and Types',
    'csharp-program-structure-and-types',
    'Understand C# files, namespaces, methods, variables, and common types.',
    1,
    1.00,
    'learning-modules/773ad089-8e61-5850-afa4-8cafd371acbf/lessons/3f02ae4e-3fec-5bf2-b302-ca4b256d387c-01-csharp-program-structure-and-types.md',
    '01-csharp-program-structure-and-types.md',
    '96f5cb6e524f1728e443bcbb7ca2f456a0f8a52c5c5f5d840bf1e32c5215970f',
    3317,
    1,
    'pending',
    NULL,
    NULL,
    now(),
    now()
),
(
    '4b51215d-be64-5c6d-82e0-41cb682b1c56',
    '773ad089-8e61-5850-afa4-8cafd371acbf',
    'Control Flow and Methods',
    'control-flow-and-methods',
    'Write decisions, loops, and reusable methods with parameters and return values.',
    2,
    1.00,
    'learning-modules/773ad089-8e61-5850-afa4-8cafd371acbf/lessons/4b51215d-be64-5c6d-82e0-41cb682b1c56-02-control-flow-and-methods.md',
    '02-control-flow-and-methods.md',
    'b7d6aa044b63d39d3c528aaf83a0887f9aaf8abe58fc9b2037cb83b6abebbdd8',
    3294,
    1,
    'pending',
    NULL,
    NULL,
    now(),
    now()
),
(
    '72d7f8c5-b564-5dd4-9ab2-c55c8f9e36b3',
    '773ad089-8e61-5850-afa4-8cafd371acbf',
    'Classes, Objects, and Properties',
    'classes-objects-and-properties',
    'Model data and behavior with classes, objects, constructors, and properties.',
    3,
    1.20,
    'learning-modules/773ad089-8e61-5850-afa4-8cafd371acbf/lessons/72d7f8c5-b564-5dd4-9ab2-c55c8f9e36b3-03-classes-objects-and-properties.md',
    '03-classes-objects-and-properties.md',
    '1cf516a9d4a139017b3b54e6de91515809cec89a9111c4854c74bbe6a5393733',
    3313,
    1,
    'pending',
    NULL,
    NULL,
    now(),
    now()
),
(
    'cca28fb9-a42d-5c59-a3bf-e4eb69837683',
    '773ad089-8e61-5850-afa4-8cafd371acbf',
    'Collections, Exceptions, and Async Basics',
    'collections-exceptions-and-async-basics',
    'Use collections, handle errors, and recognize basic async method structure.',
    4,
    1.20,
    'learning-modules/773ad089-8e61-5850-afa4-8cafd371acbf/lessons/cca28fb9-a42d-5c59-a3bf-e4eb69837683-04-collections-exceptions-and-async-basics.md',
    '04-collections-exceptions-and-async-basics.md',
    '521667c27c655f672fc492d33fb5c4ffd254a80f3be02c575ee3df028433f73a',
    3353,
    1,
    'pending',
    NULL,
    NULL,
    now(),
    now()
);

INSERT INTO public.skill_module_quiz
(skill_module_quiz_id, skill_module_id, title, description, passing_score_percent, max_attempts, status, created_at, updated_at)
VALUES
(
    '273b1223-d7a8-5bbd-bc4e-b019e2b666e4',
    '8ce0ddf3-172f-5fe6-b6a6-220a64f34a82',
    'TypeScript Basics Quiz',
    'Checks core concepts from the draft module lessons. The module is draft, but this quiz is complete for authoring and preview testing.',
    70.00,
    3,
    'draft',
    now(),
    now()
),
(
    '2d9dc217-28fe-546d-b37b-0e2c25a2d36c',
    '2c6b46a1-e0c8-51e3-9717-b8d823bc6229',
    'SQL Basics Quiz',
    'Checks core concepts from the draft module lessons. The module is draft, but this quiz is complete for authoring and preview testing.',
    70.00,
    3,
    'draft',
    now(),
    now()
),
(
    '86b1deac-28e2-5983-92c0-a672118ea2e2',
    '86d77061-3414-500b-bc93-f6469dce806b',
    'Git Basics Quiz',
    'Checks core concepts from the draft module lessons. The module is draft, but this quiz is complete for authoring and preview testing.',
    70.00,
    3,
    'draft',
    now(),
    now()
),
(
    '6930106f-f652-53c6-aee7-5dea561bbfcb',
    '773ad089-8e61-5850-afa4-8cafd371acbf',
    'C# Basics Quiz',
    'Checks core concepts from the draft module lessons. The module is draft, but this quiz is complete for authoring and preview testing.',
    70.00,
    3,
    'draft',
    now(),
    now()
);

INSERT INTO public.skill_module_quiz_question
(skill_module_quiz_question_id, skill_module_quiz_id, question_text, question_type, explanation, order_index, points, created_at, updated_at)
VALUES
(
    'a86b678a-ff6f-5f78-a0cf-fde8c7ac8319',
    '273b1223-d7a8-5bbd-bc4e-b019e2b666e4',
    'What is one main benefit of TypeScript?',
    'single_choice',
    'TypeScript catches many type mistakes before the code runs.',
    1,
    1,
    now(),
    now()
),
(
    'd3dbe3a8-74f8-59a5-8072-4fbe26682261',
    '273b1223-d7a8-5bbd-bc4e-b019e2b666e4',
    'What does type inference mean?',
    'single_choice',
    'TypeScript can infer types from assigned values and return expressions.',
    2,
    1,
    now(),
    now()
),
(
    'ce12a905-e920-5629-a789-4182c5849867',
    '273b1223-d7a8-5bbd-bc4e-b019e2b666e4',
    'Which construct describes an object shape?',
    'single_choice',
    'Interfaces are commonly used to describe object properties and methods.',
    3,
    1,
    now(),
    now()
),
(
    '4da9f46d-5aef-5d7d-bbba-73ca9f1cf634',
    '273b1223-d7a8-5bbd-bc4e-b019e2b666e4',
    'What is a union type?',
    'single_choice',
    'Unions represent alternatives such as string | number.',
    4,
    1,
    now(),
    now()
),
(
    'e4ce18c8-42de-5e90-be52-82f8a0a0002e',
    '273b1223-d7a8-5bbd-bc4e-b019e2b666e4',
    'What does narrowing do?',
    'single_choice',
    'Checks like typeof allow the compiler to narrow a union.',
    5,
    1,
    now(),
    now()
),
(
    '65e4eebc-f466-5e78-86e6-76666bc27279',
    '273b1223-d7a8-5bbd-bc4e-b019e2b666e4',
    'Why use generics?',
    'single_choice',
    'Generics let functions/classes work with multiple types while retaining type information.',
    6,
    1,
    now(),
    now()
),
(
    '13e12c2a-c5e7-5400-8b20-cb5f010bbea1',
    '273b1223-d7a8-5bbd-bc4e-b019e2b666e4',
    'What does an optional property usually use?',
    'single_choice',
    'The ? marker means a property may be absent.',
    7,
    1,
    now(),
    now()
),
(
    '6f82571a-9a41-5c58-b013-f66ab34f142b',
    '273b1223-d7a8-5bbd-bc4e-b019e2b666e4',
    'What does structural typing compare?',
    'single_choice',
    'TypeScript compatibility is based largely on structure.',
    8,
    1,
    now(),
    now()
),
(
    'a1905afb-2387-54f8-a9a1-1cb26b63c5bd',
    '273b1223-d7a8-5bbd-bc4e-b019e2b666e4',
    'Where is tsconfig commonly used?',
    'single_choice',
    'tsconfig.json controls compiler options and included files.',
    9,
    1,
    now(),
    now()
),
(
    '24a7b56b-78fa-5ab8-b680-f10949c3b701',
    '273b1223-d7a8-5bbd-bc4e-b019e2b666e4',
    'What is a type guard?',
    'single_choice',
    'Guards such as typeof and in checks help narrow unions.',
    10,
    1,
    now(),
    now()
),
(
    '4f7423a5-34b3-5e95-a7d1-dcc7f1f83597',
    '2d9dc217-28fe-546d-b37b-0e2c25a2d36c',
    'Which clause filters rows?',
    'single_choice',
    'WHERE restricts rows before they are returned.',
    1,
    1,
    now(),
    now()
),
(
    'd7d05bba-5d14-5850-a4c5-1aeec23c077c',
    '2d9dc217-28fe-546d-b37b-0e2c25a2d36c',
    'Which clause sorts result rows?',
    'single_choice',
    'ORDER BY controls result ordering.',
    2,
    1,
    now(),
    now()
),
(
    'bb5ba83f-bec2-5885-b963-241dca1d4ccc',
    '2d9dc217-28fe-546d-b37b-0e2c25a2d36c',
    'What does an INNER JOIN return?',
    'single_choice',
    'INNER JOIN keeps rows that satisfy the join condition.',
    3,
    1,
    now(),
    now()
),
(
    '551da85f-e97c-578e-a7d4-e7baab1e8855',
    '2d9dc217-28fe-546d-b37b-0e2c25a2d36c',
    'What does LEFT JOIN preserve?',
    'single_choice',
    'LEFT JOIN keeps left rows even when the right side is missing.',
    4,
    1,
    now(),
    now()
),
(
    'a1a33330-b14a-54c6-8f70-845e67b77c32',
    '2d9dc217-28fe-546d-b37b-0e2c25a2d36c',
    'Which function counts rows?',
    'single_choice',
    'COUNT is used to count rows or non-null values.',
    5,
    1,
    now(),
    now()
),
(
    'a096b162-0cec-59b9-a798-24d261dd95da',
    '2d9dc217-28fe-546d-b37b-0e2c25a2d36c',
    'When is HAVING used?',
    'single_choice',
    'HAVING filters after grouping and aggregation.',
    6,
    1,
    now(),
    now()
),
(
    '32ce1495-edf4-5b23-b415-8f9141005753',
    '2d9dc217-28fe-546d-b37b-0e2c25a2d36c',
    'What should a join condition usually compare?',
    'single_choice',
    'Joins usually connect primary and foreign keys.',
    7,
    1,
    now(),
    now()
),
(
    'b516a07e-8e6a-5f35-801c-ff84655921e2',
    '2d9dc217-28fe-546d-b37b-0e2c25a2d36c',
    'What is a subquery?',
    'single_choice',
    'Subqueries can provide intermediate results for another query.',
    8,
    1,
    now(),
    now()
),
(
    'd35b8c2c-b57f-5a32-a8e8-aa0972c10fbf',
    '2d9dc217-28fe-546d-b37b-0e2c25a2d36c',
    'Why use LIMIT while exploring?',
    'single_choice',
    'LIMIT keeps exploratory results manageable.',
    9,
    1,
    now(),
    now()
),
(
    '7da0413e-b832-58d4-93f8-5c5922490e2d',
    '2d9dc217-28fe-546d-b37b-0e2c25a2d36c',
    'What is one result validation habit?',
    'single_choice',
    'Basic sanity checks catch incorrect joins and filters.',
    10,
    1,
    now(),
    now()
),
(
    '57aa9ea0-7f50-5404-9171-9b2a846df6da',
    '86b1deac-28e2-5983-92c0-a672118ea2e2',
    'Which command shows working tree state?',
    'single_choice',
    'git status shows staged, unstaged, and untracked changes.',
    1,
    1,
    now(),
    now()
),
(
    'd38fbb2a-dd7f-5f93-96cd-63666b9bac15',
    '86b1deac-28e2-5983-92c0-a672118ea2e2',
    'What is the staging area for?',
    'single_choice',
    'The staging area lets you prepare a focused commit.',
    2,
    1,
    now(),
    now()
),
(
    '62cd87f4-d64c-5700-8e4a-01d908e463f7',
    '86b1deac-28e2-5983-92c0-a672118ea2e2',
    'What is an atomic commit?',
    'single_choice',
    'Atomic commits are easier to review and revert.',
    3,
    1,
    now(),
    now()
),
(
    'f8057ab7-53d0-5a1f-bdcb-a6846194fe80',
    '86b1deac-28e2-5983-92c0-a672118ea2e2',
    'Which command creates a commit?',
    'single_choice',
    'git commit records staged changes.',
    4,
    1,
    now(),
    now()
),
(
    'b15e548f-2f82-5553-aa12-2d2eda24f22b',
    '86b1deac-28e2-5983-92c0-a672118ea2e2',
    'Why use branches?',
    'single_choice',
    'Branches let teams work independently before merging.',
    5,
    1,
    now(),
    now()
),
(
    '70485d67-74a9-52c7-80d6-b94fbbbcf902',
    '86b1deac-28e2-5983-92c0-a672118ea2e2',
    'What does a merge combine?',
    'single_choice',
    'Merging integrates branch histories and file changes.',
    6,
    1,
    now(),
    now()
),
(
    'd74f79ed-9d19-56ce-ba0b-0c9ef0cb3eae',
    '86b1deac-28e2-5983-92c0-a672118ea2e2',
    'What does git diff help inspect?',
    'single_choice',
    'git diff shows code changes before commit.',
    7,
    1,
    now(),
    now()
),
(
    'db13ca6c-54b2-5788-8934-d6da27ec20bb',
    '86b1deac-28e2-5983-92c0-a672118ea2e2',
    'What does unstage mean?',
    'single_choice',
    'Unstaging changes what is included in the next commit.',
    8,
    1,
    now(),
    now()
),
(
    '55a8d529-4356-568a-b4b0-a9de1a5863ee',
    '86b1deac-28e2-5983-92c0-a672118ea2e2',
    'What should you do before risky cleanup?',
    'single_choice',
    'Status and diff reduce accidental data loss.',
    9,
    1,
    now(),
    now()
),
(
    'dd42fd4c-9155-523c-ba92-88965351c732',
    '86b1deac-28e2-5983-92c0-a672118ea2e2',
    'What is a merge conflict?',
    'single_choice',
    'Conflicts require manual resolution.',
    10,
    1,
    now(),
    now()
),
(
    '061801fb-7d33-59c0-8ade-d2081b202739',
    '6930106f-f652-53c6-aee7-5dea561bbfcb',
    'Which keyword declares a class?',
    'single_choice',
    'The class keyword defines a reference type with data and behavior.',
    1,
    1,
    now(),
    now()
),
(
    '3b9f3912-d15d-5405-90c7-b7f246f690be',
    '6930106f-f652-53c6-aee7-5dea561bbfcb',
    'What does a method usually contain?',
    'single_choice',
    'Methods group behavior behind a name and parameters.',
    2,
    1,
    now(),
    now()
),
(
    '153678a8-aa34-5439-887f-2f38ec7efd60',
    '6930106f-f652-53c6-aee7-5dea561bbfcb',
    'Which statement is used for branching?',
    'single_choice',
    'if chooses a path based on a condition.',
    3,
    1,
    now(),
    now()
),
(
    '59657a65-83d0-51ea-94f2-f1c9dd510fc0',
    '6930106f-f652-53c6-aee7-5dea561bbfcb',
    'What is a property commonly used for?',
    'single_choice',
    'Properties expose object state in a controlled way.',
    4,
    1,
    now(),
    now()
),
(
    '9e6678df-f397-58d0-8cf5-cf9352ac9070',
    '6930106f-f652-53c6-aee7-5dea561bbfcb',
    'What does new usually create?',
    'single_choice',
    'new constructs an instance of a type.',
    5,
    1,
    now(),
    now()
),
(
    'd6e51e1b-8884-5344-a82f-6d14fd0b54e3',
    '6930106f-f652-53c6-aee7-5dea561bbfcb',
    'Which collection stores ordered items?',
    'single_choice',
    'List<T> stores an ordered sequence of items.',
    6,
    1,
    now(),
    now()
),
(
    '6a5917d8-977a-5523-93b6-3d422d8ef73a',
    '6930106f-f652-53c6-aee7-5dea561bbfcb',
    'Which block catches exceptions?',
    'single_choice',
    'catch handles exceptions thrown in a try block.',
    7,
    1,
    now(),
    now()
),
(
    'a679cc89-8db6-537f-abdc-068ba6297146',
    '6930106f-f652-53c6-aee7-5dea561bbfcb',
    'What is async Task used for?',
    'single_choice',
    'async Task methods support asynchronous workflows.',
    8,
    1,
    now(),
    now()
),
(
    'aa76639e-757a-5df3-963b-2d57371ac0f1',
    '6930106f-f652-53c6-aee7-5dea561bbfcb',
    'What is a constructor for?',
    'single_choice',
    'Constructors run when a new object is created.',
    9,
    1,
    now(),
    now()
),
(
    'f6c37bc5-21aa-51c7-86f1-8dcea04a6767',
    '6930106f-f652-53c6-aee7-5dea561bbfcb',
    'What does type safety help prevent?',
    'single_choice',
    'Type checking catches incompatible usage before or during compilation.',
    10,
    1,
    now(),
    now()
);

INSERT INTO public.skill_module_quiz_option
(skill_module_quiz_option_id, skill_module_quiz_question_id, option_text, is_correct, explanation, order_index, created_at, updated_at)
VALUES
(
    'e7a0e27e-8252-5f8c-9117-1fa649dd0413',
    'a86b678a-ff6f-5f78-a0cf-fde8c7ac8319',
    'It adds static checks before runtime',
    TRUE,
    'TypeScript catches many type mistakes before the code runs.',
    1,
    now(),
    now()
),
(
    'c3811c04-300a-52af-a95a-0a02fb839d2b',
    'a86b678a-ff6f-5f78-a0cf-fde8c7ac8319',
    'It replaces HTML',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '44fb3c03-77c6-54be-b293-4618effead3c',
    'a86b678a-ff6f-5f78-a0cf-fde8c7ac8319',
    'It removes all runtime bugs',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    'e8d58cd8-6e1c-55bb-85e7-ed777290812f',
    'a86b678a-ff6f-5f78-a0cf-fde8c7ac8319',
    'It only works in browsers',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    '53fb5195-c61c-5302-a4ec-fffef78bfd4e',
    'd3dbe3a8-74f8-59a5-8072-4fbe26682261',
    'The compiler guesses safe types from usage',
    TRUE,
    'TypeScript can infer types from assigned values and return expressions.',
    1,
    now(),
    now()
),
(
    'a5bbed2c-8f2b-555c-bb65-aab69e068541',
    'd3dbe3a8-74f8-59a5-8072-4fbe26682261',
    'The browser changes variable names',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '3046da3e-91db-5974-b6fc-1ddf214f663e',
    'd3dbe3a8-74f8-59a5-8072-4fbe26682261',
    'The database validates the file',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    'b058cb4b-099f-5d3b-b7c6-9c865a48b172',
    'd3dbe3a8-74f8-59a5-8072-4fbe26682261',
    'CSS decides the type',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    '336e6ecc-6821-5dad-ad0d-ad3364059766',
    'ce12a905-e920-5629-a789-4182c5849867',
    'interface',
    TRUE,
    'Interfaces are commonly used to describe object properties and methods.',
    1,
    now(),
    now()
),
(
    'ba99d562-da30-59d3-b6c6-37a746c42be9',
    'ce12a905-e920-5629-a789-4182c5849867',
    'for loop',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '696f2def-0edf-5f0e-858d-8f4668e217aa',
    'ce12a905-e920-5629-a789-4182c5849867',
    'console.log',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    'b5244bb0-dfa1-5d86-bb62-5ec7d77e8c11',
    'ce12a905-e920-5629-a789-4182c5849867',
    'package script',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    'ad0b4908-1e1e-5943-9a83-d4b527abab07',
    '4da9f46d-5aef-5d7d-bbba-73ca9f1cf634',
    'A value that can be one of several types',
    TRUE,
    'Unions represent alternatives such as string | number.',
    1,
    now(),
    now()
),
(
    '8ae20bbc-d421-5e10-abf3-90e638ef5ab0',
    '4da9f46d-5aef-5d7d-bbba-73ca9f1cf634',
    'A class with private fields',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '8142da70-cba3-5218-abcd-1f672e3f4728',
    '4da9f46d-5aef-5d7d-bbba-73ca9f1cf634',
    'A CSS selector group',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    '53b218b0-26b7-55f4-b154-6a118bc7d318',
    '4da9f46d-5aef-5d7d-bbba-73ca9f1cf634',
    'A build command',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    '6021ce92-8725-5382-925f-40dd161477d6',
    'e4ce18c8-42de-5e90-be52-82f8a0a0002e',
    'Reduces a broad type to a safer specific type',
    TRUE,
    'Checks like typeof allow the compiler to narrow a union.',
    1,
    now(),
    now()
),
(
    '67bf4454-70f4-52c6-8b41-b017e4acf73c',
    'e4ce18c8-42de-5e90-be52-82f8a0a0002e',
    'Deletes unused files',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '4fbb9cac-a9ee-5b79-bc59-5d052230d2e2',
    'e4ce18c8-42de-5e90-be52-82f8a0a0002e',
    'Compresses JavaScript',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    'f31bda1e-0267-5ff1-ad39-e471dc18ef86',
    'e4ce18c8-42de-5e90-be52-82f8a0a0002e',
    'Renames imports',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    '915a63c2-7063-5df2-a70c-7745be8bad7f',
    '65e4eebc-f466-5e78-86e6-76666bc27279',
    'To preserve type relationships in reusable code',
    TRUE,
    'Generics let functions/classes work with multiple types while retaining type information.',
    1,
    now(),
    now()
),
(
    '3963ceaa-67cd-5a43-8409-ddb124f3c89f',
    '65e4eebc-f466-5e78-86e6-76666bc27279',
    'To disable compiler checks',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    'fe6933c8-fdda-533e-8368-d1cf9013ac9b',
    '65e4eebc-f466-5e78-86e6-76666bc27279',
    'To write CSS in TypeScript',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    '3ba829cf-4e97-57b0-bc93-53daeb600c99',
    '65e4eebc-f466-5e78-86e6-76666bc27279',
    'To force every value to string',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    'aa67f72d-19c7-5bb3-92f0-fe14e74c7221',
    '13e12c2a-c5e7-5400-8b20-cb5f010bbea1',
    '?',
    TRUE,
    'The ? marker means a property may be absent.',
    1,
    now(),
    now()
),
(
    'd7de8cc5-7786-51e0-9988-7ab397897831',
    '13e12c2a-c5e7-5400-8b20-cb5f010bbea1',
    '!',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    'c3e4d7e6-d806-5bc8-9e25-74609a981346',
    '13e12c2a-c5e7-5400-8b20-cb5f010bbea1',
    '#',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    '868484b3-103a-5ebf-8210-ebebc9d09f32',
    '13e12c2a-c5e7-5400-8b20-cb5f010bbea1',
    '@',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    '01404113-2035-5af6-8a97-7f2755659896',
    '6f82571a-9a41-5c58-b013-f66ab34f142b',
    'Shape and members',
    TRUE,
    'TypeScript compatibility is based largely on structure.',
    1,
    now(),
    now()
),
(
    '182406fc-6ec4-51ea-8ed9-2e30aa8328fc',
    '6f82571a-9a41-5c58-b013-f66ab34f142b',
    'File names only',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '975c7c71-62ca-5d4e-bd48-fea14217bbf4',
    '6f82571a-9a41-5c58-b013-f66ab34f142b',
    'Variable colors',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    'a5361794-cf1d-5efe-be3a-5ac86764d333',
    '6f82571a-9a41-5c58-b013-f66ab34f142b',
    'Runtime memory address',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    'c3d631c0-3010-52ee-9c26-8ec256995501',
    'a1905afb-2387-54f8-a9a1-1cb26b63c5bd',
    'Project compiler configuration',
    TRUE,
    'tsconfig.json controls compiler options and included files.',
    1,
    now(),
    now()
),
(
    'bf24091e-210a-5274-9611-f8f444056a2d',
    'a1905afb-2387-54f8-a9a1-1cb26b63c5bd',
    'HTML page title',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '34cf5dd1-5d46-5928-ba71-29b0609e149b',
    'a1905afb-2387-54f8-a9a1-1cb26b63c5bd',
    'Git author name',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    'bc04144c-f6d8-547d-acd2-e96143b18738',
    'a1905afb-2387-54f8-a9a1-1cb26b63c5bd',
    'SQL table name',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    'a00d63e9-c3ba-5c7d-a86e-81f19d07a33a',
    '24a7b56b-78fa-5ab8-b680-f10949c3b701',
    'A runtime check that helps TypeScript narrow a type',
    TRUE,
    'Guards such as typeof and in checks help narrow unions.',
    1,
    now(),
    now()
),
(
    'e62c299b-df87-513e-b728-b1c163e70e21',
    '24a7b56b-78fa-5ab8-b680-f10949c3b701',
    'A password rule',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '46de6add-a76f-591a-9df7-544a063daf5c',
    '24a7b56b-78fa-5ab8-b680-f10949c3b701',
    'A CSS reset',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    'ddf3ede6-9e9e-5103-b2c0-4d1532041f29',
    '24a7b56b-78fa-5ab8-b680-f10949c3b701',
    'A database index',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    '54a4c770-49f6-56a4-b69d-be46a3a197d6',
    '4f7423a5-34b3-5e95-a7d1-dcc7f1f83597',
    'WHERE',
    TRUE,
    'WHERE restricts rows before they are returned.',
    1,
    now(),
    now()
),
(
    '6f208a02-da3d-5389-a1ee-6b0142d6c989',
    '4f7423a5-34b3-5e95-a7d1-dcc7f1f83597',
    'ORDER BY',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '32a6aed0-8479-522c-a975-a53500f83d96',
    '4f7423a5-34b3-5e95-a7d1-dcc7f1f83597',
    'SELECT',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    '86635198-4830-5128-ac2d-b71439b4a56f',
    '4f7423a5-34b3-5e95-a7d1-dcc7f1f83597',
    'LIMIT',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    '44e72e4e-26a2-52c0-adc7-e63edaeadb61',
    'd7d05bba-5d14-5850-a4c5-1aeec23c077c',
    'ORDER BY',
    TRUE,
    'ORDER BY controls result ordering.',
    1,
    now(),
    now()
),
(
    '697206e0-5e07-5b88-9481-a4c96e37a234',
    'd7d05bba-5d14-5850-a4c5-1aeec23c077c',
    'GROUP BY',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    'cfe48067-5e45-5a71-a68f-60b847c9b31a',
    'd7d05bba-5d14-5850-a4c5-1aeec23c077c',
    'JOIN',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    'f53cae49-2e9e-5122-bc3b-439d36d2689f',
    'd7d05bba-5d14-5850-a4c5-1aeec23c077c',
    'HAVING',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    '7d630166-81c4-5195-9aca-8380b621a462',
    'bb5ba83f-bec2-5885-b963-241dca1d4ccc',
    'Matching rows from both tables',
    TRUE,
    'INNER JOIN keeps rows that satisfy the join condition.',
    1,
    now(),
    now()
),
(
    '6a0b3ab1-52ea-575c-a8a4-7c7d461e05cd',
    'bb5ba83f-bec2-5885-b963-241dca1d4ccc',
    'All rows from the left table only',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '0620c950-e837-5b08-8d96-eeb7b1b29120',
    'bb5ba83f-bec2-5885-b963-241dca1d4ccc',
    'Only duplicate columns',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    'e5808828-2d2e-5b4c-8ed9-1226129f3fe7',
    'bb5ba83f-bec2-5885-b963-241dca1d4ccc',
    'No rows',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    'ad5a069c-6ea2-5673-83ea-4116a7b232e8',
    '551da85f-e97c-578e-a7d4-e7baab1e8855',
    'All rows from the left table',
    TRUE,
    'LEFT JOIN keeps left rows even when the right side is missing.',
    1,
    now(),
    now()
),
(
    'b5a57b14-8b4e-5507-8454-2ca2eb6be03b',
    '551da85f-e97c-578e-a7d4-e7baab1e8855',
    'Only rows with no match',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '52378890-3c06-5240-983a-2e7e05b378cd',
    '551da85f-e97c-578e-a7d4-e7baab1e8855',
    'Only right table rows',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    '5dd0cc2c-27b7-5599-a6b6-094d68585d20',
    '551da85f-e97c-578e-a7d4-e7baab1e8855',
    'Only aggregate values',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    '5b670ca5-3a2b-5616-a403-b4b740c384c4',
    'a1a33330-b14a-54c6-8f70-845e67b77c32',
    'COUNT',
    TRUE,
    'COUNT is used to count rows or non-null values.',
    1,
    now(),
    now()
),
(
    'cf35530e-beb2-5f49-a5de-ebe65ec5f37f',
    'a1a33330-b14a-54c6-8f70-845e67b77c32',
    'SUM',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '27d28a5f-0874-5808-bba7-6ef994fce54b',
    'a1a33330-b14a-54c6-8f70-845e67b77c32',
    'AVG',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    '48724b1a-3009-5d5a-b421-a9e539f1d0bc',
    'a1a33330-b14a-54c6-8f70-845e67b77c32',
    'MAX',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    'f9c94f58-dc51-59e6-8a7a-9fdb2f693f16',
    'a096b162-0cec-59b9-a798-24d261dd95da',
    'To filter grouped results',
    TRUE,
    'HAVING filters after grouping and aggregation.',
    1,
    now(),
    now()
),
(
    '9d880d03-0700-5596-ab82-c01f462b85eb',
    'a096b162-0cec-59b9-a798-24d261dd95da',
    'To rename columns',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '4fd4fe1d-ab56-58ba-9e44-6be1fa15000c',
    'a096b162-0cec-59b9-a798-24d261dd95da',
    'To create an index',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    'ebf3ae0e-74ec-5079-8fe4-e14cedf04b47',
    'a096b162-0cec-59b9-a798-24d261dd95da',
    'To sort text only',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    'd9eadae6-1809-55b8-b33f-09b67e2f9d70',
    '32ce1495-edf4-5b23-b415-8f9141005753',
    'Related key columns',
    TRUE,
    'Joins usually connect primary and foreign keys.',
    1,
    now(),
    now()
),
(
    'e4f0e0c0-692e-5743-b3e5-9ce3f7d2740f',
    '32ce1495-edf4-5b23-b415-8f9141005753',
    'Two random text columns',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '0f144ae1-4797-57ae-bfb3-2687cdc28a60',
    '32ce1495-edf4-5b23-b415-8f9141005753',
    'Only constants',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    'e13b9ca7-7411-5f8e-b630-24303b396672',
    '32ce1495-edf4-5b23-b415-8f9141005753',
    'Column aliases only',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    '88bb9e02-7a64-571f-8354-81f52db191b3',
    'b516a07e-8e6a-5f35-801c-ff84655921e2',
    'A query nested inside another query',
    TRUE,
    'Subqueries can provide intermediate results for another query.',
    1,
    now(),
    now()
),
(
    '0a5774f7-9956-50ba-826e-40a5e69cfff9',
    'b516a07e-8e6a-5f35-801c-ff84655921e2',
    'A backup table',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '619299b7-5b68-57af-954d-c76f2231df74',
    'b516a07e-8e6a-5f35-801c-ff84655921e2',
    'A data type',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    '675be4cd-51c7-504b-8090-e1455d57caff',
    'b516a07e-8e6a-5f35-801c-ff84655921e2',
    'A stored password',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    '514d5082-1d8f-53c6-b920-8ba6c2f4b120',
    'd35b8c2c-b57f-5a32-a8e8-aa0972c10fbf',
    'To inspect a small result set safely',
    TRUE,
    'LIMIT keeps exploratory results manageable.',
    1,
    now(),
    now()
),
(
    '59801706-e42f-5826-9523-014117a937cb',
    'd35b8c2c-b57f-5a32-a8e8-aa0972c10fbf',
    'To delete rows faster',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '012d97ab-a53a-579e-9a7d-4ec90f88f860',
    'd35b8c2c-b57f-5a32-a8e8-aa0972c10fbf',
    'To disable indexes',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    'c5b9f828-e734-511c-95e0-bff43b3dc57e',
    'd35b8c2c-b57f-5a32-a8e8-aa0972c10fbf',
    'To force grouping',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    '215346a0-aea8-51f1-a3a1-86a3c7a7657a',
    '7da0413e-b832-58d4-93f8-5c5922490e2d',
    'Check row counts and sample rows',
    TRUE,
    'Basic sanity checks catch incorrect joins and filters.',
    1,
    now(),
    now()
),
(
    'c791b554-ff09-5ded-918c-6b4ff3bb1543',
    '7da0413e-b832-58d4-93f8-5c5922490e2d',
    'Never read the output',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    'f6251064-3b44-5cee-89cc-ef7ea3d0cf3c',
    '7da0413e-b832-58d4-93f8-5c5922490e2d',
    'Ignore null values',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    'f0e879a0-5b69-53a8-b05a-da8f1c870a7c',
    '7da0413e-b832-58d4-93f8-5c5922490e2d',
    'Remove WHERE clauses',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    'f1ed2660-1b30-5ef9-9314-6b1998d5ac52',
    '57aa9ea0-7f50-5404-9171-9b2a846df6da',
    'git status',
    TRUE,
    'git status shows staged, unstaged, and untracked changes.',
    1,
    now(),
    now()
),
(
    '5772c952-2182-59ef-8910-92ccef5f564e',
    '57aa9ea0-7f50-5404-9171-9b2a846df6da',
    'git merge',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '172955f9-cbda-5154-aca5-7e1fafd8c413',
    '57aa9ea0-7f50-5404-9171-9b2a846df6da',
    'git clone --bare',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    '55825cc8-bdab-52f7-95f5-7100557088c7',
    '57aa9ea0-7f50-5404-9171-9b2a846df6da',
    'git blame',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    '66f9fd6a-f3ba-533e-8d4b-9650ab0662f7',
    'd38fbb2a-dd7f-5f93-96cd-63666b9bac15',
    'Choosing what goes into the next commit',
    TRUE,
    'The staging area lets you prepare a focused commit.',
    1,
    now(),
    now()
),
(
    '1343d582-0e0a-5113-85bf-2966754b1448',
    'd38fbb2a-dd7f-5f93-96cd-63666b9bac15',
    'Deleting branches automatically',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '99b34b04-1a63-510f-8c8a-a1307ef71252',
    'd38fbb2a-dd7f-5f93-96cd-63666b9bac15',
    'Running tests only',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    'fefef3bc-ce47-5e8e-8edf-9b486816604f',
    'd38fbb2a-dd7f-5f93-96cd-63666b9bac15',
    'Hosting remote repositories',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    '80efdb67-fb99-585a-bf8d-00fb83a042c8',
    '62cd87f4-d64c-5700-8e4a-01d908e463f7',
    'A focused commit with one logical change',
    TRUE,
    'Atomic commits are easier to review and revert.',
    1,
    now(),
    now()
),
(
    '8cca888d-5a3a-5a10-97ec-6b65035ea28d',
    '62cd87f4-d64c-5700-8e4a-01d908e463f7',
    'A commit with every file in the repo',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '59d6bef5-4e55-53dd-8f60-2b034ae52f74',
    '62cd87f4-d64c-5700-8e4a-01d908e463f7',
    'A hidden remote branch',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    '6836db65-9534-5127-a8f5-a8addd9f86ae',
    '62cd87f4-d64c-5700-8e4a-01d908e463f7',
    'A merge conflict',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    '88c15994-c287-5d5d-990b-a85e2bc95ec6',
    'f8057ab7-53d0-5a1f-bdcb-a6846194fe80',
    'git commit',
    TRUE,
    'git commit records staged changes.',
    1,
    now(),
    now()
),
(
    'cb0c6307-aa3e-5b94-8ae4-bcc2692216a7',
    'f8057ab7-53d0-5a1f-bdcb-a6846194fe80',
    'git status',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '8bc240ea-1a38-5819-9049-a3658ff77242',
    'f8057ab7-53d0-5a1f-bdcb-a6846194fe80',
    'git diff',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    '4043153e-28a6-562e-8969-91af858a4eb4',
    'f8057ab7-53d0-5a1f-bdcb-a6846194fe80',
    'git branch -d',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    '696f2911-3734-569e-85ea-a71eb0f42042',
    'b15e548f-2f82-5553-aa12-2d2eda24f22b',
    'To isolate work from the main line',
    TRUE,
    'Branches let teams work independently before merging.',
    1,
    now(),
    now()
),
(
    '1bc6f859-c4bf-594e-87d3-3b4b82277702',
    'b15e548f-2f82-5553-aa12-2d2eda24f22b',
    'To remove history',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '6cd0c39a-2d55-54d0-804f-ea4581cf31eb',
    'b15e548f-2f82-5553-aa12-2d2eda24f22b',
    'To disable staging',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    '451ca6a1-2866-50c8-b6cf-aaf841dcd926',
    'b15e548f-2f82-5553-aa12-2d2eda24f22b',
    'To rename all files',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    '0744bcc5-7e93-509e-99d1-577c29a6dc81',
    '70485d67-74a9-52c7-80d6-b94fbbbcf902',
    'Changes from one branch into another',
    TRUE,
    'Merging integrates branch histories and file changes.',
    1,
    now(),
    now()
),
(
    '38a5e12f-2044-58f5-acf2-2e52cd48d23d',
    '70485d67-74a9-52c7-80d6-b94fbbbcf902',
    'Only commit messages',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '34bcebe5-5ce8-5f79-849f-2212f4136e7e',
    '70485d67-74a9-52c7-80d6-b94fbbbcf902',
    'Remote passwords',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    'c363abc1-2591-5670-9850-8dfa8f65372f',
    '70485d67-74a9-52c7-80d6-b94fbbbcf902',
    'Ignored files',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    '3bf75f3b-c7f8-53d3-8078-e944b7319354',
    'd74f79ed-9d19-56ce-ba0b-0c9ef0cb3eae',
    'Line-level changes',
    TRUE,
    'git diff shows code changes before commit.',
    1,
    now(),
    now()
),
(
    '8e8c1517-cfc7-55b2-8b43-8b5e9098e49f',
    'd74f79ed-9d19-56ce-ba0b-0c9ef0cb3eae',
    'Installed packages',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '1a867573-bc11-5c94-9a8e-2858a30e7507',
    'd74f79ed-9d19-56ce-ba0b-0c9ef0cb3eae',
    'Database rows',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    '38a16b46-3605-55f3-a1bf-81e1ac3812ae',
    'd74f79ed-9d19-56ce-ba0b-0c9ef0cb3eae',
    'Browser events',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    '60c0559b-e6fe-5abe-b1e1-d8214c91c874',
    'db13ca6c-54b2-5788-8934-d6da27ec20bb',
    'Remove changes from staging but keep file edits',
    TRUE,
    'Unstaging changes what is included in the next commit.',
    1,
    now(),
    now()
),
(
    'd41525fa-abb4-53e4-aad9-656ddb7fb288',
    'db13ca6c-54b2-5788-8934-d6da27ec20bb',
    'Delete the file permanently',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '97611ece-690f-5d0d-a5dd-a037c7cdbdd4',
    'db13ca6c-54b2-5788-8934-d6da27ec20bb',
    'Push to remote',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    '61034373-f5d2-5619-ae88-ca11b8d596b2',
    'db13ca6c-54b2-5788-8934-d6da27ec20bb',
    'Create a tag',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    'ba90ce88-bffe-5c3b-95b9-2d17cb781155',
    '55a8d529-4356-568a-b4b0-a9de1a5863ee',
    'Check status and diff',
    TRUE,
    'Status and diff reduce accidental data loss.',
    1,
    now(),
    now()
),
(
    'c60c360b-713d-5620-8ad1-c245f1dfc09e',
    '55a8d529-4356-568a-b4b0-a9de1a5863ee',
    'Delete .git',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '6f483fdb-9e02-53d8-88e2-7e1af6273adc',
    '55a8d529-4356-568a-b4b0-a9de1a5863ee',
    'Ignore all changes',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    '8710071b-70a3-57bb-bbb3-11ee731ba540',
    '55a8d529-4356-568a-b4b0-a9de1a5863ee',
    'Merge blindly',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    '1b922adf-b7be-55c5-a5bd-7718cb4aa805',
    'dd42fd4c-9155-523c-ba92-88965351c732',
    'Git cannot automatically combine competing changes',
    TRUE,
    'Conflicts require manual resolution.',
    1,
    now(),
    now()
),
(
    'be053ba8-7779-51e9-9afd-269629b9c6be',
    'dd42fd4c-9155-523c-ba92-88965351c732',
    'A successful deployment',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '2f4df601-624c-5d26-9963-8340040e773d',
    'dd42fd4c-9155-523c-ba92-88965351c732',
    'An empty commit',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    '5279d374-1989-5386-9ba2-a113101dc184',
    'dd42fd4c-9155-523c-ba92-88965351c732',
    'A deleted remote',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    '8ec5161d-4714-56d7-87ac-bbdc249a61bb',
    '061801fb-7d33-59c0-8ade-d2081b202739',
    'class',
    TRUE,
    'The class keyword defines a reference type with data and behavior.',
    1,
    now(),
    now()
),
(
    '153792f8-f825-5643-99c1-63bfd6afbe5d',
    '061801fb-7d33-59c0-8ade-d2081b202739',
    'table',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    'b3eeba36-c5cc-58cd-aaad-205e4ed93d67',
    '061801fb-7d33-59c0-8ade-d2081b202739',
    'select',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    'e11794f6-d284-5cdb-b0a0-8a04f6db7308',
    '061801fb-7d33-59c0-8ade-d2081b202739',
    'module',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    '05d1741f-959b-5422-8743-4b6999a140ab',
    '3b9f3912-d15d-5405-90c7-b7f246f690be',
    'Reusable behavior',
    TRUE,
    'Methods group behavior behind a name and parameters.',
    1,
    now(),
    now()
),
(
    '67f4da86-f35c-57fe-a8a6-77bb65c02827',
    '3b9f3912-d15d-5405-90c7-b7f246f690be',
    'Only CSS rules',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    'eda4903c-145c-5832-bf1e-846f8a1c0fa7',
    '3b9f3912-d15d-5405-90c7-b7f246f690be',
    'Database rows',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    '33cd9534-938e-5ead-a91f-6fa0cb7b009b',
    '3b9f3912-d15d-5405-90c7-b7f246f690be',
    'Git branches',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    'f0adf6cd-4100-5223-94fa-7e8bd4242218',
    '153678a8-aa34-5439-887f-2f38ec7efd60',
    'if',
    TRUE,
    'if chooses a path based on a condition.',
    1,
    now(),
    now()
),
(
    'c1699a15-8e45-54fa-9a9e-4d919c531164',
    '153678a8-aa34-5439-887f-2f38ec7efd60',
    'namespace',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '23474f06-2b70-574b-95e5-bf97a36e2571',
    '153678a8-aa34-5439-887f-2f38ec7efd60',
    'using',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    'b9c887bf-33a7-5668-bc7d-edaee9b80303',
    '153678a8-aa34-5439-887f-2f38ec7efd60',
    'return type',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    'e47acf02-f095-5634-88f6-9a158d9f6fff',
    '59657a65-83d0-51ea-94f2-f1c9dd510fc0',
    'Exposing object data with accessors',
    TRUE,
    'Properties expose object state in a controlled way.',
    1,
    now(),
    now()
),
(
    '67667ff8-a6da-56f7-bfb0-eceeeb894709',
    '59657a65-83d0-51ea-94f2-f1c9dd510fc0',
    'Creating a database index',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '3b9709b4-9df7-57e6-905d-068e69ad0bf0',
    '59657a65-83d0-51ea-94f2-f1c9dd510fc0',
    'Installing packages',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    '369d2290-1e8f-59f2-98af-95044e2be2af',
    '59657a65-83d0-51ea-94f2-f1c9dd510fc0',
    'Sorting a Git branch',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    '157c09da-1c41-5593-9320-9db242933fda',
    '9e6678df-f397-58d0-8cf5-cf9352ac9070',
    'An object instance',
    TRUE,
    'new constructs an instance of a type.',
    1,
    now(),
    now()
),
(
    'e64ca3a1-b274-56d4-9424-5ccccfe1b572',
    '9e6678df-f397-58d0-8cf5-cf9352ac9070',
    'A SQL query',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    'e56ea456-2e9e-5fc7-b0b0-de7f76862d04',
    '9e6678df-f397-58d0-8cf5-cf9352ac9070',
    'A branch merge',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    '2070f364-43e7-5e80-ba50-9461630a23aa',
    '9e6678df-f397-58d0-8cf5-cf9352ac9070',
    'An HTML tag',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    '53e793e9-6b13-5972-90c9-5eaf36b6282e',
    'd6e51e1b-8884-5344-a82f-6d14fd0b54e3',
    'List<T>',
    TRUE,
    'List<T> stores an ordered sequence of items.',
    1,
    now(),
    now()
),
(
    'e4309e0a-b80f-5d6e-b933-a132a83b5b98',
    'd6e51e1b-8884-5344-a82f-6d14fd0b54e3',
    'try',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '639de920-4939-5dd7-85f3-cb1e1fd5e30e',
    'd6e51e1b-8884-5344-a82f-6d14fd0b54e3',
    'namespace',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    '8cdffe6d-d875-5ff2-b560-f8004785f004',
    'd6e51e1b-8884-5344-a82f-6d14fd0b54e3',
    'bool',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    '3ef5e5bf-d476-58da-81a6-44df49199143',
    '6a5917d8-977a-5523-93b6-3d422d8ef73a',
    'catch',
    TRUE,
    'catch handles exceptions thrown in a try block.',
    1,
    now(),
    now()
),
(
    '31f8e9f6-bc1d-5534-964b-8014854056cc',
    '6a5917d8-977a-5523-93b6-3d422d8ef73a',
    'using',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    'fa911015-9fca-5124-bc09-aaa98a0ce374',
    '6a5917d8-977a-5523-93b6-3d422d8ef73a',
    'namespace',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    'e6c3c1a1-6590-591e-bf61-ed97156a4384',
    '6a5917d8-977a-5523-93b6-3d422d8ef73a',
    'class',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    '381b8b15-01e4-5170-b3a0-4ad10150b92e',
    'a679cc89-8db6-537f-abdc-068ba6297146',
    'Asynchronous operations that complete later',
    TRUE,
    'async Task methods support asynchronous workflows.',
    1,
    now(),
    now()
),
(
    'e2629c21-fd83-5964-a320-9eab3a8ff217',
    'a679cc89-8db6-537f-abdc-068ba6297146',
    'Declaring CSS layout',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '4686a225-136e-5090-be95-a726fd06a2d4',
    'a679cc89-8db6-537f-abdc-068ba6297146',
    'Running SQL joins',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    '841635e0-97d0-55d6-90c3-2743d04d21fa',
    'a679cc89-8db6-537f-abdc-068ba6297146',
    'Creating a string literal',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    '41a5a89e-04ab-5af8-a09e-128d9ce4e128',
    'aa76639e-757a-5df3-963b-2d57371ac0f1',
    'Initializing new objects',
    TRUE,
    'Constructors run when a new object is created.',
    1,
    now(),
    now()
),
(
    'b7580ea0-2b02-5aee-88a4-c3bd210ec4ba',
    'aa76639e-757a-5df3-963b-2d57371ac0f1',
    'Deleting methods',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '9e26ff39-8497-5a70-b8b5-34951b475cc9',
    'aa76639e-757a-5df3-963b-2d57371ac0f1',
    'Filtering arrays only',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    '2082e702-3eb4-5554-a361-81fe858e449c',
    'aa76639e-757a-5df3-963b-2d57371ac0f1',
    'Creating commits',
    FALSE,
    NULL,
    4,
    now(),
    now()
),
(
    '9ace75ed-c61d-5660-89e9-8fed7c7e6a14',
    'f6c37bc5-21aa-51c7-86f1-8dcea04a6767',
    'Using values in incompatible ways',
    TRUE,
    'Type checking catches incompatible usage before or during compilation.',
    1,
    now(),
    now()
),
(
    'b67cdd65-67ee-5404-b011-7a6398be85a0',
    'f6c37bc5-21aa-51c7-86f1-8dcea04a6767',
    'All network errors',
    FALSE,
    NULL,
    2,
    now(),
    now()
),
(
    '1f05936d-6991-5ad1-ac34-ce997b9f6c50',
    'f6c37bc5-21aa-51c7-86f1-8dcea04a6767',
    'Every merge conflict',
    FALSE,
    NULL,
    3,
    now(),
    now()
),
(
    'd316c834-44b8-553b-af52-0811d1c86322',
    'f6c37bc5-21aa-51c7-86f1-8dcea04a6767',
    'Bad color contrast',
    FALSE,
    NULL,
    4,
    now(),
    now()
);

COMMIT;
