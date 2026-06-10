-- Shared skills used by more than one roadmap seed.
-- Run this before the individual roadmap seed files.
-- Idempotent and safe to rerun even if some roadmap seeds were already applied.

BEGIN;

DROP TABLE IF EXISTS seed_shared_skill;
CREATE TEMP TABLE seed_shared_skill (
    slug text PRIMARY KEY,
    name text NOT NULL UNIQUE,
    category text NOT NULL,
    description text NOT NULL
) ON COMMIT DROP;

INSERT INTO seed_shared_skill (slug, name, category, description)
VALUES
('airflow', 'Airflow', 'Orchestration', 'Workflow orchestration platform commonly used for scheduled data, ML, and automation pipelines.'),
('api-fundamentals', 'API Fundamentals', 'API', 'Core API concepts including requests, responses, resources, contracts, status codes, authentication boundaries, and integration workflows.'),
('api-security', 'API Security', 'Security', 'Protecting APIs through authentication, authorization, validation, rate limiting, secure transport, secrets handling, and abuse prevention.'),
('accessibility', 'Accessibility', 'Quality', 'Designing and validating digital experiences that are usable by people with disabilities and compatible with assistive technologies.'),
('accessibility-testing', 'Accessibility Testing', 'Quality', 'Testing web and application experiences against accessibility standards, keyboard navigation, screen reader behavior, and semantic markup expectations.'),
('a-b-testing', 'A/B Testing', 'Analytics', 'Designing, interpreting, and communicating controlled experiments for product, analytics, and model decision-making.'),
('bitbucket', 'Bitbucket', 'Tools', 'Repositories, branches, pull requests, code review, and team collaboration workflows in Bitbucket.'),
('capacity-planning', 'Capacity Planning', 'Architecture', 'Estimating demand, sizing infrastructure, planning growth, and validating performance limits.'),
('ci-cd', 'CI/CD', 'DevOps', 'Continuous integration and continuous delivery practices for automated validation, release, deployment, rollback, and delivery confidence.'),
('classification-metrics', 'Classification Metrics', 'Machine Learning', 'Metrics for classification tasks including accuracy, precision, recall, F1, ROC-AUC, PR-AUC, calibration, and confusion matrices.'),
('clustering', 'Clustering', 'Machine Learning', 'Grouping similar records or observations using unsupervised learning techniques and validating cluster usefulness.'),
('data-catalogs', 'Data Catalogs', 'Data', 'Metadata systems that help users discover, understand, govern, and reuse datasets across an organization.'),
('data-governance', 'Data Governance', 'Data', 'Policies, ownership, stewardship, lineage, quality, privacy, and controls that make data trustworthy and usable.'),
('data-lineage', 'Data Lineage', 'Data', 'Tracing data origin, transformations, dependencies, and downstream usage across pipelines and systems.'),
('data-quality', 'Data Quality', 'Data', 'Ensuring data is accurate, complete, consistent, timely, valid, unique, and fit for its intended use.'),
('data-quality-dimensions', 'Data Quality Dimensions', 'Data', 'Common dimensions for evaluating data quality, including accuracy, completeness, consistency, validity, timeliness, and uniqueness.'),
('data-tests', 'Data Tests', 'Data', 'Automated checks that validate schemas, constraints, relationships, distributions, freshness, and business rules in data workflows.'),
('data-validation', 'Data Validation', 'Data', 'Validating schemas, types, ranges, constraints, expectations, and data assumptions before using data in applications, analytics, or ML.'),
('data-visualization', 'Data Visualization', 'Analytics', 'Communicating data through charts, dashboards, visual encodings, and visual analysis patterns.'),
('descriptive-statistics', 'Descriptive Statistics', 'Statistics', 'Summarizing data through distributions, central tendency, dispersion, percentiles, correlation, and exploratory statistics.'),
('devops', 'DevOps', 'DevOps', 'Collaboration, automation, release, deployment, operations, and continuous improvement practices.'),
('docker', 'Docker', 'DevOps', 'Container images, Dockerfiles, Compose, registries, and containerized development/deployment workflows.'),
('environment-management', 'Environment Management', 'Tools', 'Managing local and project environments, dependencies, configuration, runtime versions, and reproducible setup workflows.'),
('error-analysis', 'Error Analysis', 'Machine Learning', 'Investigating failures by slice, segment, input pattern, class, subgroup, and operational scenario to improve systems and models.'),
('etl-vs-elt', 'ETL vs ELT', 'Data', 'Understanding extraction, transformation, and loading patterns and when transformations should happen before or after loading data.'),
('experiment-tracking', 'Experiment Tracking', 'Machine Learning', 'Tracking parameters, metrics, artifacts, datasets, code versions, and run comparisons for repeatable experiments.'),
('feature-engineering', 'Machine Learning', 'Machine Learning', 'Creating, transforming, selecting, and validating useful model features while avoiding leakage and training-serving mismatch.'),
('foundation-readiness-check', 'Foundation Readiness Check', 'Readiness', 'Checkpoint for validating prerequisite knowledge before progressing into more advanced roadmap sections.'),
('git', 'Git', 'Tools', 'Commits, branches, merge, rebase, history, and collaborative version control.'),
('github', 'GitHub', 'Tools', 'Repositories, pull requests, issues, Actions, code review, and collaboration workflows.'),
('gitlab', 'GitLab', 'Tools', 'Repositories, merge requests, issues, CI/CD, and team collaboration workflows.'),
('graphql', 'GraphQL', 'API', 'GraphQL schemas, queries, mutations, fragments, pagination, caching, and API tradeoffs.'),
('http-fundamentals', 'HTTP Fundamentals', 'Web', 'HTTP requests, responses, methods, status codes, headers, cookies, caching, redirects, and browser/server communication.'),
('incident-response', 'Incident Response', 'Reliability', 'Responding to production incidents with triage, mitigation, communication, and follow-up analysis.'),
('internet-basics', 'Internet Basics', 'Web', 'Foundational internet concepts including DNS, HTTP, browsers, servers, hosting, TCP/IP, and client-server communication.'),
('interview-readiness', 'Interview Readiness', 'Career Readiness', 'Preparing for technical interviews, portfolio review, behavioral questions, and role-specific case discussions.'),
('ip-addressing-and-subnetting', 'IP Addressing and Subnetting', 'Networking', 'IPv4 and IPv6 addressing, CIDR notation, subnet masks, address planning, routing implications, and subnet calculations.'),
('kafka', 'Kafka', 'Messaging', 'Distributed event streaming for producers, consumers, topics, partitions, and streaming pipelines.'),
('kubernetes', 'Kubernetes', 'DevOps', 'Container orchestration, deployments, services, scaling, configuration, and cluster operations.'),
('model-registry', 'Model Registry', 'Machine Learning', 'Managing model versions, metadata, approval states, stages, deployment candidates, and rollback candidates.'),
('model-selection', 'Model Selection', 'Machine Learning', 'Comparing algorithms, validation strategies, metrics, constraints, and tradeoffs to choose an appropriate model.'),
('monitoring', 'Monitoring', 'Operations', 'Tracking system health through metrics, dashboards, alerts, logs, and operational checks.'),
('nosql', 'NoSQL', 'Database', 'Non-relational database models, access patterns, consistency tradeoffs, and scalability considerations.'),
('observability', 'Observability', 'Operations', 'Understanding systems through logs, metrics, traces, telemetry, dashboards, and alerting.'),
('performance-analysis', 'Performance Analysis', 'Performance', 'Measuring, analyzing, and improving latency, throughput, bottlenecks, resource usage, and user/system performance.'),
('pii-and-sensitive-data', 'PII and Sensitive Data', 'Security', 'Identifying and protecting personal, confidential, regulated, or sensitive data throughout storage, processing, and sharing.'),
('portfolio-certification-and-capstone', 'Portfolio, Certification, and Capstone', 'Career Readiness', 'Preparing portfolio evidence, certification study artifacts, capstone work, and role-specific project explanations.'),
('portfolio-interview-readiness-and-capstone', 'Portfolio, Interview Readiness, and Capstone', 'Career Readiness', 'Preparing portfolio evidence, capstone work, interview stories, and role-specific project explanations.'),
('privacy-and-data-protection', 'Privacy and Data Protection', 'Security', 'Protecting personal and sensitive data through minimization, access control, retention, anonymization, consent, and regulatory awareness.'),
('python', 'Python', 'Programming', 'Python syntax, packaging, virtual environments, scripting, backend work, data engineering, and AI workflows.'),
('python-syntax-and-control-flow', 'Python Syntax and Control Flow', 'Programming', 'Python variables, functions, conditionals, loops, comprehensions, exceptions, modules, and control flow fundamentals.'),
('quality-monitoring', 'Quality Monitoring', 'Quality', 'Monitoring outputs, processes, data, or systems for regressions, anomalies, defects, and quality drift over time.'),
('relational-databases', 'Relational Databases', 'Database', 'Tables, rows, primary keys, foreign keys, constraints, normalization, indexing, and relationships.'),
('regression-metrics', 'Regression Metrics', 'Machine Learning', 'Metrics for regression tasks including MAE, MSE, RMSE, R-squared, quantile loss, and domain-specific error interpretation.'),
('reproducible-experiments', 'Reproducible Experiments', 'Machine Learning', 'Controlling code, data, random seeds, configuration, environments, and artifacts so experiments can be repeated and audited.'),
('responsible-ai-and-governance', 'Responsible AI and Governance', 'AI', 'Fairness, transparency, accountability, privacy, documentation, risk management, and governance practices for AI systems.'),
('secrets-management', 'Secrets Management', 'Security', 'Managing API keys, credentials, tokens, and sensitive configuration safely.'),
('slowly-changing-dimensions', 'Slowly Changing Dimensions', 'Data', 'Modeling historical changes in dimensional data using type 1, type 2, type 3, and related warehouse patterns.'),
('sql', 'SQL', 'Database', 'Querying and managing relational data using SELECT, joins, grouping, aggregates, mutations, and transactions.'),
('statistics', 'Statistics', 'Statistics', 'Statistical reasoning for describing data, estimating uncertainty, testing hypotheses, and interpreting analytical results.'),
('technical-case-studies', 'Technical Case Studies', 'Career Readiness', 'Analyzing realistic technical scenarios and explaining architecture, tradeoffs, debugging, and implementation decisions.'),
('testing', 'Testing', 'Quality', 'Automated and manual verification practices that improve correctness, confidence, and maintainability.'),
('testing-fundamentals', 'Testing Fundamentals', 'Quality', 'Core testing concepts including test levels, test types, test design, defect reporting, regression testing, and quality risk.'),
('training-loops', 'Training Loops', 'Machine Learning', 'Implementing and maintaining model training, validation, checkpointing, logging, and inference loops.'),
('workflow-orchestration', 'Workflow Orchestration', 'Orchestration', 'Coordinating multi-step workflows, dependencies, retries, scheduling, and failure handling.'),
('full-stack-development', 'Full Stack Development', 'Software Engineering', 'Building applications across frontend, backend, data persistence, authentication, deployment, and production operations.'),
('frontend-backend-integration', 'Frontend-Backend Integration', 'Software Engineering', 'Connecting user interfaces to backend APIs with reliable contracts, validation, error handling, and state synchronization.'),
('api-client-design', 'API Client Design', 'Software Engineering', 'Designing frontend API client layers with request handling, retries, authentication, caching, and typed responses.'),
('end-to-end-type-safety', 'End-to-End Type Safety', 'Software Engineering', 'Keeping client, server, API contracts, validation schemas, and data models aligned through shared types and schema validation.'),
('production-web-deployment', 'Production Web Deployment', 'DevOps', 'Preparing, deploying, configuring, and maintaining web applications in production environments.'),
('full-stack-testing', 'Full Stack Testing', 'Quality Engineering', 'Testing web applications across components, APIs, integration boundaries, and end-to-end user flows.'),
('web-performance-optimization', 'Web Performance Optimization', 'Frontend Development', 'Measuring and improving page speed, loading behavior, rendering performance, and user-perceived responsiveness.'),
('secure-web-application-development', 'Secure Web Application Development', 'Security', 'Building web applications that handle authentication, authorization, sessions, input validation, and common web security risks safely.'),
('spreadsheets', 'Spreadsheets', 'Data', 'Spreadsheet analysis, formulas, tables, pivots, charts, and business reporting workflows.'),
('excel', 'Excel', 'Data', 'Microsoft Excel usage for data cleaning, formulas, pivot tables, analysis, and reporting.'),
('power-bi', 'Power BI', 'Data', 'Microsoft Power BI dashboarding, data modeling, Power Query, DAX, and business reporting.'),
('tableau', 'Tableau', 'Data', 'Tableau dashboarding, visual analytics, calculated fields, and interactive reporting.'),
('exploratory-data-analysis', 'Exploratory Data Analysis', 'Data', 'Structured exploration of datasets using summaries, distributions, relationships, and data quality checks.'),
('data-cleaning', 'Data Cleaning', 'Data', 'Techniques for fixing missing, duplicated, inconsistent, invalid, or poorly structured data.'),
('data-storytelling', 'Data Storytelling', 'Data', 'Communication of insights through clear narratives, charts, context, and decision-focused recommendations.'),
('stakeholder-communication', 'Stakeholder Communication', 'Professional Skills', 'Clarifying requirements, communicating trade-offs, presenting findings, and aligning analysis with stakeholder decisions.'),
('business-metrics', 'Business Metrics', 'Business', 'Definition and interpretation of KPIs, leading indicators, lagging indicators, and domain-specific business measures.'),
('dashboard-design', 'Dashboard Design', 'Data', 'Designing dashboards with clear layout, visual hierarchy, interactivity, usability, and decision support.'),
('cohort-analysis', 'Cohort Analysis', 'Data', 'Analyzing groups of users, customers, or records by shared start period, behavior, or lifecycle stage.'),
('funnel-analysis', 'Funnel Analysis', 'Data', 'Measuring step-by-step conversion, drop-off, and bottlenecks across a user or business process.'),
('retention-analysis', 'Retention Analysis', 'Data', 'Measuring repeat behavior, churn, cohort survival, and long-term engagement.'),
('time-series-analysis', 'Time Series Analysis', 'Data', 'Analyzing trends, seasonality, rolling metrics, and time-indexed business data.'),
('survey-analysis', 'Survey Analysis', 'Data', 'Designing, cleaning, summarizing, and interpreting survey response data responsibly.'),
('data-modeling', 'Data Modeling', 'Data', 'Designing tables, entities, dimensions, facts, grains, and relationships for reliable analysis.'),
('semantic-layer', 'Semantic Layer', 'Data', 'Defining governed metrics, reusable business logic, dimensions, and analytical entities across tools.'),
('dashboard-quality-assurance', 'Dashboard Quality Assurance', 'Data', 'Testing dashboard filters, metrics, visuals, data freshness, permissions, and stakeholder usability.'),
('analytics-engineering-awareness', 'Analytics Engineering Awareness', 'Data', 'Understanding modeled analytics layers, transformations, documentation, testing, and BI-ready datasets.'),
('data-contracts', 'Data Contracts', 'Data', 'Documenting expected schemas, ownership, changes, and quality guarantees between data producers and consumers.'),
('linux', 'Linux', 'Operating Systems', 'Using Linux systems, shells, processes, permissions, services, and server administration fundamentals.'),
('bash', 'Bash', 'Programming', 'Writing shell commands and scripts for automation, system administration, and operational workflows.'),
('networking-fundamentals', 'Networking Fundamentals', 'Networking', 'Understanding IP, DNS, routing, ports, protocols, load balancing, firewalls, and common network troubleshooting.'),
('dns', 'DNS', 'Networking', 'Understanding domain names, DNS records, resolution flow, caching, TTLs, and operational DNS troubleshooting.'),
('cloud-computing', 'Cloud Computing', 'Cloud', 'Using cloud compute, storage, networking, identity, managed services, pricing, and operational models.'),
('aws', 'AWS', 'Cloud', 'Working with Amazon Web Services core infrastructure, IAM, compute, networking, storage, monitoring, and deployment services.'),
('azure', 'Azure', 'Cloud', 'Working with Microsoft Azure core infrastructure, identity, compute, networking, storage, monitoring, and deployment services.'),
('google-cloud', 'Google Cloud', 'Cloud', 'Working with Google Cloud core infrastructure, IAM, compute, networking, storage, monitoring, and deployment services.'),
('infrastructure-as-code', 'Infrastructure as Code', 'DevOps', 'Defining, reviewing, versioning, and provisioning infrastructure through declarative or programmatic configuration.'),
('terraform', 'Terraform', 'DevOps', 'Using Terraform to define, plan, provision, and manage infrastructure as code.'),
('configuration-management', 'Configuration Management', 'DevOps', 'Managing server, package, service, and environment configuration consistently across infrastructure.'),
('ansible', 'Ansible', 'DevOps', 'Automating configuration management, provisioning, deployment, and operational tasks with Ansible playbooks.'),
('containerization', 'Containerization', 'DevOps', 'Packaging applications and runtime dependencies into portable containers for development and production.'),
('helm', 'Helm', 'DevOps', 'Packaging, configuring, and deploying Kubernetes applications using Helm charts and releases.'),
('gitops', 'GitOps', 'DevOps', 'Managing infrastructure and deployment state through Git workflows, pull requests, reconciliation, and audit history.'),
('sre-practices', 'SRE Practices', 'Reliability Engineering', 'Applying service-level objectives, error budgets, incident response, toil reduction, and reliability engineering practices.'),
('load-balancing', 'Load Balancing', 'Networking', 'Distributing traffic across services or infrastructure to improve availability, scalability, and reliability.'),
('reverse-proxies', 'Reverse Proxies', 'Networking', 'Using reverse proxies for routing, TLS termination, caching, compression, and application gateway behavior.'),
('release-management', 'Release Management', 'DevOps', 'Planning, coordinating, deploying, validating, and rolling back software releases safely.'),
('platform-engineering', 'Platform Engineering', 'DevOps', 'Building internal platforms, golden paths, templates, and developer workflows that improve delivery reliability and speed.'),
('cloud-computing', 'Cloud Computing', 'Cloud', 'Using cloud service models, deployment models, regions, availability zones, managed services, pricing, and operational tradeoffs.'),
('aws', 'AWS', 'Cloud', 'Working with Amazon Web Services core infrastructure, IAM, compute, networking, storage, monitoring, and deployment services.'),
('azure', 'Azure', 'Cloud', 'Working with Microsoft Azure core infrastructure, identity, compute, networking, storage, monitoring, and deployment services.'),
('google-cloud', 'Google Cloud', 'Cloud', 'Working with Google Cloud core infrastructure, IAM, compute, networking, storage, monitoring, and deployment services.'),
('iam', 'Identity and Access Management', 'Security', 'Managing identities, roles, policies, permissions, service accounts, and least-privilege access.'),
('cloud-networking', 'Cloud Networking', 'Cloud', 'Designing and troubleshooting cloud VPCs, subnets, routes, NAT, firewalls, private connectivity, load balancers, and DNS.'),
('cloud-storage', 'Cloud Storage', 'Cloud', 'Using object, block, file, archive, and managed storage services with durability, availability, lifecycle, and cost controls.'),
('serverless', 'Serverless', 'Cloud', 'Building event-driven and managed compute workflows using functions, queues, triggers, and managed platform services.'),
('cloud-security', 'Cloud Security', 'Security', 'Applying cloud security best practices across identity, networking, data protection, workloads, monitoring, and governance.'),
('cloud-monitoring', 'Cloud Monitoring', 'Cloud', 'Monitoring cloud resources, logs, metrics, traces, alerts, health events, and operational dashboards.'),
('cloud-cost-optimization', 'Cloud Cost Optimization', 'Cloud', 'Managing cloud spend through budgets, tags, right-sizing, lifecycle policies, reserved capacity, and FinOps practices.'),
('cloud-migration', 'Cloud Migration', 'Cloud', 'Planning, assessing, migrating, validating, and modernizing workloads from on-premises or legacy environments to cloud platforms.'),
('cloud-architecture', 'Cloud Architecture', 'Cloud', 'Designing secure, reliable, scalable, cost-aware cloud systems using provider architecture frameworks.'),
('infrastructure-as-code', 'Infrastructure as Code', 'DevOps', 'Defining, reviewing, versioning, and provisioning infrastructure through declarative or programmatic configuration.'),
('terraform', 'Terraform', 'DevOps', 'Using Terraform to define, plan, provision, and manage infrastructure as code.'),
('load-balancing', 'Load Balancing', 'Networking', 'Distributing traffic across services or infrastructure to improve availability, scalability, and reliability.'),
('dns', 'DNS', 'Networking', 'Understanding domain names, DNS records, resolution flow, caching, TTLs, and operational DNS troubleshooting.'),
('mobile-development', 'Mobile Development', 'Mobile', 'Building applications for mobile platforms with platform lifecycles, UI patterns, device APIs, releases, and app store constraints.'),
('android-development', 'Android Development', 'Mobile', 'Building Android applications using Kotlin, Android Studio, Jetpack libraries, lifecycle APIs, navigation, and platform services.'),
('ios-development', 'iOS Development', 'Mobile', 'Building iOS applications using Swift, Xcode, SwiftUI or UIKit, platform lifecycles, navigation, and Apple platform services.'),
('react-native', 'React Native', 'Mobile', 'Building cross-platform mobile applications using React Native, TypeScript, native modules, navigation, and mobile platform APIs.'),
('flutter', 'Flutter', 'Mobile', 'Building cross-platform mobile applications using Dart, Flutter widgets, navigation, state management, and platform integrations.'),
('kotlin', 'Kotlin', 'Programming', 'Using Kotlin for Android, backend, or multiplatform development with null safety, coroutines, collections, and idiomatic language patterns.'),
('swift', 'Swift', 'Programming', 'Using Swift for Apple platform development with value types, optionals, concurrency, protocols, and app architecture patterns.'),
('dart', 'Dart', 'Programming', 'Using Dart for Flutter application development with strong typing, async programming, collections, and object-oriented patterns.'),
('mobile-ui-ux', 'Mobile UI/UX', 'Design', 'Designing mobile interfaces around touch targets, navigation patterns, accessibility, responsive layouts, and platform conventions.'),
('mobile-accessibility', 'Mobile Accessibility', 'Accessibility', 'Making mobile apps usable with screen readers, semantic labels, contrast, dynamic type, keyboard support, and accessible interaction patterns.'),
('mobile-testing', 'Mobile Testing', 'Testing', 'Testing mobile applications with unit tests, widget/UI tests, integration tests, device/emulator testing, and release validation.'),
('mobile-performance', 'Mobile Performance', 'Performance', 'Optimizing startup time, rendering, memory, battery, network usage, bundle size, and smooth interaction on mobile devices.'),
('app-store-release', 'App Store Release', 'Mobile', 'Preparing mobile apps for release through signing, metadata, review guidelines, staged rollout, crash monitoring, and version management.'),
('mobile-security', 'Mobile Security', 'Security', 'Protecting mobile apps through secure storage, transport security, authentication flows, permissions, dependency safety, and reverse-engineering awareness.'),
('offline-first', 'Offline-First Apps', 'Mobile', 'Designing apps that work with unreliable networks using local persistence, sync queues, caching, conflict handling, and background constraints.');

-- Update canonical rows that already exist by slug.
UPDATE public.skill s
SET name = ss.name,
    category = ss.category,
    description = ss.description,
    is_active = true
FROM seed_shared_skill ss
WHERE s.slug = ss.slug;

-- Update rows that already exist by name but have a non-canonical slug.
-- Do not force the slug here; this avoids slug unique conflicts if a canonical row also exists.
UPDATE public.skill s
SET category = ss.category,
    description = ss.description,
    is_active = true
FROM seed_shared_skill ss
WHERE s.name = ss.name
  AND NOT EXISTS (
      SELECT 1
      FROM public.skill canonical
      WHERE canonical.slug = ss.slug
  );

-- Insert only missing shared skills. This avoids both slug and name unique conflicts.
INSERT INTO public.skill (slug, name, category, description, is_active)
SELECT ss.slug, ss.name, ss.category, ss.description, true
FROM seed_shared_skill ss
WHERE NOT EXISTS (
    SELECT 1
    FROM public.skill s
    WHERE s.slug = ss.slug OR s.name = ss.name
);

COMMIT;
