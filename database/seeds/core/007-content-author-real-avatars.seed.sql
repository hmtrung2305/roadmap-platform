-- Refresh demo content-manager avatars with photo-style placeholder portraits.
-- Safe to rerun. Profiles remain fictional demo authors.

BEGIN;

WITH author_avatars(username_normalized, avatar_url) AS (
    VALUES
        ('contentmanager',  'https://randomuser.me/api/portraits/men/32.jpg'),
        ('contentmanager2', 'https://randomuser.me/api/portraits/men/45.jpg'),
        ('contentmanager3', 'https://randomuser.me/api/portraits/men/52.jpg'),
        ('contentmanager4', 'https://randomuser.me/api/portraits/men/75.jpg')
)
UPDATE public.user_profile profile
SET
    avatar_url = mapping.avatar_url,
    updated_at = now()
FROM author_avatars mapping
JOIN public."user" app_user
    ON app_user.username_normalized = mapping.username_normalized
WHERE profile.user_id = app_user.user_id
  AND profile.avatar_url IS DISTINCT FROM mapping.avatar_url;

COMMIT;
