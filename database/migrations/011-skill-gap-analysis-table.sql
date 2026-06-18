



---==============================================
--- Drop tables
---==============================================

DROP TABLE IF EXISTS public.career_role_skill_group CASCADE;
DROP TABLE IF EXISTS public.skill_group_item CASCADE;
DROP TABLE IF EXISTS public.skill_group CASCADE;


---============================================
---1. skill_group
---============================================
CREATE TABLE IF NOT EXISTS public.skill_group (
    skill_group_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name varchar(100) NOT NULL,
    slug varchar(100) NOT NULL UNIQUE,
    completion_rule varchar(20) NOT NULL DEFAULT 'ANY',
    required_skill_count int NULL,
    description text,
    
    CONSTRAINT chk_skill_group_completion_rule CHECK (completion_rule IN ('ANY', 'ALL', 'COUNT'))
);

---==============================================
---2. skill_group_item
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
---3. career_role_skill_group
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

