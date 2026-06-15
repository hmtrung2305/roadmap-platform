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
    'Foundations & Developer Tools',
    'data-engineer-foundations',
    'COUNT',
    5,
    'Core data engineering concepts and developer tooling.'
),

(
    'SQL, Databases & Data Modeling',
    'data-engineer-modeling',
    'COUNT',
    6,
    'Databases, SQL and data modeling fundamentals.'
),

(
    'Storage, Warehousing & Lakehouse',
    'data-engineer-storage',
    'COUNT',
    5,
    'Storage systems, warehouses and lakehouse architecture.'
),

(
    'Batch Processing & ETL Pipelines',
    'data-engineer-batch-etl',
    'COUNT',
    5,
    'Batch processing and ETL/ELT pipelines.'
),

(
    'Data Ingestion & CDC',
    'data-engineer-ingestion',
    'COUNT',
    4,
    'Data ingestion, APIs and change data capture.'
),

(
    'Streaming Data Systems',
    'data-engineer-streaming',
    'COUNT',
    4,
    'Streaming and event-driven architectures.'
),

(
    'Cloud Data Platforms',
    'data-engineer-cloud',
    'ANY',
    NULL,
    'Cloud-based data platforms.'
),

(
    'Analytics Engineering & dbt',
    'data-engineer-analytics',
    'COUNT',
    4,
    'Transformations, dbt and analytics engineering.'
),

(
    'Data Quality, Governance & Security',
    'data-engineer-governance',
    'COUNT',
    5,
    'Quality, governance, metadata and security.'
),

(
    'DevOps, Reliability & Architecture',
    'data-engineer-operations',
    'COUNT',
    6,
    'Operations, reliability, architecture and production readiness.'
);


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
        ('data-engineer-foundations', 1),
        ('data-engineer-modeling', 1),
        ('data-engineer-storage', 1),
        ('data-engineer-batch-etl', 1),

        -- Priority 2
        ('data-engineer-ingestion', 2),
        ('data-engineer-streaming', 2),
        ('data-engineer-cloud', 2),
        ('data-engineer-analytics', 2),

        -- Priority 3
        ('data-engineer-governance', 3),
        ('data-engineer-operations', 3)

) v(slug, priority)
ON TRUE
JOIN public.skill_group sg
    ON sg.slug = v.slug
WHERE cr.slug = 'data-engineer'
ON CONFLICT (career_role_id, skill_group_id)
DO UPDATE SET
    priority = EXCLUDED.priority;



	


-- =============================================
-- 3. Skill Group Item
-- =============================================

-- =============================================
-- 3.1 Foundations & Developer Tools
-- =============================================
INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'data-engineering',
    'data-engineering-role',
    'data-lifecycle',
    'data-engineering-foundations',
    'data-engineering-role-and-workflow',
    'python',
    'linux',
    'shell',
    'linux-shell-and-cli',
    'git',
    'developer-tools'
)
WHERE sg.slug = 'data-engineer-foundations';
-- =============================================
-- 3.2 SQL, Databases & Data Modeling
-- =============================================
INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'sql',
    'database',
    'relational-databases',
    'postgres',
    'nosql',
    'search',
    'data-modeling',
    'dimensional-modeling',
    'normalization-and-denormalization',
    'slowly-changing-dimensions',
    'data-vault-awareness',
    'modeling-basics',
    'warehouse-concepts'
)
WHERE sg.slug = 'data-engineer-modeling';
-- =============================================
-- 3.3 Storage, Warehousing & Lakehouse
-- =============================================
INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'warehouse',
    'lakehouse',
    'storage',
    'file-formats',
    'csv-and-json',
    'parquet-and-orc',
    'avro-and-schema-evolution',
    'object-storage',
    'data-lake-layout',
    'lakehouse-table-formats',
    'medallion-architecture',
    'oltp-vs-olap',
    'staging-ods-and-data-marts',
    'partitioning-and-clustering',
    'columnar-databases'
)
WHERE sg.slug = 'data-engineer-storage';
-- =============================================
-- 3.4 Batch Processing & ETL Pipelines
-- =============================================
INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'batch',
    'etl',
    'etl-vs-elt',
    'idempotent-pipelines',
    'incremental-processing',
    'backfills-and-reprocessing',
    'pandas',
    'polars',
    'duckdb',
    'pandas-data-processing',
    'polars-data-processing',
    'duckdb-local-analytics',
    'spark',
    'spark-core-concepts',
    'pyspark-dataframes',
    'spark-performance-basics',
    'batch-processing-and-orchestration',
    'etl-and-elt-patterns',
    'python-data-processing',
    'distributed-batch-processing'
)
WHERE sg.slug = 'data-engineer-batch-etl';
-- =============================================
-- 3.5 Data Ingestion & CDC
-- =============================================
INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'api',
    'api-ingestion',
    'file-ingestion',
    'cdc',
    'cdc-concepts',
    'database-replication-basics',
    'debezium-basics',
    'schema',
    'schema-registry',
    'schema-drift-handling',
    'data-contracts',
    'requirements-and-data-contract-thinking',
    'data-contracts-and-schemas',
    'ingestion-source-types',
    'change-data-capture'
)
WHERE sg.slug = 'data-engineer-ingestion';
-- =============================================
-- 3.6 Streaming Data Systems
-- =============================================
INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'streaming',
    'batch-vs-streaming',
    'event-design',
    'delivery-semantics',
    'kafka',
    'spark-structured-streaming',
    'flink',
    'apache-flink',
    'streaming-foundations',
    'stream-processing-engines',
    'streaming-and-event-driven-data-systems'
)
WHERE sg.slug = 'data-engineer-streaming';
-- =============================================
-- 3.7 Cloud Data Platforms
-- =============================================
INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'cloud',
    'aws',
    'gcp',
    'azure',
    'aws-data-platform',
    'google-cloud-data-platform',
    'azure-data-platform',
    'bigquery',
    'redshift',
    'snowflake',
    'databricks',
    'cloud-networking-basics',
    'cloud-cost-management',
    'choose-a-cloud-platform',
    'choose-a-warehouse-or-lakehouse',
    'cloud-data-platforms-and-warehouses'
)
WHERE sg.slug = 'data-engineer-cloud';
-- =============================================
-- 3.8 Analytics Engineering & dbt
-- =============================================
INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'analytics-engineering',
    'dbt',
    'dbt-project-structure',
    'dbt-tests-and-documentation',
    'dbt-incremental-models',
    'analytics-engineering-and-transformation',
    'dbt-core-workflow',
    'transformation-design',
    'analytics',
    'metric-definition',
    'semantic-layer-awareness',
    'data-marts',
    'bi-tool-integration',
    'reverse-etl-awareness',
    'bi-and-serving-layer'
)
WHERE sg.slug = 'data-engineer-analytics';
-- =============================================
-- 3.9 Data Quality, Governance & Security
-- =============================================
INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'data-quality',
    'data-quality-dimensions',
    'data-tests',
    'quality-alerting',
    'governance',
    'catalog',
    'data-catalogs',
    'metadata-management',
    'data-lineage',
    'lineage-and-impact-analysis',
    'governance-and-metadata',
    'privacy',
    'security',
    'pii-and-sensitive-data',
    'iam-and-access-control',
    'rbac-and-abac',
    'audit-and-compliance-awareness',
    'privacy-and-security'
)
WHERE sg.slug = 'data-engineer-governance';
-- =============================================
-- 3.10 DevOps, Reliability & Architecture
-- =============================================

INSERT INTO public.skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM public.skill_group sg
JOIN public.skill s
ON s.slug IN
(
    'devops',
    'docker',
    'terraform',
    'terraform-basics',
    'iac',
    'ci',
    'ci-cd',
    'kubernetes',
    'orchestration',
    'workflow-orchestration',
    'airflow',
    'orchestration-design',
    'monitoring',
    'observability',
    'logging-metrics-and-tracing',
    'pipeline-monitoring',
    'alerting-and-runbooks',
    'reliability',
    'incident-response',
    'slas-slos-and-slis',
    'backups-and-disaster-recovery',
    'performance',
    'query-performance',
    'pipeline-cost-optimization',
    'architecture',
    'lambda-and-kappa-architecture',
    'data-mesh-awareness',
    'data-platform-architecture',
    'requirements-to-architecture',
    'tradeoff-analysis',
    'data-engineering-system-design',
    'capacity-planning'
)
WHERE sg.slug = 'data-engineer-operations';







