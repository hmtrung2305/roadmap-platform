BEGIN;

---==============================================
--- Drop tables
---==============================================

DROP TABLE IF EXISTS public.career_role_skill_group CASCADE;
DROP TABLE IF EXISTS public.skill_group_item CASCADE;
DROP TABLE IF EXISTS public.career_role_skill CASCADE;
DROP TABLE IF EXISTS public.skill_group CASCADE;

---=============================================
---1. career_role_skill
---=============================================
CREATE TABLE IF NOT EXISTS public.career_role_skill (
    career_role_skill_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),

    career_role_id uuid NOT NULL,
    skill_id uuid NOT NULL,

    priority int NOT NULL,

    CONSTRAINT fk_career_role_skill_role
        FOREIGN KEY (career_role_id)
        REFERENCES public.career_role(career_role_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_career_role_skill_skill
        FOREIGN KEY (skill_id)
        REFERENCES public.skill(skill_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_career_role_skill
        UNIQUE (career_role_id, skill_id),

    CONSTRAINT chk_career_role_skill_priority
        CHECK (priority BETWEEN 1 AND 4)
);

---============================================
---2. skill_group
---============================================
CREATE TABLE IF NOT EXISTS public.skill_group (
    skill_group_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),

    name varchar(100) NOT NULL,
    slug varchar(100) NOT NULL UNIQUE,

    description text
);

---==============================================
---3. skill_group_item
---==============================================
CREATE TABLE IF NOT EXISTS public.skill_group_item (
    skill_group_item_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),

    skill_group_id uuid NOT NULL,
    skill_id uuid NOT NULL,

    CONSTRAINT fk_skill_group_item_group
        FOREIGN KEY (skill_group_id)
        REFERENCES public.skill_group(skill_group_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_skill_group_item_skill
        FOREIGN KEY (skill_id)
        REFERENCES public.skill(skill_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_skill_group_item
        UNIQUE (skill_group_id, skill_id)
);

---==============================================
---4. career_role_skill_group
---==============================================
CREATE TABLE IF NOT EXISTS public.career_role_skill_group (
    career_role_skill_group_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),

    career_role_id uuid NOT NULL,
    skill_group_id uuid NOT NULL,

    priority int NOT NULL,

    CONSTRAINT fk_career_role_skill_group_role
        FOREIGN KEY (career_role_id)
        REFERENCES public.career_role(career_role_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_career_role_skill_group_group
        FOREIGN KEY (skill_group_id)
        REFERENCES public.skill_group(skill_group_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_career_role_skill_group
        UNIQUE (career_role_id, skill_group_id),

    CONSTRAINT chk_career_role_skill_group_priority
        CHECK (priority BETWEEN 1 AND 4)
);

---==============================================
--- Indexes
---==============================================

CREATE INDEX IF NOT EXISTS ix_career_role_skill_role_id
    ON public.career_role_skill(career_role_id);

CREATE INDEX IF NOT EXISTS ix_career_role_skill_skill_id
    ON public.career_role_skill(skill_id);

CREATE INDEX IF NOT EXISTS ix_skill_group_slug
    ON public.skill_group(slug);

CREATE INDEX IF NOT EXISTS ix_skill_group_item_group_id
    ON public.skill_group_item(skill_group_id);

CREATE INDEX IF NOT EXISTS ix_skill_group_item_skill_id
    ON public.skill_group_item(skill_id);

CREATE INDEX IF NOT EXISTS ix_career_role_skill_group_role_id
    ON public.career_role_skill_group(career_role_id);

CREATE INDEX IF NOT EXISTS ix_career_role_skill_group_group_id
    ON public.career_role_skill_group(skill_group_id);

COMMIT;