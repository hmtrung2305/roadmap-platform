-- ============================================================
-- Migration 015: Add phone number to user profile
-- ============================================================

BEGIN;

ALTER TABLE public.user_profile
    ADD COLUMN phone_number varchar(32);

COMMIT;
