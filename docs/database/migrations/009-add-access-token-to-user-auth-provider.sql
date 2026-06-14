ALTER TABLE public.user_auth_provider
ADD COLUMN IF NOT EXISTS access_token text;