-- =========================================================
-- 006 - Add pending local registration storage
-- =========================================================
-- Purpose:
-- - Move local registration pending state out of public."user".
-- - Allow registration verification tokens to belong to a pending registration
--   before a real user account exists.
--
-- Notes:
-- - This migration is additive and safe to run before the backend refactor.
-- - It does not delete or migrate existing pending_verification users.
-- - A later cleanup/backfill migration can remove old pending user rows after
--   the backend fully switches to pending_local_registration.

BEGIN;

CREATE TABLE IF NOT EXISTS public.pending_local_registration
(
    pending_local_registration_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),

    username varchar(50) NOT NULL,
    username_normalized varchar(50) NOT NULL,

    email varchar(254) NOT NULL,
    password_hash text NOT NULL,

    expires_at timestamptz NOT NULL,

    used_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_pending_local_registration_email_active
    ON public.pending_local_registration(email)
    WHERE used_at IS NULL;

CREATE UNIQUE INDEX IF NOT EXISTS uq_pending_local_registration_username_active
    ON public.pending_local_registration(username_normalized)
    WHERE used_at IS NULL;

CREATE INDEX IF NOT EXISTS ix_pending_local_registration_expires_at
    ON public.pending_local_registration(expires_at)
    WHERE used_at IS NULL;

ALTER TABLE public.email_verification_token
ADD COLUMN IF NOT EXISTS pending_local_registration_id uuid;

ALTER TABLE public.email_verification_token
ALTER COLUMN user_id DROP NOT NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'fk_email_verification_pending_local_registration_id'
    ) THEN
        ALTER TABLE public.email_verification_token
        ADD CONSTRAINT fk_email_verification_pending_local_registration_id
            FOREIGN KEY (pending_local_registration_id)
            REFERENCES public.pending_local_registration(pending_local_registration_id)
            ON DELETE CASCADE;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'chk_email_verification_token_single_owner'
    ) THEN
        ALTER TABLE public.email_verification_token
        ADD CONSTRAINT chk_email_verification_token_single_owner
            CHECK (
                (user_id IS NOT NULL AND pending_local_registration_id IS NULL)
                OR
                (user_id IS NULL AND pending_local_registration_id IS NOT NULL)
            );
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ix_email_verification_token_pending_local_registration_id
    ON public.email_verification_token(pending_local_registration_id);

COMMIT;
