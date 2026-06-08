-- Add pending_verification user status for local email registration flow.
-- This allows locally registered users to exist before email verification
-- without marking them as fully active.

BEGIN;

ALTER TABLE public."user"
DROP CONSTRAINT IF EXISTS chk_user_status;

ALTER TABLE public."user"
ADD CONSTRAINT chk_user_status
CHECK (status IN ('active', 'pending_verification', 'suspended', 'deleted'));

-- Backfill existing local users that were created as active
-- but still have an unverified local auth provider.
UPDATE public."user" u
SET status = 'pending_verification',
    updated_at = now()
WHERE u.status = 'active'
  AND EXISTS (
      SELECT 1
      FROM public.user_auth_provider uap
      WHERE uap.user_id = u.user_id
        AND uap.provider = 'local'
        AND uap.email_verified_at IS NULL
  );

COMMIT;
