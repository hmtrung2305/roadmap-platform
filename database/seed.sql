-- Main roadmap seed runner.
-- Requires psql because it uses \i include commands.
-- Run from the project root so these relative paths resolve correctly.

\echo 'Seeding shared/core skills...'
\i database/seeds/core/shared-skills.seed.sql

\echo 'Seeding roles and permissions...'
\i database/seeds/core/001-rbac-roles-permissions.seed.sql

\echo 'Seeding roles and permissions...'
\i database/seeds/core/002-rbac-roles-permissions.seed.sql

\echo 'Seeding roles and permissions...'
\i database/seeds/core/003-rbac-roles-permissions.seed.sql

\echo 'Seeding roadmap draft ownership permissions...'
\i database/seeds/core/004-rbac-roadmap-draft-own-permissions.seed.sql

\echo 'Seeding users...'
\i database/seeds/core/dev-users.seed.sql

\echo 'Core seed completed.'

\echo 'Seeding AI Engineer roadmap...'
\i database/seeds/roadmaps/ai-engineer-roadmap.seed.sql

\echo 'Seeding Backend Developer roadmap...'
\i database/seeds/roadmaps/backend-roadmap.seed.sql

\echo 'Seeding Business Intelligence Analyst roadmap...'
\i database/seeds/roadmaps/business-intelligence-roadmap.seed.sql

\echo 'Seeding Cyber Security Expert roadmap...'
\i database/seeds/roadmaps/cyber-security-expert-roadmap.seed.sql

\echo 'Seeding Data Engineer roadmap...'
\i database/seeds/roadmaps/data-engineer-roadmap.seed.sql

\echo 'Seeding Frontend Developer roadmap...'
\i database/seeds/roadmaps/frontend-roadmap.seed.sql

\echo 'Seeding Game Developer roadmap...'
\i database/seeds/roadmaps/game-developer-roadmap.seed.sql

\echo 'Seeding Machine Learning Engineer roadmap...'
\i database/seeds/roadmaps/machine-learning-engineer-roadmap.seed.sql

\echo 'Seeding Network Engineer roadmap...'
\i database/seeds/roadmaps/network-engineer-roadmap.seed.sql

\echo 'Seeding QA Engineer roadmap...'
\i database/seeds/roadmaps/qa-engineer-roadmap.seed.sql

\echo 'Seeding Full Stack Developer roadmap...'
\i database/seeds/roadmaps/fullstack-developer-roadmap.seed.sql

\echo 'Seeding Data Analyst roadmap...'
\i database/seeds/roadmaps/data-analyst-roadmap.seed.sql

\echo 'Seeding DevOps Engineer roadmap...'
\i database/seeds/roadmaps/devops-engineer-roadmap.seed.sql

\echo 'Seeding Cloud Engineer roadmap...'
\i database/seeds/roadmaps/cloud-engineer-roadmap.seed.sql

\echo 'Seeding Mobile Developer roadmap...'
\i database/seeds/roadmaps/mobile-developer-roadmap.seed.sql

\echo 'Seeding Data Scientist roadmap...'
\i database/seeds/roadmaps/data-scientist-roadmap.seed.sql

\echo 'Seeding Database Engineer roadmap...'
\i database/seeds/roadmaps/database-engineer-roadmap.seed.sql

\echo 'Seeding Site Reliability Engineer roadmap...'
\i database/seeds/roadmaps/site-reliability-engineer-roadmap.seed.sql

\echo 'Roadmap seed completed.'

\echo 'Seeding draft learning modules...'
\i database/seeds/learning-modules/draft-learning-modules.seed.sql

\echo 'Seeding published learning modules...'
\i database/seeds/learning-modules/published-learning-modules.seed.sql


\echo 'Learning module seed completed.'

\echo 'Seeding assessment levels...'
\i database/seeds/assessments/assessment-levels.seed.sql
\echo 'Assessment levels seed completed.'
