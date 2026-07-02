BEGIN;

UPDATE public.ai_credit_plan
SET daily_credit_limit = 10
WHERE daily_credit_limit < 10;

COMMIT;
