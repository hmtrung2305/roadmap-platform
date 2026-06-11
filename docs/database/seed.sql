-- Main roadmap seed runner.
-- Requires psql because it uses \i include commands.
-- Run from the project root so these relative paths resolve correctly.

\echo 'Seeding shared/core skills...'
\i docs/database/seeds/core/shared-skills.seed.sql

\echo 'Seeding AI Engineer roadmap...'
\i docs/database/seeds/roadmaps/ai-engineer-roadmap.seed.sql

\echo 'Seeding Backend Developer roadmap...'
\i docs/database/seeds/roadmaps/backend-roadmap.seed.sql

\echo 'Seeding Business Intelligence Analyst roadmap...'
\i docs/database/seeds/roadmaps/business-intelligence-roadmap.seed.sql

\echo 'Seeding Cyber Security Expert roadmap...'
\i docs/database/seeds/roadmaps/cyber-security-expert-roadmap.seed.sql

\echo 'Seeding Data Engineer roadmap...'
\i docs/database/seeds/roadmaps/data-engineer-roadmap.seed.sql

\echo 'Seeding Frontend Developer roadmap...'
\i docs/database/seeds/roadmaps/frontend-roadmap.seed.sql

\echo 'Seeding Game Developer roadmap...'
\i docs/database/seeds/roadmaps/game-developer-roadmap.seed.sql

\echo 'Seeding Machine Learning Engineer roadmap...'
\i docs/database/seeds/roadmaps/machine-learning-engineer-roadmap.seed.sql

\echo 'Seeding Network Engineer roadmap...'
\i docs/database/seeds/roadmaps/network-engineer-roadmap.seed.sql

\echo 'Seeding QA Engineer roadmap...'
\i docs/database/seeds/roadmaps/qa-engineer-roadmap.seed.sql

\echo 'Seeding Full Stack Developer roadmap...'
\i docs/database/seeds/roadmaps/fullstack-developer-roadmap.seed.sql

\echo 'Seeding Data Analyst roadmap...'
\i docs/database/seeds/roadmaps/data-analyst-roadmap.seed.sql

\echo 'Seeding DevOps Engineer roadmap...'
\i docs/database/seeds/roadmaps/devops-engineer-roadmap.seed.sql

\echo 'Seeding Cloud Engineer roadmap...'
\i docs/database/seeds/roadmaps/cloud-engineer-roadmap.seed.sql

\echo 'Seeding Mobile Developer roadmap...'
\i docs/database/seeds/roadmaps/mobile-developer-roadmap.seed.sql

\echo 'Roadmap seed completed.'