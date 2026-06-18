-- reset-database.sql
-- Development/local reset only. This deletes all objects in the public schema.

DROP SCHEMA IF EXISTS public CASCADE;

CREATE SCHEMA public;

GRANT ALL ON SCHEMA public TO postgres;
GRANT ALL ON SCHEMA public TO public;

\i database/schema.sql
\i database/seed.sql