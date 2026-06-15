-- =============================================
-- AI ENGINEER SKILL GAP ANALYSIS
-- =============================================
-- =============================================
-- CLEAN AI ENGINEER SKILL GAP ANALYSIS
-- =============================================

-- Xóa mapping career role -> skill group
DELETE FROM public.career_role_skill_group
WHERE career_role_id = (
    SELECT career_role_id
    FROM public.career_role
    WHERE slug = 'ai-engineer'
);

-- Xóa skill groups
DELETE FROM public.skill_group
WHERE slug LIKE 'ai-engineer-%';
-- =============================================
-- 1. Skill Group
-- =============================================

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
    'Programming, Math & Data Foundations',
    'ai-engineer-foundations',
    'ALL',
    NULL,
    'Core programming, mathematics and data analysis foundations.'
),

(
    'Machine Learning Foundations',
    'ai-engineer-ml-foundations',
    'COUNT',
    5,
    'Core machine learning concepts and model evaluation.'
),

(
    'Deep Learning & Neural Networks',
    'ai-engineer-deep-learning',
    'COUNT',
    5,
    'Neural networks, transformers and deep learning concepts.'
),

(
    'LLMs & Prompt Engineering',
    'ai-engineer-llm-prompting',
    'COUNT',
    6,
    'Large language models and prompt engineering.'
),

(
    'Model APIs & Providers',
    'ai-engineer-model-providers',
    'ANY',
    NULL,
    'Commercial and open-source model providers.'
),

(
    'Embeddings, Vector Search & RAG',
    'ai-engineer-rag',
    'COUNT',
    5,
    'Embeddings, retrieval and RAG systems.'
),

(
    'Agents & Tool Use',
    'ai-engineer-agents',
    'COUNT',
    4,
    'Agentic workflows, tools and orchestration.'
),

(
    'Evaluation, Safety & Guardrails',
    'ai-engineer-evaluation-safety',
    'COUNT',
    4,
    'Evaluation frameworks, testing and safety.'
),

(
    'MLOps & LLMOps',
    'ai-engineer-ops',
    'COUNT',
    4,
    'Deployment, monitoring and operational excellence.'
),

(
    'Portfolio & Career Readiness',
    'ai-engineer-portfolio',
    'ANY',
    NULL,
    'Projects, portfolio and interview preparation.'
)

ON CONFLICT (slug)
DO UPDATE SET
    name = EXCLUDED.name,
    completion_rule = EXCLUDED.completion_rule,
    required_skill_count = EXCLUDED.required_skill_count,
    description = EXCLUDED.description;


-- =============================================
-- 2. Career Role Skill Group
-- =============================================
-- =============================================
-- 2. Career Role Skill Group
-- =============================================

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

        -- Priority 1
        ('ai-engineer-foundations', 1),
        ('ai-engineer-ml-foundations', 1),
        ('ai-engineer-deep-learning', 1),
        ('ai-engineer-llm-prompting', 1),

        -- Priority 2
        ('ai-engineer-model-providers', 2),
        ('ai-engineer-rag', 2),
        ('ai-engineer-agents', 2),

        -- Priority 3
        ('ai-engineer-evaluation-safety', 3),
        ('ai-engineer-ops', 3),
        ('ai-engineer-portfolio', 3)

) v(slug, priority)
ON TRUE
JOIN public.skill_group sg
    ON sg.slug = v.slug
WHERE cr.slug = 'ai-engineer'
ON CONFLICT (career_role_id, skill_group_id)
DO UPDATE SET
    priority = EXCLUDED.priority;
-- =============================================
-- 3. Skill Group Item
-- =============================================

-- =============================================
-- 3.1. Programming, Math & Data Foundations
-- =============================================
INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'python',
    'python-syntax-and-control-flow',
    'functions-and-modules',
    'numpy-arrays',
    'pandas-dataframes',
    'data-cleaning',
    'exploratory-data-analysis',
    'linear-algebra-vectors-and-matrices',
    'probability-foundations',
    'descriptive-statistics'
)
WHERE sg.slug = 'ai-engineer-foundations';
-- =============================================
-- 3.2 Machine Learning Foundations
-- =============================================
INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'ml-core-concepts',
    'supervised-vs-unsupervised-learning',
    'linear-and-logistic-regression',
    'decision-trees-and-random-forests',
    'gradient-boosting',
    'cross-validation',
    'classification-metrics',
    'regression-metrics',
    'feature-engineering',
    'train-validation-test-splits'
)
WHERE sg.slug = 'ai-engineer-ml-foundations';
-- =============================================
-- 3.3 Deep Learning & Neural Networks
-- =============================================
INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'neural-network-basics',
    'activation-functions',
    'loss-functions',
    'forward-and-backpropagation',
    'tensors-and-autograd',
    'pytorch-fundamentals',
    'transformer-architecture',
    'attention-mechanism',
    'nlp-and-transformers',
    'deep-learning-frameworks'
)
WHERE sg.slug = 'ai-engineer-deep-learning';
-- =============================================
-- 3.4 LLMs & Prompt Engineering
-- =============================================
INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'llm-fundamentals',
    'prompt-engineering',
    'few-shot-prompting',
    'prompt-templates',
    'prompt-management',
    'prompt-versioning',
    'function-calling',
    'structured-outputs',
    'tokens-and-context-windows',
    'temperature-and-sampling'
)
WHERE sg.slug = 'ai-engineer-llm-prompting';
-- =============================================
-- 3.5 Model APIs & Providers
-- =============================================
INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'openai-api',
    'anthropic-api',
    'gemini-api',
    'amazon-bedrock',
    'vertex-ai',
    'azure-ai-foundry',
    'open-source-models',
    'provider-routing'
)
WHERE sg.slug = 'ai-engineer-model-providers';
-- =============================================
-- 3.6 Embeddings, Vector Search & RAG
-- =============================================
INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'embedding-systems',
    'embedding-storage',
    'vector-databases-search',
    'pgvector',
    'chroma',
    'pinecone',
    'qdrant',
    'weaviate',
    'rag-design',
    'hybrid-search',
    'chunking-strategies',
    'retriever-design'
)
WHERE sg.slug = 'ai-engineer-rag';
-- =============================================
-- 3.7 Agents & Tool Use
-- =============================================
INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'agent-patterns',
    'agent-memory-state',
    'tool-calling',
    'langgraph-basics',
    'planner-executor-pattern',
    'router-agents',
    'multi-agent-awareness',
    'conversation-state',
    'conversation-persistence',
    'long-term-memory'
)
WHERE sg.slug = 'ai-engineer-agents';
-- =============================================
-- 3.8 Evaluation, Safety & Guardrails
-- =============================================
INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'llm-evaluation',
    'rag-evaluation',
    'answer-faithfulness',
    'evaluation-plan',
    'golden-test-sets',
    'content-moderation',
    'guardrails-and-policy',
    'owasp-llm-top-10',
    'prompt-injection-awareness',
    'hallucination-reduction'
)
WHERE sg.slug = 'ai-engineer-evaluation-safety';
-- =============================================
-- 3.9 MLOps & LLMOps
-- =============================================
INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'llmops-practices',
    'mlflow-tracking',
    'dvc',
    'deployment-foundations',
    'docker',
    'ci-cd',
    'observability',
    'structured-logging',
    'metrics-and-dashboards',
    'production-reliability'
)
WHERE sg.slug = 'ai-engineer-ops';
-- =============================================
-- 3.10 Portfolio & Career Readiness
-- =============================================
INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'ms-required-llm-chat-api',
    'ms-required-rag-app',
    'ms-required-agent-workflow',
    'ms-required-deployed-ai-system',
    'ms-required-ai-capstone',
    'project-storytelling',
    'interview-readiness',
    'technical-case-studies',
    'portfolio-interview-readiness-and-capstone'
)
WHERE sg.slug = 'ai-engineer-portfolio';



