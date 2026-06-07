-- Shared skills used by more than one roadmap seed.
-- Run this before the individual roadmap seed files.

BEGIN;

INSERT INTO public.skill (slug, name, category, description, is_active)
VALUES
('airflow', 'Airflow', 'Orchestration', 'Workflow orchestration platform commonly used for scheduled data, ML, and automation pipelines.', true),
('bitbucket', 'Bitbucket', 'Tools', 'Repositories, branches, pull requests, code review, and team collaboration workflows in Bitbucket.', true),
('capacity-planning', 'Capacity Planning', 'Architecture', 'Estimating demand, sizing infrastructure, planning growth, and validating performance limits.', true),
('devops', 'DevOps', 'DevOps', 'Collaboration, automation, release, deployment, operations, and continuous improvement practices.', true),
('docker', 'Docker', 'DevOps', 'Container images, Dockerfiles, Compose, registries, and containerized development/deployment workflows.', true),
('foundation-readiness-check', 'Foundation Readiness Check', 'Readiness', 'Checkpoint for validating prerequisite knowledge before progressing into more advanced roadmap sections.', true),
('git', 'Git', 'Tools', 'Commits, branches, merge, rebase, history, and collaborative version control.', true),
('github', 'GitHub', 'Tools', 'Repositories, pull requests, issues, Actions, code review, and collaboration workflows.', true),
('gitlab', 'GitLab', 'Tools', 'Repositories, merge requests, issues, CI/CD, and team collaboration workflows.', true),
('graphql', 'GraphQL', 'API', 'GraphQL schemas, queries, mutations, fragments, pagination, caching, and API tradeoffs.', true),
('incident-response', 'Incident Response', 'Reliability', 'Responding to production incidents with triage, mitigation, communication, and follow-up analysis.', true),
('interview-readiness', 'Interview Readiness', 'Career Readiness', 'Preparing for technical interviews, portfolio review, behavioral questions, and role-specific case discussions.', true),
('kafka', 'Kafka', 'Messaging', 'Distributed event streaming for producers, consumers, topics, partitions, and streaming pipelines.', true),
('kubernetes', 'Kubernetes', 'DevOps', 'Container orchestration, deployments, services, scaling, configuration, and cluster operations.', true),
('monitoring', 'Monitoring', 'Operations', 'Tracking system health through metrics, dashboards, alerts, logs, and operational checks.', true),
('nosql', 'NoSQL', 'Database', 'Non-relational database models, access patterns, consistency tradeoffs, and scalability considerations.', true),
('observability', 'Observability', 'Operations', 'Understanding systems through logs, metrics, traces, telemetry, dashboards, and alerting.', true),
('portfolio-interview-readiness-and-capstone', 'Portfolio, Interview Readiness, and Capstone', 'Career Readiness', 'Preparing portfolio evidence, capstone work, interview stories, and role-specific project explanations.', true),
('python', 'Python', 'Programming', 'Python syntax, packaging, virtual environments, scripting, backend work, data engineering, and AI workflows.', true),
('relational-databases', 'Relational Databases', 'Database', 'Tables, rows, primary keys, foreign keys, constraints, normalization, indexing, and relationships.', true),
('secrets-management', 'Secrets Management', 'Security', 'Managing API keys, credentials, tokens, and sensitive configuration safely.', true),
('sql', 'SQL', 'Database', 'Querying and managing relational data using SELECT, joins, grouping, aggregates, mutations, and transactions.', true),
('technical-case-studies', 'Technical Case Studies', 'Career Readiness', 'Analyzing realistic technical scenarios and explaining architecture, tradeoffs, debugging, and implementation decisions.', true),
('testing', 'Testing', 'Quality', 'Automated and manual verification practices that improve correctness, confidence, and maintainability.', true),
('workflow-orchestration', 'Workflow Orchestration', 'Orchestration', 'Coordinating multi-step workflows, dependencies, retries, scheduling, and failure handling.', true)
ON CONFLICT (slug) DO UPDATE SET
    name = EXCLUDED.name,
    category = EXCLUDED.category,
    description = EXCLUDED.description,
    is_active = true;

COMMIT;
