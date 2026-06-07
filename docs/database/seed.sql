-- Main roadmap seed runner.
-- Requires psql because it uses \i include commands.

\echo 'Seeding shared skills...'
\i docs/database/seeds/core/shared-skills.seed.sql

\echo 'Seeding AI engineer roadmap...'
\i docs/database/seeds/roadmaps/ai-engineer-roadmap.seed.sql

\echo 'Seeding backend roadmap...'
\i docs/database/seeds/roadmaps/backend-roadmap.seed.sql

\echo 'Seeding data engineer roadmap...'
\i docs/database/seeds/roadmaps/data-engineer-roadmap.seed.sql

\echo 'Seeding frontend roadmap...'
\i docs/database/seeds/roadmaps/frontend-roadmap.seed.sql

\echo 'Seeding game developer roadmap...'
\i docs/database/seeds/roadmaps/game-developer-roadmap.seed.sql

\echo 'Seeding network engineer roadmap...'
\i docs/database/seeds/roadmaps/network-engineer-roadmap.seed.sql
