



---========================================
---Skill Group Backend
---========================================
INSERT INTO public.skill_group
(
    name,
    slug,
    completion_rule,
    required_skill_count,
    description
)
VALUES

(
    'Programming',
    'backend-programming',
    'ANY',
    NULL,
    'Backend programming languages and coding fundamentals.'
),

(
    'Internet Fundamentals',
    'backend-internet-fundamentals',
    'ALL',
    NULL,
    'Internet, networking, DNS and HTTP fundamentals.'
),

(
    'API Development',
    'backend-api-development',
    'COUNT',
    3,
    'API design, documentation and communication protocols.'
),

(
    'Database',
    'backend-database',
    'ANY',
    NULL,
    'Relational and NoSQL database concepts.'
),

(
    'Security',
    'backend-security',
    'COUNT',
    3,
    'Authentication, authorization and application security.'
),

(
    'Testing & Quality',
    'backend-testing-quality',
    'COUNT',
    3,
    'Testing strategies and code quality practices.'
),

(
    'DevOps & Infrastructure',
    'backend-devops-infrastructure',
    'COUNT',
    3,
    'Infrastructure, deployment and DevOps practices.'
),

(
    'Architecture',
    'backend-architecture',
    'COUNT',
    3,
    'Software architecture and design patterns.'
),

(
    'Scalability & Performance',
    'backend-scalability-performance',
    'COUNT',
    3,
    'Scaling systems and performance optimization.'
),

(
    'Operations & Observability',
    'backend-operations-observability',
    'COUNT',
    2,
    'Logging, monitoring and observability.'
)

ON CONFLICT (slug)
DO UPDATE SET
    name = EXCLUDED.name,
    completion_rule = EXCLUDED.completion_rule,
    required_skill_count = EXCLUDED.required_skill_count,
    description = EXCLUDED.description;



---========================================
---Skill Group Item
---========================================

---==========Programming==========---
INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'java',
    'csharp',
    'golang',
    'nodejs',
    'php',
    'ruby',
    'rust'
)
WHERE sg.slug = 'backend-programming'
ON CONFLICT DO NOTHING;

---==========Internet Fundamentals==========---

INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'internet-basics',
    'dns',
    'http',
    'browser-behavior',
    'networking-basics',
    'domain-hosting'
)
WHERE sg.slug = 'backend-internet-fundamentals'
ON CONFLICT DO NOTHING;

---==========API Development==========---

INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'rest-api',
    'json-api',
    'openapi',
    'api-documentation',
    'api-versioning',
    'pagination-filtering',
    'grpc',
    'soap'
)
WHERE sg.slug = 'backend-api-development'
ON CONFLICT DO NOTHING;

---==========Database==========---
INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'database-design',
    'normalization',
    'indexes',
    'transactions',
    'acid',
    'orm',
    'postgresql',
    'mysql',
    'mongodb',
    'redis'
)
WHERE sg.slug = 'backend-database'
ON CONFLICT DO NOTHING;

---==========Security==========---

INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'authentication',
    'authorization',
    'jwt',
    'oauth',
    'openid',
    'owasp',
    'csrf',
    'xss',
    'sql-injection',
    'tls-https'
)
WHERE sg.slug = 'backend-security'
ON CONFLICT DO NOTHING;

---==========Testing & Quality==========---
INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'unit-testing',
    'integration-testing',
    'functional-testing',
    'tdd',
    'code-review'
)
WHERE sg.slug = 'backend-testing-quality'
ON CONFLICT DO NOTHING;

---==========DevOps & Infrastructure==========---
INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'terminal',
    'nginx',
    'ci-cd',
    'containerization',
    'infrastructure-basics',
    'web-servers'
)
WHERE sg.slug = 'backend-devops-infrastructure'
ON CONFLICT DO NOTHING;

---==========Architecture==========---

INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'software-architecture',
    'monolith',
    'microservices',
    'domain-driven-design',
    'design-patterns',
    'twelve-factor',
    'soa',
    'cqrs',
    'event-sourcing'
)
WHERE sg.slug = 'backend-architecture'
ON CONFLICT DO NOTHING;

---==========Scalability & Performance==========---

INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'caching',
    'load-balancing',
    'types-of-scaling',
    'database-scaling',
    'sharding',
    'replication',
    'profiling-performance',
    'circuit-breaker',
    'backpressure'
)
WHERE sg.slug = 'backend-scalability-performance'
ON CONFLICT DO NOTHING;

---==========Operations & Observability==========---

INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'logging',
    'metrics',
    'telemetry',
    'instrumentation',
    'message-brokers',
    'rabbitmq'
)
WHERE sg.slug = 'backend-operations-observability'
ON CONFLICT DO NOTHING;

---========================================
--- Career Role Skill Group
---========================================
INSERT INTO public.career_role_skill_group
(
    career_role_id,
    skill_group_id,
    priority
)
SELECT
    cr.career_role_id,
    sg.skill_group_id,
    v.priority
FROM public.career_role cr
JOIN (
    VALUES
        ('backend-programming', 1),
        ('backend-internet-fundamentals', 1),
        ('backend-api-development', 1),
        ('backend-database', 1),
        ('backend-security', 1),

        ('backend-testing-quality', 2),
        ('backend-devops-infrastructure', 2),
        ('backend-scalability-performance', 2),

        ('backend-architecture', 3),
        ('backend-operations-observability', 3)
) v(slug, priority)
    ON TRUE
JOIN public.skill_group sg
    ON sg.slug = v.slug
WHERE cr.slug = 'backend-developer'
ON CONFLICT (career_role_id, skill_group_id)
DO UPDATE SET
    priority = EXCLUDED.priority;



