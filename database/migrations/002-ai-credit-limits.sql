-- Adds daily AI credit limits for authenticated prompt usage.
-- Default users use the free plan. Premium/admin can be assigned later by
-- inserting or updating one row in public.user_ai_credit_plan.

CREATE TABLE IF NOT EXISTS public.ai_credit_plan
(
    plan_code varchar(30) PRIMARY KEY,
    daily_credit_limit int NOT NULL,
    monthly_credit_limit int,
    description text,
    created_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT chk_ai_credit_plan_daily_limit
        CHECK (daily_credit_limit >= 0),

    CONSTRAINT chk_ai_credit_plan_monthly_limit
        CHECK (monthly_credit_limit IS NULL OR monthly_credit_limit >= 0)
);

INSERT INTO public.ai_credit_plan
    (plan_code, daily_credit_limit, monthly_credit_limit, description)
VALUES
    ('free', 5, NULL, 'Default experimental plan for regular users.'),
    ('premium', 100, NULL, 'Higher daily credit limit for future paid users.'),
    ('admin', 1000, NULL, 'High daily credit limit for administrators and internal testing.')
ON CONFLICT (plan_code) DO UPDATE
SET
    daily_credit_limit = EXCLUDED.daily_credit_limit,
    monthly_credit_limit = EXCLUDED.monthly_credit_limit,
    description = EXCLUDED.description;

CREATE TABLE IF NOT EXISTS public.user_ai_credit_plan
(
    user_id uuid PRIMARY KEY,
    plan_code varchar(30) NOT NULL,
    expires_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_user_ai_credit_plan_user_id
        FOREIGN KEY (user_id)
        REFERENCES public.user(user_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_user_ai_credit_plan_plan_code
        FOREIGN KEY (plan_code)
        REFERENCES public.ai_credit_plan(plan_code)
        ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS public.ai_credit_usage
(
    usage_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,
    feature_name varchar(80) NOT NULL,
    credit_cost int NOT NULL DEFAULT 1,
    request_ref_id uuid,
    metadata jsonb,
    created_at timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT fk_ai_credit_usage_user_id
        FOREIGN KEY (user_id)
        REFERENCES public.user(user_id)
        ON DELETE CASCADE,

    CONSTRAINT chk_ai_credit_usage_credit_cost
        CHECK (credit_cost > 0)
);

CREATE INDEX IF NOT EXISTS idx_ai_credit_usage_user_created_at
    ON public.ai_credit_usage(user_id, created_at);

CREATE INDEX IF NOT EXISTS idx_ai_credit_usage_feature_created_at
    ON public.ai_credit_usage(feature_name, created_at);