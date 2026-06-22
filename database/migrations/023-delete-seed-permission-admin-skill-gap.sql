DELETE FROM public.permission_role pr
USING public.permission p,
      public.role r
WHERE pr.permission_id = p.permission_id
  AND pr.role_id = r.role_id
  AND r.role_name = 'admin'
  AND p.permission_name IN
  (
      'skill_gap_config.view.any',
      'skill_gap_config.update.any'
  );

