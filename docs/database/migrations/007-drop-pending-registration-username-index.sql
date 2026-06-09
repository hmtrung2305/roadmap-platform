-- =========================================================
-- 007 - Allow duplicate pending registration usernames
-- =========================================================
-- Purpose:
-- - The pending_local_registration table should not reserve usernames.
-- - Only public."user" should enforce final username uniqueness.
-- - This allows a user to retry registration with the same username
--   after typing the wrong email domain.

BEGIN;

DROP INDEX IF EXISTS public.uq_pending_local_registration_username_active;

COMMIT;
