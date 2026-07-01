CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Defensive role creation. The role-permission seed remains the source of
-- truth for role-permission mappings.
INSERT INTO public.role (role_name)
VALUES
    ('learner'),
    ('content_manager'),
    ('admin')
ON CONFLICT (role_name) DO NOTHING;

-- =========================================================
-- Users
-- =========================================================

INSERT INTO public.user
(user_id, username, username_normalized, status, created_at, updated_at, deleted_at)
VALUES
(
    '23ef42fc-bc96-4017-8a52-e00cd6a3b985',
    'Learner',
    'learner',
    'active',
    now(),
    now(),
    NULL
),
(
    'd877b560-13c7-4a8f-83c2-563c8da7d4a1',
    'ContentManager',
    'contentmanager',
    'active',
    now(),
    now(),
    NULL
),
(
    '4b760089-bc9d-4775-9d6d-b3a791cb0d02',
    'ContentManager2',
    'contentmanager2',
    'active',
    now(),
    now(),
    NULL
),
(
    'f82a7417-9394-4869-844e-03771b01b2ab',
    'ContentManager3',
    'contentmanager3',
    'active',
    now(),
    now(),
    NULL
),
(
    '71f15afa-83d4-4b31-99ab-c58f6abe0f83',
    'ContentManager4',
    'contentmanager4',
    'active',
    now(),
    now(),
    NULL
),
(
    'c9fb5f8e-6cc5-42d6-9a66-7cf21a2f3876',
    'Admin',
    'admin',
    'active',
    now(),
    now(),
    NULL
),
(
    '0ea6f478-ef58-4d61-9a8c-57d4697893c0',
    'GodSeed',
    'godseed',
    'active',
    now(),
    now(),
    NULL
)
ON CONFLICT (user_id) DO UPDATE SET
    username = EXCLUDED.username,
    username_normalized = EXCLUDED.username_normalized,
    status = EXCLUDED.status,
    updated_at = now(),
    deleted_at = EXCLUDED.deleted_at;

-- =========================================================
-- Local Auth Providers
-- =========================================================
-- Development password for all seeded accounts: Test@123

INSERT INTO public.user_auth_provider
(id, user_id, email, password_hash, provider, provider_user_id, provider_username, created_at, pending_email, email_verified_at)
VALUES
(
    '3a294dad-c1dd-48ff-8133-e6ab6797d0cb',
    '23ef42fc-bc96-4017-8a52-e00cd6a3b985',
    'learner@roadmap.local',
    'AQAAAAIAAYagAAAAEA6TqnazsepY+egZS7ZR0Ji67+lVqUPM/wxonQ4uK4rmt2oiq2Mg4a2XKn9oRrk5Bg==',
    'local',
    'learner@roadmap.local',
    NULL,
    now(),
    NULL,
    now()
),
(
    'a7c7a8f8-b64e-4b11-a214-11e52702c7e7',
    'd877b560-13c7-4a8f-83c2-563c8da7d4a1',
    'content.manager@roadmap.local',
    'AQAAAAIAAYagAAAAEA6TqnazsepY+egZS7ZR0Ji67+lVqUPM/wxonQ4uK4rmt2oiq2Mg4a2XKn9oRrk5Bg==',
    'local',
    'content.manager@roadmap.local',
    NULL,
    now(),
    NULL,
    now()
),
(
    'd4122665-907a-42f9-bdbf-a00ee3228dde',
    '4b760089-bc9d-4775-9d6d-b3a791cb0d02',
    'content.manager2@roadmap.local',
    'AQAAAAIAAYagAAAAEA6TqnazsepY+egZS7ZR0Ji67+lVqUPM/wxonQ4uK4rmt2oiq2Mg4a2XKn9oRrk5Bg==',
    'local',
    'content.manager2@roadmap.local',
    NULL,
    now(),
    NULL,
    now()
),
(
    '099f89dd-2fd7-45c9-aa1a-717f14a2be73',
    'f82a7417-9394-4869-844e-03771b01b2ab',
    'content.manager3@roadmap.local',
    'AQAAAAIAAYagAAAAEA6TqnazsepY+egZS7ZR0Ji67+lVqUPM/wxonQ4uK4rmt2oiq2Mg4a2XKn9oRrk5Bg==',
    'local',
    'content.manager3@roadmap.local',
    NULL,
    now(),
    NULL,
    now()
),
(
    '78be9511-8bdb-4f0f-bf2c-0d8f2e9b5f2e',
    '71f15afa-83d4-4b31-99ab-c58f6abe0f83',
    'content.manager4@roadmap.local',
    'AQAAAAIAAYagAAAAEA6TqnazsepY+egZS7ZR0Ji67+lVqUPM/wxonQ4uK4rmt2oiq2Mg4a2XKn9oRrk5Bg==',
    'local',
    'content.manager4@roadmap.local',
    NULL,
    now(),
    NULL,
    now()
),
(
    'e24e9350-8fbc-4738-8625-3eb513fc9178',
    'c9fb5f8e-6cc5-42d6-9a66-7cf21a2f3876',
    'admin@roadmap.local',
    'AQAAAAIAAYagAAAAEA6TqnazsepY+egZS7ZR0Ji67+lVqUPM/wxonQ4uK4rmt2oiq2Mg4a2XKn9oRrk5Bg==',
    'local',
    'admin@roadmap.local',
    NULL,
    now(),
    NULL,
    now()
),
(
    '9f3dd6ee-7ff1-43a3-8ff8-336b7ab99c3e',
    '0ea6f478-ef58-4d61-9a8c-57d4697893c0',
    'god@roadmap.local',
    'AQAAAAIAAYagAAAAEA6TqnazsepY+egZS7ZR0Ji67+lVqUPM/wxonQ4uK4rmt2oiq2Mg4a2XKn9oRrk5Bg==',
    'local',
    'god@roadmap.local',
    NULL,
    now(),
    NULL,
    now()
)
ON CONFLICT (provider, provider_user_id) DO UPDATE SET
    user_id = EXCLUDED.user_id,
    email = EXCLUDED.email,
    password_hash = EXCLUDED.password_hash,
    provider_username = EXCLUDED.provider_username,
    pending_email = EXCLUDED.pending_email,
    email_verified_at = EXCLUDED.email_verified_at;

-- =========================================================
-- User Profiles
-- =========================================================

INSERT INTO public.user_profile
(
    user_id,
    display_name,
    headline,
    bio,
    location,
    avatar_url,
    cover_image_url,
    career_goal,
    "current_role",
    public_email,
    github_url,
    linkedin_url,
    resume_url,
    personal_website_url,
    is_public,
    created_at,
    updated_at
)
VALUES
(
    '23ef42fc-bc96-4017-8a52-e00cd6a3b985',
    'Learner',
    'Learner Test Account',
    'Development account for validating normal learner flows.',
    'Ho Chi Minh City',
    NULL,
    NULL,
    NULL,
    'Learner',
    'learner@roadmap.local',
    NULL,
    NULL,
    NULL,
    NULL,
    FALSE,
    now(),
    now()
),
(
    'd877b560-13c7-4a8f-83c2-563c8da7d4a1',
    'Content Manager',
    'Learning Content Manager',
    'Development account for validating content_manager-only learning module management flows.',
    'Ho Chi Minh City',
    NULL,
    NULL,
    NULL,
    'Content Manager',
    'content.manager@roadmap.local',
    NULL,
    NULL,
    NULL,
    NULL,
    FALSE,
    now(),
    now()
),
(
    '4b760089-bc9d-4775-9d6d-b3a791cb0d02',
    'Content Manager 2',
    'Learning Content Manager',
    'Development account for validating owned content manager roadmap flows.',
    'Ho Chi Minh City',
    NULL,
    NULL,
    NULL,
    'Content Manager',
    'content.manager2@roadmap.local',
    NULL,
    NULL,
    NULL,
    NULL,
    FALSE,
    now(),
    now()
),
(
    'f82a7417-9394-4869-844e-03771b01b2ab',
    'Content Manager 3',
    'Learning Content Manager',
    'Development account for validating owned content manager roadmap flows.',
    'Ho Chi Minh City',
    NULL,
    NULL,
    NULL,
    'Content Manager',
    'content.manager3@roadmap.local',
    NULL,
    NULL,
    NULL,
    NULL,
    FALSE,
    now(),
    now()
),
(
    '71f15afa-83d4-4b31-99ab-c58f6abe0f83',
    'Content Manager 4',
    'Learning Content Manager',
    'Development account for validating owned content manager roadmap flows.',
    'Ho Chi Minh City',
    NULL,
    NULL,
    NULL,
    'Content Manager',
    'content.manager4@roadmap.local',
    NULL,
    NULL,
    NULL,
    NULL,
    FALSE,
    now(),
    now()
),
(
    'c9fb5f8e-6cc5-42d6-9a66-7cf21a2f3876',
    'Admin',
    'Platform Administrator',
    'Development account for validating admin governance flows.',
    'Ho Chi Minh City',
    NULL,
    NULL,
    NULL,
    'Administrator',
    'admin@roadmap.local',
    NULL,
    NULL,
    NULL,
    NULL,
    FALSE,
    now(),
    now()
),
(
    '0ea6f478-ef58-4d61-9a8c-57d4697893c0',
    'God Account',
    'All-Roles Development Account',
    'Development account for validating combined-role behavior. Do not use as a production role model.',
    'Ho Chi Minh City',
    NULL,
    NULL,
    NULL,
    'All-Roles Test Account',
    'god@roadmap.local',
    NULL,
    NULL,
    NULL,
    NULL,
    FALSE,
    now(),
    now()
)
ON CONFLICT (user_id) DO UPDATE SET
    display_name = EXCLUDED.display_name,
    headline = EXCLUDED.headline,
    bio = EXCLUDED.bio,
    location = EXCLUDED.location,
    avatar_url = EXCLUDED.avatar_url,
    cover_image_url = EXCLUDED.cover_image_url,
    career_goal = EXCLUDED.career_goal,
    "current_role" = EXCLUDED."current_role",
    public_email = EXCLUDED.public_email,
    github_url = EXCLUDED.github_url,
    linkedin_url = EXCLUDED.linkedin_url,
    resume_url = EXCLUDED.resume_url,
    personal_website_url = EXCLUDED.personal_website_url,
    is_public = EXCLUDED.is_public,
    updated_at = now();

-- =========================================================
-- User Role Assignments
-- =========================================================

WITH assignments(username_normalized, role_name) AS (
    VALUES
        -- Existing personal account
        ('tommymoonn', 'learner'),

        -- Dedicated role-surface accounts
        ('learner', 'learner'),
        ('contentmanager', 'content_manager'),
        ('contentmanager2', 'content_manager'),
        ('contentmanager3', 'content_manager'),
        ('contentmanager4', 'content_manager'),
        ('admin', 'admin'),

        -- All-roles development account
        ('godseed', 'learner'),
        ('godseed', 'content_manager'),
        ('godseed', 'admin')
)
INSERT INTO public.user_role (user_id, role_id)
SELECT u.user_id, r.role_id
FROM assignments a
JOIN public.user u
    ON u.username_normalized = a.username_normalized
JOIN public.role r
    ON r.role_name = a.role_name
ON CONFLICT (user_id, role_id) DO NOTHING;