-- Data Analyst Roadmap seed.
-- Idempotent seed. Safe to rerun.
BEGIN;
CREATE EXTENSION IF NOT EXISTS pgcrypto;

DROP TABLE IF EXISTS seed_edge;
DROP TABLE IF EXISTS seed_node_resource;
DROP TABLE IF EXISTS seed_node_skill;
DROP TABLE IF EXISTS seed_node;
DROP TABLE IF EXISTS seed_resource;
DROP TABLE IF EXISTS seed_skill;
DROP TABLE IF EXISTS seed_roadmap_map;

DELETE FROM public.progress_event WHERE roadmap_enrollment_id IN (SELECT e.roadmap_enrollment_id FROM public.roadmap_enrollment e JOIN public.roadmap_version rv ON rv.roadmap_version_id = e.roadmap_version_id JOIN public.roadmap r ON r.roadmap_id = rv.roadmap_id WHERE r.title IN ('Data Analyst Roadmap', 'Data Analytics Roadmap', 'Data Analyst Roadmap - Generated Seed'));
DELETE FROM public.user_node_progress WHERE roadmap_enrollment_id IN (SELECT e.roadmap_enrollment_id FROM public.roadmap_enrollment e JOIN public.roadmap_version rv ON rv.roadmap_version_id = e.roadmap_version_id JOIN public.roadmap r ON r.roadmap_id = rv.roadmap_id WHERE r.title IN ('Data Analyst Roadmap', 'Data Analytics Roadmap', 'Data Analyst Roadmap - Generated Seed'));
DELETE FROM public.roadmap_enrollment WHERE roadmap_version_id IN (SELECT rv.roadmap_version_id FROM public.roadmap_version rv JOIN public.roadmap r ON r.roadmap_id = rv.roadmap_id WHERE r.title IN ('Data Analyst Roadmap', 'Data Analytics Roadmap', 'Data Analyst Roadmap - Generated Seed'));
DELETE FROM public.roadmap WHERE title IN ('Data Analyst Roadmap', 'Data Analytics Roadmap', 'Data Analyst Roadmap - Generated Seed');

INSERT INTO public.career_role (name, slug, description, category, is_active) VALUES (
    'Data Analyst',
    'data-analyst',
    'Data analyst path covering business context, spreadsheets, SQL, statistics, cleaning, visualization, BI tools, Python, governance, advanced analytics awareness, and portfolio work.',
    'Data',
    true
) ON CONFLICT (slug) DO UPDATE SET name = EXCLUDED.name, description = EXCLUDED.description, category = EXCLUDED.category, is_active = true, updated_at = now();

DROP TABLE IF EXISTS seed_roadmap_map;
CREATE TEMP TABLE seed_roadmap_map AS
WITH role_row AS (SELECT career_role_id FROM public.career_role WHERE slug = 'data-analyst'), inserted_roadmap AS (
    INSERT INTO public.roadmap (career_role_id, title, description, visibility)
SELECT career_role_id, 'Data Analyst Roadmap', 'A structured learning path for becoming a data analyst, covering business context, spreadsheets, SQL, statistics, data cleaning, visualization, BI tools, Python analytics, governance, stakeholder communication, advanced analysis awareness, and portfolio-ready projects.', 'public'
FROM role_row
    RETURNING roadmap_id
), inserted_version AS (
    INSERT INTO public.roadmap_version
    (roadmap_id, version_number, major_version, minor_version, patch_version, release_type, status, title, description, estimated_total_hours, layout_direction, layout_algorithm, published_at)
SELECT
    roadmap_id,
    1,
    1,
    0,
    0,
    'initial',
    'published',
    'Data Analyst Roadmap',
    'A practical data analyst roadmap that moves from business questions and data foundations to analysis workflows, dashboards, stakeholder-ready communication, and capstone portfolio work.',
    340,
    'TB',
    'custom',
    now()
FROM inserted_roadmap
    RETURNING roadmap_version_id, roadmap_id
) SELECT roadmap_id, roadmap_version_id FROM inserted_version;

-- Skills.
DROP TABLE IF EXISTS seed_skill;
CREATE TEMP TABLE seed_skill (slug text PRIMARY KEY, name text NOT NULL, category text NOT NULL, description text NOT NULL) ON COMMIT DROP;
INSERT INTO seed_skill VALUES






('a-b-test-interpretation', 'A/B Test Interpretation', 'Data & Analytics', 'Interpret controlled experiment results using metrics, guardrails, confidence, and practical business impact.'),
('accessibility-in-data-visualization', 'Accessibility in Data Visualization', 'Data & Analytics', 'Designing charts and dashboards with readable labels, contrast, color-safe encodings, and inclusive presentation choices.'),
('aggregation-and-grouping', 'Aggregation and Grouping', 'Data & Analytics', 'Using GROUP BY, aggregate functions, HAVING, and conditional aggregation to create business summaries.'),
('analysis-automation-scripts', 'Analysis Automation Scripts', 'Data & Analytics', 'Convert repeated notebook or spreadsheet tasks into scripts with parameters, outputs, and basic logging.'),
('analytical-deliverable-types', 'Analytical Deliverable Types', 'Data & Analytics', 'Distinguish ad hoc analysis, recurring reports, dashboards, metric dictionaries, insight decks, and decision memos.'),
('analytical-thinking', 'Analytical Thinking', 'Data & Analytics', 'Break a business problem into hypotheses, segments, comparisons, assumptions, and measurable evidence.'),
('analytics-backlog-and-prioritization', 'Analytics Backlog and Prioritization', 'Data & Analytics', 'Prioritize requests using impact, urgency, effort, data availability, and decision value.'),
('analytics-documentation', 'Analytics Documentation', 'Tools & Workflow', 'Writing README files, metric notes, data dictionaries, caveats, and dashboard usage guidance.'),
('analytics-qa-review-process', 'Analytics QA Review Process', 'Software Quality', 'Review queries, dashboards, charts, metric definitions, and stakeholder claims before release.'),
('analytics-requirements-intake', 'Analytics Requirements Intake', 'Data & Analytics', 'Clarify stakeholders, scope, definitions, constraints, deadline, expected output, and acceptance criteria before analysis work starts.'),
('avoiding-misleading-visuals', 'Avoiding Misleading Visuals', 'Data & Analytics', 'Identify misleading axes, cherry-picked comparisons, overplotting, bad aggregation, and unsupported causal claims.'),
('business-questions-and-kpis', 'Business Questions and KPIs', 'Data & Analytics', 'The practical scope of Business Questions and KPIs includes question framing, data preparation, metrics, analysis, validation, visualization, and communication.'),
('causal-inference-caution', 'Causal Inference Caution', 'Data & Analytics', 'Recognizing selection bias, confounding, reverse causality, and why observational analysis rarely proves causation by itself.'),
('chart-accessibility', 'Chart Accessibility', 'Design & UX', 'Designing charts with readable labels, contrast, alt text, ordering, and non-color-only encodings for wider accessibility.'),
('cohort-and-retention-sql', 'Cohort and Retention SQL', 'Database', 'Building cohort tables and retention calculations using signup periods, activity windows, joins, and window functions.'),
('conditional-formatting-for-analysis', 'Conditional Formatting for Analysis', 'Data & Analytics', 'Using conditional formatting to highlight outliers, thresholds, missing values, priority records, and exceptions without hiding raw data.'),
('ctes-and-subqueries', 'CTEs and Subqueries', 'Data & Analytics', 'Break complex analytical queries into readable steps using CTEs and subqueries.'),
('customer-retention-analysis', 'Customer Retention Analysis', 'Data & Analytics', 'Analyzing repeat behavior, churn, cohort retention, and lifecycle stages with clear time windows and caveats.'),
('dashboard-data-modeling', 'Dashboard Data Modeling', 'Data & Analytics', 'Shape data into useful dimensions, measures, calculated fields, relationships, and grain for dashboard use.'),
('dashboard-filters-and-interactivity', 'Dashboard Filters and Interactivity', 'Data & Analytics', 'Using filters, parameters, slicers, drilldowns, and tooltips without making dashboards confusing.'),
('dashboard-ownership-and-maintenance', 'Dashboard Ownership and Maintenance', 'Data & Analytics', 'Defining dashboard owners, refresh checks, retirement rules, metric change logs, and support expectations.'),
('dashboard-performance-and-refresh', 'Dashboard Performance and Refresh', 'Data & Analytics', 'Improve dashboard load time, data refresh reliability, model size, and visual complexity without losing analytical value.'),
('dashboard-purpose-and-layout', 'Dashboard Purpose and Layout', 'Data & Analytics', 'Designing dashboard pages around audience, action, scan path, filters, and decision context.'),
('data-analyst-foundations-review', 'Data Analyst Foundations Review', 'Data & Analytics', 'Applying Data Analyst Foundations Review means working with question framing, data preparation, metrics, analysis, validation, visualization, and communication.'),
('data-analyst-role-and-responsibilities', 'Data Analyst Role and Responsibilities', 'Data & Analytics', 'Comparing data analyst responsibilities with BI, data science, and data engineering roles, and identify the expected deliverables in business teams.'),
('data-analyst-take-home-challenge-practice', 'Data Analyst Take-Home Challenge Practice', 'Data & Analytics', 'Applying scoped take-home analyses with timeboxing, assumption notes, clean outputs, and concise stakeholder recommendations.'),
('data-catalogs-and-lineage', 'Data Catalogs and Lineage', 'Data Engineering', 'Understanding where data comes from, who owns it, how it changes, and how to trace analysis outputs back to sources.'),
('data-dictionary-authoring', 'Data Dictionary Authoring', 'Data & Analytics', 'Documenting column definitions, units, allowed values, owners, caveats, and example records so analysis is reviewable.'),
('data-source-types', 'Data Source Types', 'Data & Analytics', 'Comparing transactional systems, event logs, surveys, spreadsheets, third-party data, and public datasets to choose appropriate analysis methods.'),
('data-types-and-format-standardization', 'Data Types and Format Standardization', 'Data & Analytics', 'Standardize dates, categories, currencies, text fields, and units so analysis calculations are reliable.'),
('data-validation-rules', 'Data Validation Rules', 'Data & Analytics', 'Creating checks for accepted values, nullability, uniqueness, referential logic, and business constraints.'),
('dax-and-calculated-measures', 'DAX and Calculated Measures', 'Data & Analytics', 'Creating basic DAX measures and understand filter context enough to debug common Power BI calculation mistakes.'),
('dimensional-modeling-for-analysts', 'Dimensional Modeling for Analysts', 'Data & Analytics', 'The practical scope of Dimensional Modeling for Analysts includes question framing, data preparation, metrics, analysis, validation, visualization, and communication.'),
('duplicates-and-entity-resolution', 'Duplicates and Entity Resolution', 'Data & Analytics', 'Detecting duplicate records, conflicting identifiers, and entity matching issues that can distort metrics.'),
('e-commerce-funnel-analysis', 'E-Commerce Funnel Analysis', 'Data & Analytics', 'Analyzing product views, carts, checkout, purchase conversion, and drop-off using clear funnel definitions.'),
('excel-formulas-and-functions', 'Excel Formulas and Functions', 'Data & Analytics', 'Competency in Excel Formulas and Functions requires attention to formulas, references, structured tables, pivots, validation, auditability, and clear workbook design.'),
('finance-analytics-track', 'Finance Analytics Track', 'Data & Analytics', 'Analyzing budgets, variance, revenue, margin, cost drivers, and financial reporting caveats.'),
('forecasting', 'Forecasting', 'Data & Analytics', 'Understanding trend, seasonality, baseline forecasts, forecast errors, and when forecasting assumptions are fragile.'),
('geospatial-analysis', 'Geospatial Analysis', 'Data & Analytics', 'Understanding when location data, maps, regional aggregation, and privacy constraints matter in analysis.'),
('insight-narrative', 'Insight Narrative', 'Data & Analytics', 'Writing findings that connect evidence, context, recommendation, uncertainty, and next action.'),
('interactive-python-data-apps', 'Interactive Python Data Apps', 'Programming', 'Building a lightweight Streamlit or Plotly-based data app for portfolio exploration and stakeholder demos.'),
('joins-and-relationships', 'Joins and Relationships', 'Data & Analytics', 'Using inner, left, right, and full joins while checking row counts, duplicate inflation, and unmatched records.'),
('lookup-and-reference-functions', 'Lookup and Reference Functions', 'Data & Analytics', 'Using lookup patterns to enrich datasets, validate values, and combine information from multiple sheets.'),
('marketing-analytics-track', 'Marketing Analytics Track', 'Data & Analytics', 'Analyzing campaign performance, acquisition channels, conversion rates, attribution limitations, and customer segments.'),
('metric-definitions-and-semantic-consistency', 'Metric Definitions and Semantic Consistency', 'Data & Analytics', 'Defining metrics precisely, document grain and filters, and prevent conflicting dashboard definitions.'),
('missing-data-handling', 'Missing Data Handling', 'Data & Analytics', 'Identify why values are missing and choose appropriate handling strategies without hiding bias or uncertainty.'),
('notebook-workflows', 'Notebook Workflows', 'Data & Analytics', 'Using notebooks for exploration while keeping cells ordered, assumptions documented, and outputs reproducible.'),
('numpy-for-analysis', 'NumPy for Analysis', 'Data & Analytics', 'Using arrays, vectorized operations, masks, and numerical operations that support pandas and analytical computing.'),
('operations-analytics-track', 'Operations Analytics Track', 'Data & Analytics', 'Analyzing process throughput, cycle time, capacity, defects, inventory, and operational bottlenecks.'),
('outliers-and-anomalies', 'Outliers and Anomalies', 'Data & Analytics', 'Identify unusual values, decide whether they are errors or valid extremes, and document treatment decisions.'),
('outliers-and-robust-summaries', 'Outliers and Robust Summaries', 'Data & Analytics', 'Identify outliers, compare robust statistics, and decide whether unusual values are valid signals, quality issues, or segmentation needs.'),
('pandas-time-series-analysis', 'pandas Time Series Analysis', 'Data & Analytics', 'The practical scope of pandas Time Series Analysis includes time-aware baselines, trend and seasonality, validation windows, uncertainty, and business interpretation.'),
('pivot-tables-and-summaries', 'Pivot Tables and Summaries', 'Data & Analytics', 'Creating pivot tables to summarize, segment, filter, and explore business data efficiently.'),
('practical-vs-statistical-significance', 'Practical vs Statistical Significance', 'Data & Analytics', 'Separating statistically detectable changes from changes that matter enough to justify product or business action.'),
('predictive-modeling', 'Predictive Modeling', 'Data & Analytics', 'Understanding supervised prediction, train/test split, leakage, model metrics, and why analysts should be cautious with claims.'),
('presentation-delivery', 'Presentation Delivery', 'Career & Communication', 'Present analysis in a way that answers likely stakeholder questions and separates facts, assumptions, and recommendations.'),
('privacy-and-sensitive-data-handling', 'Privacy and Sensitive Data Handling', 'Cybersecurity', 'Identify PII, reduce unnecessary exposure, and apply responsible handling practices in analysis outputs.'),
('product-analytics-track', 'Product Analytics Track', 'Data & Analytics', 'Analyzing activation, engagement, retention, funnels, cohorts, experiments, and feature impact.'),
('public-data-api-extraction', 'Public Data API Extraction', 'Data & Analytics', 'Fetch and structure public API data while handling parameters, pagination, rate limits, and reproducible extraction notes.'),
('python-basics-for-analysts', 'Python Basics for Analysts', 'Programming', 'Using variables, functions, control flow, data structures, files, and packages for analysis tasks.'),
('python-report-automation', 'Python Report Automation', 'Programming', 'Automating repeated data pulls, cleaning steps, charts, exports, and lightweight reporting scripts.'),
('query-debugging-and-performance', 'Query Debugging and Performance', 'Database', 'Debug query logic, inspect intermediate row counts, read simple execution plans, and avoid expensive analytical query mistakes.'),
('regression-analysis', 'Regression Analysis', 'Data & Analytics', 'Using regression to quantify relationships, check assumptions, inspect residuals, communicate uncertainty, and avoid overstating causality.'),
('relational-data-models', 'Relational Data Models', 'Database', 'Understanding tables, rows, columns, keys, relationships, grain, and how business events become relational data.'),
('reproducible-cleaning-workflows', 'Reproducible Cleaning Workflows', 'Data & Analytics', 'Avoid one-off manual fixes by documenting transformations in SQL, Power Query, or Python scripts.'),
('resume-and-project-positioning', 'Resume and Project Positioning', 'Career & Communication', 'Describe analyst projects with concrete business questions, tools, methods, outcomes, and trade-offs.'),
('revenue-and-pricing-analysis', 'Revenue and Pricing Analysis', 'Data & Analytics', 'Analyzing revenue, discounting, average order value, margin awareness, billing data, and pricing changes carefully.'),
('sales-analytics-track', 'Sales Analytics Track', 'Data & Analytics', 'Analyzing pipeline, conversion, revenue, quota attainment, territory performance, and sales operations metrics.'),
('sample-size-and-power', 'Sample Size and Power', 'Data & Analytics', 'Explaining how small samples and underpowered comparisons affect experiment interpretation and business confidence.'),
('schema-drift-and-source-changes', 'Schema Drift and Source Changes', 'Data & Analytics', 'Detecting changed columns, renamed fields, type shifts, and upstream source changes before they break reports.'),
('segmentation-and-clustering', 'Segmentation and Clustering', 'Data & Analytics', 'Understanding analyst-friendly segmentation and clustering concepts, validation issues, and business interpretation risks.'),
('semantic-models-and-reusable-metrics', 'Semantic Models and Reusable Metrics', 'Data & Analytics', 'Defining reusable measures, dimensions, relationships, and business logic so dashboards stay consistent across audiences.'),
('small-multiples-and-faceting', 'Small Multiples and Faceting', 'Data & Analytics', 'Using repeated chart panels to compare categories, regions, or time periods without overloading a single chart.'),
('spreadsheet-analysis-review', 'Spreadsheet Analysis Review', 'Data & Analytics', 'Spreadsheet Analysis Review is demonstrated through formulas, references, structured tables, pivots, validation, auditability, and clear workbook design.'),
('spreadsheet-charts-and-reports', 'Spreadsheet Charts and Reports', 'Data & Analytics', 'Creating basic charts and summary reports that communicate trends, comparisons, and exceptions clearly.'),
('spreadsheet-data-validation-rules', 'Spreadsheet Data Validation Rules', 'Data & Analytics', 'Using validation lists, allowed ranges, input rules, and error prompts to reduce manual spreadsheet data quality issues.'),
('spreadsheet-lookup-patterns', 'Spreadsheet Lookup Patterns', 'Data & Analytics', 'Using XLOOKUP-style patterns to enrich records, audit mismatches, and avoid brittle manual copy-paste analysis.'),
('sql-analysis-review', 'SQL Analysis Review', 'Database', 'Working with SQL Analysis Review requires sound decisions about data structures, query behavior, integrity, performance, concurrency, and operational safety.'),
('sql-data-quality-checks', 'SQL Data Quality Checks', 'Database', 'Writing SQL checks for duplicates, nulls, invalid ranges, referential mismatches, and unexpected row counts.'),
('sql-date-and-time-analysis', 'SQL Date and Time Analysis', 'Database', 'Using date extraction, intervals, truncation, rolling periods, and calendar logic for time-based business analysis.'),
('sql-interview-practice', 'SQL Interview Practice', 'Career & Communication', 'Applying joins, aggregations, windows, edge cases, and explaining query logic under interview constraints.'),
('stakeholder-interview-questions', 'Stakeholder Interview Questions', 'Career & Communication', 'Ask focused questions that uncover decision context, definitions, constraints, success criteria, and risk before doing analysis.'),
('text-cleaning-and-standardization', 'Text Cleaning and Standardization', 'Data & Analytics', 'Clean inconsistent names, categories, casing, spacing, punctuation, and labels before grouping or joining data.'),
('time-series-decomposition', 'Time Series Decomposition', 'Data & Analytics', 'Explaining trend, seasonality, residuals, smoothing, and why naive forecasts are useful baselines.'),
('visual-design-principles', 'Visual Design Principles', 'Design & UX', 'Using visual hierarchy, labels, color, whitespace, and chart simplicity to reduce cognitive load.'),
('workbook-structure-and-tables', 'Workbook Structure and Tables', 'Data & Analytics', 'Organize raw data, analysis tabs, lookup tables, outputs, and documentation in a maintainable workbook.');

INSERT INTO public.skill (name, slug, category, description, is_active)
SELECT ss.name, ss.slug, ss.category, ss.description, true
FROM seed_skill ss
WHERE NOT EXISTS (SELECT 1 FROM public.skill s WHERE s.slug = ss.slug OR s.name = ss.name);

-- Learning resources.
DROP TABLE IF EXISTS seed_resource;
CREATE TEMP TABLE seed_resource (resource_key text PRIMARY KEY, title text NOT NULL, url text NOT NULL, resource_type text NOT NULL, description text NOT NULL, provider text, difficulty_level text) ON COMMIT DROP;
INSERT INTO seed_resource VALUES
('google-data-analytics-certificate', 'Google Data Analytics Professional Certificate', 'https://www.coursera.org/professional-certificates/google-data-analytics', 'course', 'Structured beginner-friendly certificate covering data analysis process, spreadsheets, SQL, visualization, and capstone work.', 'Coursera / Google', 'beginner'),
('ibm-data-analyst-certificate', 'IBM Data Analyst Professional Certificate', 'https://www.coursera.org/professional-certificates/ibm-data-analyst', 'course', 'Professional certificate covering spreadsheets, SQL, Python, visualization, and analyst portfolio projects.', 'Coursera / IBM', 'beginner'),
('coursera-wharton-business-analytics', 'Business Analytics Specialization', 'https://www.coursera.org/specializations/business-analytics', 'course', 'Business analytics course sequence focused on decision-making with data.', 'Coursera / Wharton', 'intermediate'),
('ibm-data-governance', 'What is Data Governance?', 'https://www.ibm.com/topics/data-governance', 'article', 'Conceptual article explaining data governance roles, policies, and value.', 'IBM', 'beginner'),
('data-dbt-intro', 'What is dbt?', 'https://docs.getdbt.com/docs/introduction', 'documentation', 'dbt documentation explaining analytics engineering and transformation workflows.', 'dbt Labs', 'beginner'),
('dbt-docs', 'dbt Documentation', 'https://docs.getdbt.com/docs/introduction', 'documentation', 'Official dbt documentation for analytics engineering concepts, transformations, tests, and documentation.', 'dbt Labs', 'intermediate'),
('atlassian-stakeholder-management', 'Stakeholder Management', 'https://www.atlassian.com/work-management/project-management/stakeholder-management', 'article', 'Practical stakeholder management guide useful for analytics intake and communication.', 'Atlassian', 'beginner'),
('excel-xlookup', 'XLOOKUP Function', 'https://support.microsoft.com/en-us/office/xlookup-function-b7fd680e-6d10-43e6-84f9-88eae8bf5929', 'documentation', 'Official Microsoft reference for XLOOKUP, useful for analyst lookup workflows.', 'Microsoft Support', 'beginner'),
('cur-pandas-merge', 'Merge, join, concatenate and compare - pandas', 'https://pandas.pydata.org/docs/user_guide/merging.html', 'documentation', 'pandas guide for joining and combining datasets.', 'pandas', 'intermediate'),
('microsoft-excel-help', 'Microsoft Excel Help and Learning', 'https://support.microsoft.com/en-us/excel', 'documentation', 'Official Microsoft Excel help covering formulas, tables, pivots, charts, and workbook workflows.', 'Microsoft Support', 'beginner'),
('excel-formulas-overview', 'Overview of Formulas in Excel', 'https://support.microsoft.com/en-us/office/overview-of-formulas-in-excel-ecfdc708-9162-49e8-b993-c311f47ca173', 'documentation', 'Official guide to Excel formulas and calculation concepts.', 'Microsoft Support', 'beginner'),
('cur-excel-formulas', 'Overview of formulas in Excel', 'https://support.microsoft.com/en-us/office/overview-of-formulas-in-excel-ecfdc708-9162-49e8-b993-c311f47ca173', 'documentation', 'Microsoft guide to formulas and functions in Excel.', 'Microsoft Support', 'beginner'),
('excel-pivottables-guide', 'Create a PivotTable to Analyze Worksheet Data', 'https://support.microsoft.com/en-us/office/create-a-pivottable-to-analyze-worksheet-data-a9a84538-bfe9-40a9-a8e9-f99134456576', 'documentation', 'Official PivotTable guide for summarizing and exploring spreadsheet data.', 'Microsoft Support', 'beginner'),
('data-excel-pivottable', 'Create a PivotTable to analyze worksheet data', 'https://support.microsoft.com/en-us/excel/get-started/create-a-pivottable-to-analyze-worksheet-data', 'documentation', 'Microsoft support guide for creating PivotTables to summarize and analyze data.', 'Microsoft Support', 'beginner'),
('cur-excel-pivot', 'Create a PivotTable to analyze worksheet data', 'https://support.microsoft.com/en-us/office/create-a-pivottable-to-analyze-worksheet-data-a9a84538-bfe9-40a9-a8e9-f99134456576', 'documentation', 'Microsoft guide for creating PivotTables.', 'Microsoft Support', 'beginner'),
('power-query-docs', 'Power Query Documentation', 'https://learn.microsoft.com/en-us/power-query/', 'documentation', 'Official documentation for Power Query data import, transformation, and cleaning workflows.', 'Microsoft Learn', 'intermediate'),
('power-bi-docs', 'Power BI Documentation', 'https://learn.microsoft.com/en-us/power-bi/', 'documentation', 'Official Power BI documentation for reports, semantic models, dashboards, and administration.', 'Microsoft Learn', 'beginner'),
('sqlbolt', 'SQLBolt Interactive Lessons', 'https://sqlbolt.com/', 'practice', 'Interactive SQL practice lessons for SELECT, filtering, joins, aggregates, and table operations.', 'SQLBolt', 'beginner'),
('data-sqlbolt', 'SQLBolt Interactive Lessons', 'https://sqlbolt.com/', 'practice', 'Interactive SQL practice lessons for SELECT, filtering, joins, aggregates, and table operations.', 'SQLBolt', 'beginner'),
('mode-sql-tutorial', 'Mode SQL Tutorial', 'https://mode.com/sql-tutorial/', 'course', 'SQL tutorial focused on analytical querying and business data analysis.', 'Mode', 'beginner'),
('postgres-datetime-functions', 'PostgreSQL Date/Time Functions and Operators', 'https://www.postgresql.org/docs/current/functions-datetime.html', 'documentation', 'Official PostgreSQL reference for date and time analysis in SQL.', 'PostgreSQL', 'intermediate'),
('postgres-window-functions', 'PostgreSQL Window Functions', 'https://www.postgresql.org/docs/current/tutorial-window.html', 'documentation', 'Official PostgreSQL tutorial for analytical window functions.', 'PostgreSQL', 'intermediate'),
('kaggle-advanced-sql', 'Kaggle Learn: Advanced SQL', 'https://www.kaggle.com/learn/advanced-sql', 'course', 'Hands-on SQL course covering joins, unions, analytic functions, and nested queries.', 'Kaggle', 'intermediate'),
('khan-statistics', 'Khan Academy Statistics and Probability', 'https://www.khanacademy.org/math/statistics-probability', 'course', 'Beginner-friendly statistics and probability lessons for analysts.', 'Khan Academy', 'beginner'),
('data-khan-statistics', 'Khan Academy Statistics and Probability', 'https://www.khanacademy.org/math/statistics-probability', 'course', 'Beginner-friendly course for probability, sampling, distributions, and statistics.', 'Khan Academy', 'beginner'),
('cur-khan-statistics', 'Khan Academy Statistics and Probability', 'https://www.khanacademy.org/math/statistics-probability', 'course', 'Statistics and probability course for foundational concepts.', 'Khan Academy', 'beginner'),
('google-ab-testing-course', 'Google Analytics Academy', 'https://analytics.google.com/analytics/academy/', 'course', 'Google Analytics learning resources useful for product and marketing analytics context.', 'Google', 'beginner'),
('openintro-statistics', 'OpenIntro Statistics', 'https://www.openintro.org/book/os/', 'book', 'Free statistics textbook with practical examples and exercises.', 'OpenIntro', 'beginner'),
('statistics-how-to-power', 'Statistical Power', 'https://www.statisticshowto.com/probability-and-statistics/statistics-definitions/statistical-power/', 'article', 'Focused explanation of statistical power for experiment interpretation.', 'Statistics How To', 'intermediate'),
('scipy-stats', 'SciPy Statistical Functions', 'https://docs.scipy.org/doc/scipy/reference/stats.html', 'documentation', 'Official SciPy statistical functions reference for Python analysis.', 'SciPy', 'intermediate'),
('statsmodels-docs', 'statsmodels Documentation', 'https://www.statsmodels.org/stable/index.html', 'documentation', 'Python statistical modeling documentation for regression, tests, and time series analysis.', 'statsmodels', 'intermediate'),
('great-expectations-docs', 'Great Expectations Documentation', 'https://docs.greatexpectations.io/docs/', 'documentation', 'Data validation documentation for expectations, checks, and quality rules.', 'Great Expectations', 'intermediate'),
('dbt-tests', 'dbt Data Tests', 'https://docs.getdbt.com/docs/build/data-tests', 'documentation', 'Official dbt documentation for validating transformed analytical datasets.', 'dbt Labs', 'intermediate'),
('cur-dbt-tests', 'Data tests - dbt', 'https://docs.getdbt.com/docs/build/data-tests', 'documentation', 'dbt documentation for data tests and quality checks.', 'dbt Labs', 'intermediate'),
('pandas-missing-data', 'pandas Working with Missing Data', 'https://pandas.pydata.org/docs/user_guide/missing_data.html', 'documentation', 'Official pandas guide for missing data handling.', 'pandas', 'intermediate'),
('cur-pandas-missing', 'Working with missing data - pandas', 'https://pandas.pydata.org/docs/user_guide/missing_data.html', 'documentation', 'pandas guide for missing-data handling.', 'pandas', 'intermediate'),
('pandas-text-data', 'pandas Working with Text Data', 'https://pandas.pydata.org/docs/user_guide/text.html', 'documentation', 'Official pandas guide for string operations and text cleaning.', 'pandas', 'intermediate'),
('kaggle-intro-sql', 'Kaggle Learn: Intro to SQL', 'https://www.kaggle.com/learn/intro-to-sql', 'course', 'Hands-on SQL micro-course for analytical querying fundamentals.', 'Kaggle', 'beginner'),
('storytelling-with-data-blog', 'Storytelling with Data Blog', 'https://www.storytellingwithdata.com/blog', 'article', 'Practical articles on communicating data clearly through charts and narrative.', 'Storytelling with Data', 'beginner'),
('data-to-viz', 'From Data to Viz', 'https://www.data-to-viz.com/', 'article', 'Practical guide for choosing charts based on data shape and analytical purpose.', 'From Data to Viz', 'beginner'),
('data-kaggle-visualization', 'Kaggle Learn: Data Visualization', 'https://www.kaggle.com/learn/data-visualization', 'course', 'Hands-on Kaggle course for practical data visualization.', 'Kaggle Learn', 'beginner'),
('w3c-accessibility-intro', 'Introduction to Web Accessibility', 'https://www.w3.org/WAI/fundamentals/accessibility-intro/', 'article', 'W3C introduction to accessibility principles that also apply to shared dashboards and reports.', 'W3C WAI', 'beginner'),
('wcag-understanding', 'Understanding WCAG 2.2', 'https://www.w3.org/WAI/WCAG22/Understanding/', 'documentation', 'Detailed accessibility guidance useful for dashboard readability and accessible reporting.', 'W3C WAI', 'intermediate'),
('cur-mdn-accessibility', 'Accessibility - MDN', 'https://developer.mozilla.org/en-US/docs/Learn_web_development/Core/Accessibility', 'documentation', 'MDN guide for accessible web content.', 'MDN Web Docs', 'beginner'),
('nngroup-dashboards', 'Dashboards: Making Charts and Graphs Easier to Understand', 'https://www.nngroup.com/articles/dashboards/', 'article', 'User experience article on making dashboards easier to interpret.', 'Nielsen Norman Group', 'intermediate'),
('tableau-help', 'Tableau Desktop and Web Authoring Help', 'https://help.tableau.com/current/pro/desktop/en-us/', 'documentation', 'Official Tableau authoring documentation for charts, dashboards, calculations, and publishing.', 'Tableau', 'intermediate'),
('tableau-visual-best-practices', 'Tableau Visual Best Practices', 'https://help.tableau.com/current/pro/desktop/en-us/visual_best_practices.htm', 'documentation', 'Official Tableau guidance for clear and effective visual analysis.', 'Tableau', 'intermediate'),
('matplotlib-docs', 'Matplotlib Pyplot Tutorial', 'https://matplotlib.org/stable/tutorials/pyplot.html', 'documentation', 'Matplotlib tutorial for plotting values, labels, styles, and basic charts.', 'Matplotlib', 'beginner'),
('tableau-training', 'Tableau Training Videos', 'https://www.tableau.com/learn/training', 'video', 'Official Tableau training videos for visual analytics and dashboard workflows.', 'Tableau', 'beginner'),
('looker-studio-help', 'Looker Studio Help', 'https://support.google.com/looker-studio', 'documentation', 'Official Looker Studio help for reports, data sources, calculated fields, and sharing.', 'Google Help', 'beginner'),
('looker-docs', 'Looker Documentation', 'https://cloud.google.com/looker/docs', 'documentation', 'Official Looker documentation for governed BI modeling and analytics workflows.', 'Google Cloud', 'intermediate'),
('tableau-performance-recording', 'Create a Performance Recording', 'https://help.tableau.com/current/pro/desktop/en-us/perf_record_create_desktop.htm', 'documentation', 'Official Tableau documentation for diagnosing dashboard performance.', 'Tableau', 'advanced'),
('power-bi-guidance', 'Power BI Guidance Documentation', 'https://learn.microsoft.com/en-us/power-bi/guidance/', 'documentation', 'Official Power BI implementation, modeling, and performance guidance.', 'Microsoft Learn', 'intermediate'),
('python-docs', 'Python Tutorial', 'https://docs.python.org/3/tutorial/index.html', 'documentation', 'Official Python tutorial covering language basics, data structures, modules, and exceptions.', 'Python', 'beginner'),
('python-tutorial', 'Python Tutorial', 'https://docs.python.org/3/tutorial/', 'documentation', 'Official Python tutorial for programming fundamentals.', 'Python', 'beginner'),
('common-python-tutorial', 'The Python Tutorial', 'https://docs.python.org/3/tutorial/index.html', 'documentation', 'Official Python tutorial for syntax, functions, modules, errors, and standard language features.', 'Python', 'beginner'),
('cur-matplotlib-quickstart', 'Matplotlib Quick Start', 'https://matplotlib.org/stable/users/explain/quick_start.html', 'documentation', 'Matplotlib quick start for plotting and figures.', 'Matplotlib', 'beginner'),
('seaborn-docs', 'seaborn Documentation', 'https://seaborn.pydata.org/', 'documentation', 'Official seaborn documentation for statistical data visualization.', 'seaborn', 'beginner'),
('census-data-api', 'Census Data API User Guide', 'https://www.census.gov/data/developers/guidance/api-user-guide.html', 'documentation', 'Official Census API guide useful for public-data analysis practice.', 'U.S. Census Bureau', 'intermediate'),
('world-bank-api', 'World Bank API Documentation', 'https://datahelpdesk.worldbank.org/knowledgebase/topics/125589-developer-information', 'documentation', 'Official World Bank developer documentation for public economic datasets.', 'World Bank', 'intermediate'),
('ibm-data-quality', 'What is Data Quality?', 'https://www.ibm.com/topics/data-quality', 'article', 'Conceptual article explaining dimensions and business impact of data quality.', 'IBM', 'beginner'),
('google-analytics-help', 'Google Analytics Help', 'https://support.google.com/analytics/', 'documentation', 'Official Google Analytics help for product, marketing, traffic, and conversion metrics.', 'Google Help', 'beginner'),
('nngroup-analytics-reports', 'Analytics and User Experience Articles', 'https://www.nngroup.com/topic/analytics/', 'article', 'Credible UX analytics articles for interpreting user behavior data.', 'Nielsen Norman Group', 'intermediate'),
('google-analytics-events', 'Google Analytics Events', 'https://support.google.com/analytics/answer/9322688', 'documentation', 'Official GA4 guide for understanding event-based product and marketing data.', 'Google Help', 'intermediate'),
('shopify-analytics', 'Shopify Analytics', 'https://help.shopify.com/en/manual/reports-and-analytics/shopify-reports', 'documentation', 'Official Shopify guide for e-commerce reporting and analytics concepts.', 'Shopify Help Center', 'beginner'),
('stripe-analytics-docs', 'Stripe Sigma Documentation', 'https://docs.stripe.com/stripe-data/query-billing-data', 'documentation', 'Official Stripe documentation for querying billing and revenue data examples.', 'Stripe Docs', 'intermediate'),
('pandas-timeseries', 'pandas Time Series / Date Functionality', 'https://pandas.pydata.org/docs/user_guide/timeseries.html', 'documentation', 'Official pandas guide for time-indexed data and date operations.', 'pandas', 'intermediate'),
('statsmodels-time-series', 'statsmodels Time Series Analysis', 'https://www.statsmodels.org/stable/tsa.html', 'documentation', 'Official statsmodels time series analysis documentation.', 'statsmodels', 'advanced'),
('prophet-docs', 'Prophet Documentation', 'https://facebook.github.io/prophet/', 'documentation', 'Forecasting documentation for practical analyst-level time series modeling.', 'Prophet', 'intermediate'),
('scikit-learn-user-guide', 'scikit-learn User Guide', 'https://scikit-learn.org/stable/user_guide.html', 'documentation', 'Official scikit-learn user guide for analyst-level predictive modeling awareness.', 'scikit-learn', 'intermediate'),
('scikit-learn-model-evaluation', 'scikit-learn Model Evaluation', 'https://scikit-learn.org/stable/modules/model_evaluation.html', 'documentation', 'Official scikit-learn evaluation metrics reference.', 'scikit-learn', 'intermediate'),
('ml-sklearn-metrics', 'Model Evaluation - scikit-learn', 'https://scikit-learn.org/stable/modules/model_evaluation.html', 'documentation', 'scikit-learn documentation for classification, regression, and clustering metrics.', 'scikit-learn', 'intermediate'),
('common-github-readme', 'About READMEs - GitHub Docs', 'https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-readmes', 'documentation', 'GitHub documentation for writing useful repository README files.', 'GitHub Docs', 'beginner'),
('cur-github-readme', 'About READMEs - GitHub Docs', 'https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-readmes', 'documentation', 'GitHub documentation for repository README files and project evidence.', 'GitHub Docs', 'beginner'),
('github-writing-readmes', 'About READMEs', 'https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-readmes', 'documentation', 'GitHub documentation for writing clear project README files.', 'GitHub Docs', 'beginner'),
('sup-github-actions', 'GitHub Actions: Understanding Workflows', 'https://docs.github.com/en/actions/get-started/quickstart/get-started/understand-github-actions', 'documentation', 'GitHub Actions documentation for workflows, jobs, runners, events, and automation pipelines.', 'GitHub Docs', 'intermediate'),
('sup-github-flow', 'GitHub Flow', 'https://docs.github.com/en/get-started/using-github/github-flow', 'documentation', 'Official GitHub guide for branch, pull request, review, and merge workflow.', 'GitHub Docs', 'beginner'),
('sup-google-code-review', 'Google Engineering Practices: Code Review', 'https://google.github.io/eng-practices/review/', 'documentation', 'Google guidance for code reviews, pull requests, reviewer expectations, and review quality.', 'Google Engineering Practices', 'intermediate'),
('sup-numpy-beginners', 'NumPy: Absolute Beginners', 'https://numpy.org/doc/stable/user/absolute_beginners.html', 'documentation', 'Official NumPy guide for arrays, indexing, vectorized operations, and numerical analysis basics.', 'NumPy', 'beginner'),
('sup-openintro-statistics', 'OpenIntro Statistics', 'https://www.openintro.org/book/os/', 'book', 'Free statistics textbook with probability, sampling, hypothesis testing, regression, and exercises.', 'OpenIntro', 'beginner'),
('sup-pro-git-branching', 'Pro Git: Branching and Merging', 'https://git-scm.com/book/en/v2/Git-Branching-Branches-in-a-Nutshell', 'book', 'Official Pro Git chapter covering branches, merging, and collaborative version-control workflows.', 'Git', 'beginner'),
('sup-scipy-stats', 'SciPy Statistical Functions', 'https://docs.scipy.org/doc/scipy/reference/stats.html', 'documentation', 'Official SciPy statistics reference for distributions, hypothesis tests, and statistical functions.', 'SciPy', 'intermediate'),
('sup-google-tech-writing-one', 'Technical Writing One', 'https://developers.google.com/tech-writing/one', 'course', 'Google course for clear technical writing, documentation structure, and audience-focused explanations.', 'Google Developers', 'beginner'),
('sup-pandas-user-guide', 'pandas User Guide', 'https://pandas.pydata.org/docs/user_guide/index.html', 'documentation', 'Official pandas user guide for DataFrames, indexing, missing data, grouping, merging, and analysis.', 'pandas', 'intermediate'),
('sup-postgres-tutorial', 'PostgreSQL Tutorial', 'https://www.postgresql.org/docs/current/tutorial.html', 'documentation', 'Official PostgreSQL tutorial for relational tables, SQL queries, joins, aggregates, and transactions.', 'PostgreSQL', 'beginner'),
('sup-python-unittest', 'unittest: Unit testing framework', 'https://docs.python.org/3/library/unittest.html', 'documentation', 'Official Python unittest documentation for test cases, fixtures, assertions, and test suites.', 'Python', 'beginner'),
('sup-statsmodels-docs', 'statsmodels Documentation', 'https://www.statsmodels.org/stable/index.html', 'documentation', 'statsmodels documentation for regression, statistical tests, time series, and model diagnostics.', 'statsmodels', 'intermediate');

UPDATE public.learning_resource lr
SET title = sr.title,
    resource_type = sr.resource_type,
    description = sr.description,
    provider = sr.provider,
    difficulty_level = sr.difficulty_level,
    language_code = 'en',
    verification_status = 'verified',
    updated_at = now()
FROM (SELECT DISTINCT ON (url) title, url, resource_type, description, provider, difficulty_level FROM seed_resource ORDER BY url, resource_key) sr
WHERE lr.url = sr.url;

INSERT INTO public.learning_resource (title, url, resource_type, description, provider, difficulty_level, language_code, verification_status)
SELECT sr.title, sr.url, sr.resource_type, sr.description, sr.provider, sr.difficulty_level, 'en', 'verified'
FROM (SELECT DISTINCT ON (url) title, url, resource_type, description, provider, difficulty_level FROM seed_resource ORDER BY url, resource_key) sr
WHERE NOT EXISTS (SELECT 1 FROM public.learning_resource lr WHERE lr.url = sr.url);

-- Roadmap nodes.
DROP TABLE IF EXISTS seed_node;
CREATE TEMP TABLE seed_node (
    node_key text PRIMARY KEY,
    parent_key text,
    order_index int NOT NULL,
    node_type text NOT NULL,
    checkpoint_type text,
    selection_type text,
    required_count int,
    title text NOT NULL,
    description text NOT NULL,
    layout_role text,
    estimated_hours int,
    difficulty_level text,
    metadata jsonb,
    is_required boolean,
    is_trackable boolean,
    learning_outcomes jsonb,
    completion_criteria jsonb,
    node_id uuid DEFAULT gen_random_uuid()
) ON COMMIT DROP;
INSERT INTO seed_node
(node_key, parent_key, order_index, node_type, checkpoint_type, selection_type, required_count, title, description, layout_role, estimated_hours, difficulty_level, metadata, is_required, is_trackable, learning_outcomes, completion_criteria)
VALUES
('ph-da-foundations', NULL, 1, 'phase', NULL, NULL, NULL, 'Data Analyst Foundations', 'Understand the data analyst role, business context, analytics lifecycle, and how analysis supports decision-making.', 'trunk', 24, 'beginner', '{"generatedBy": "roadmap-platform", "visualRole": "trunk"}'::jsonb, true, false, '["Explain the purpose and practical use of Data Analyst Foundations.", "Apply Data Analyst Foundations to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Data Analyst Foundations in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('grp-da-role-context', 'ph-da-foundations', 1, 'choice_group', NULL, 'complete_all', 3, 'Role, Business Context, and Intake', 'Organize role expectations, stakeholder intake, and the analytics lifecycle before technical analysis starts.', 'side', 5, 'beginner', '{"generatedBy": "roadmap-platform", "groupingPurpose": "phase_topic_group", "selectionType": "complete_all"}'::jsonb, true, false, '["Explain how the topics in Role, Business Context, and Intake work together in a data analyst workflow.", "Apply the grouped role, business context, and intake skills to a practical analysis task."]'::jsonb, '["Complete all required child nodes in Role, Business Context, and Intake.", "Produce notes, examples, or project evidence that connects the grouped skills."]'::jsonb),
('grp-da-thinking-ethics', 'ph-da-foundations', 2, 'choice_group', NULL, 'complete_all', 3, 'Analytical Thinking, KPIs, and Responsible Use', 'Group the decision-framing, KPI, bias, ethics, and reasoning skills used across every analysis task.', 'side', 6, 'beginner', '{"generatedBy": "roadmap-platform", "groupingPurpose": "phase_topic_group", "selectionType": "complete_all"}'::jsonb, true, false, '["Explain how the topics in Analytical Thinking, KPIs, and Responsible Use work together in a data analyst workflow.", "Apply the grouped analytical thinking, kpis, and responsible use skills to a practical analysis task."]'::jsonb, '["Complete all required child nodes in Analytical Thinking, KPIs, and Responsible Use.", "Produce notes, examples, or project evidence that connects the grouped skills."]'::jsonb),
('data-analyst-role', 'grp-da-role-context', 1, 'topic', NULL, NULL, NULL, 'Data Analyst Role and Responsibilities', 'Compare data analyst responsibilities with BI, data science, and data engineering roles, and identify the expected deliverables in business teams.', 'choice', 3, 'beginner', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Data Analyst Role and Responsibilities.", "Apply Data Analyst Role and Responsibilities to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Data Analyst Role and Responsibilities in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('analytics-lifecycle', 'grp-da-role-context', 2, 'topic', NULL, NULL, NULL, 'Analytics Lifecycle', 'Follow the path from question intake and data discovery to analysis, validation, communication, and follow-up measurement.', 'choice', 4, 'beginner', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Analytics Lifecycle.", "Apply Analytics Lifecycle to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Analytics Lifecycle in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('business-questions-and-kpis', 'grp-da-thinking-ethics', 1, 'topic', NULL, NULL, NULL, 'Business Questions and KPIs', 'Turn vague requests into measurable questions, define KPIs, and identify what decision the analysis should support.', 'choice', 4, 'beginner', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Business Questions and KPIs.", "Apply Business Questions and KPIs to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Business Questions and KPIs in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('data-literacy-and-ethics', 'grp-da-thinking-ethics', 2, 'topic', NULL, NULL, NULL, 'Data Literacy and Responsible Use', 'Understand data types, collection limitations, bias, privacy, and responsible interpretation before analysis begins.', 'choice', 4, 'beginner', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Data Literacy and Responsible Use.", "Apply Data Literacy and Responsible Use to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Data Literacy and Responsible Use in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('requirements-intake', 'grp-da-role-context', 3, 'topic', NULL, NULL, NULL, 'Analytics Requirements Intake', 'Clarify stakeholders, scope, definitions, constraints, deadline, expected output, and acceptance criteria before analysis work starts.', 'choice', 3, 'beginner', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Analytics Requirements Intake.", "Apply Analytics Requirements Intake to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Analytics Requirements Intake in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('analytical-thinking-basics', 'grp-da-thinking-ethics', 3, 'topic', NULL, NULL, NULL, 'Analytical Thinking Basics', 'Break a business problem into hypotheses, segments, comparisons, assumptions, and measurable evidence.', 'choice', 4, 'beginner', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Analytical Thinking Basics.", "Apply Analytical Thinking Basics to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Analytical Thinking Basics in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('chk-da-foundations-review', 'ph-da-foundations', 90, 'checkpoint', 'review', NULL, NULL, 'Data Analyst Foundations Review', 'Review checkpoint for Data Analyst Foundations. The learner synthesizes completed topics such as Data Analyst Role and Responsibilities, Analytics Lifecycle, Analytics Requirements Intake, Business Questions and KPIs, records evidence, and updates the learning plan before continuing.', 'checkpoint', 2, 'beginner', '{"generatedBy": "roadmap-platform", "checkpointType": "review", "checkpointPurpose": "phase_review", "reviewMode": "review", "evidenceRequired": true, "claimLimit": "orientation_and_progress_review_only_not_certification"}'::jsonb, true, true, '["Explain how topics from Data Analyst Foundations connect to realistic data analysis work.", "Select evidence that shows attempted practice, reasoning, or implementation decisions.", "Identify which topics need review before moving to later roadmap areas."]'::jsonb, '["Write a concise review of the main ideas from this segment.", "Include one evidence artifact such as notes, a diagram, lab output, code, query results, screenshots, or a project review.", "Record open questions, weak areas, or follow-up actions.", "Mark the checkpoint complete only after the review artifact and next steps are captured."]'::jsonb),
('proj-analytics-question-brief', 'ph-da-foundations', 100, 'project', NULL, NULL, NULL, 'Required Analytics Question Brief', 'Milestone project: Write a one-page analytics brief that defines a stakeholder question, KPI, target audience, assumptions, data needed, and decision supported.', 'side', 5, 'beginner', '{"project":true,"projectBrief":"Write a one-page analytics brief that defines a stakeholder question, KPI, target audience, assumptions, data needed, and decision supported.","projectKind":"analytics_artifact","projectScope":"focused_milestone","projectPurpose":"Demonstrate the surrounding roadmap segment through a focused milestone artifact.","phaseContext":"Data Analyst Foundations","deliverable":"a Analytics Question Brief workbook, report, or dashboard with documented assumptions","skillsToPractice":["Stakeholder Communication","Business Metrics"],"suggestedSteps":["Define the Analytics Question Brief scenario and list the specific items to demonstrate: KPI definitions.","Define the business question, audience, grain, metric formulas, and decision the artifact should support.","Prepare or clean the dataset and document assumptions, exclusions, and data-quality checks.","Build the report, dashboard, notebook, or workbook with the minimum views needed to answer the question.","Validate the numbers against source rows, spot checks, or reconciliation notes.","Write a short interpretation with caveats and recommended next actions.","Keep the scope focused enough to complete, review, and explain the a Analytics Question Brief workbook, report, or dashboard with documented assumptions."],"expectedEvidence":["a Analytics Question Brief workbook, report, or dashboard with documented assumptions.","README or notes that explain setup, usage, assumptions, and expected output.","Metric definitions, charts/tables, source checks, and a concise interpretation for the intended audience.","Validation evidence such as tests, screenshots, logs, query results, metrics, traces, or review notes.","Short reflection on trade-offs, limitations, and one reasonable next improvement."],"reviewFocus":["Does the artifact directly match the project brief?","Can another person run, inspect, or review the result from the provided notes?","Are assumptions, limitations, and trade-offs stated clearly?"],"scopeGuardrails":["Keep the implementation focused on the stated project brief.","Prefer a complete small artifact over a broad unfinished system.","Treat completion as reviewable evidence, not a certification claim."],"claimLimit":"Completion creates reviewable project evidence. It does not certify mastery or guarantee job readiness.","evidenceRequired":true,"optionalPractice":false,"milestone":true,"nodeKey":"proj-analytics-question-brief"}'::jsonb, true, true, '["Apply Stakeholder Communication, Business Metrics through a focused Analytics Question Brief artifact.","Explain the main design, data, tooling, workflow, or review decisions behind the project.","Validate the result with evidence that another person can inspect.","Document assumptions, limitations, and a concrete follow-up improvement."]'::jsonb, '["The a Analytics Question Brief workbook, report, or dashboard with documented assumptions is available for review.","The Analytics Question Brief scope, setup path, input data or scenario, and expected output are documented.","The main workflow runs or can be inspected through a notebook, repository, dashboard, prototype, report, configuration, or case study.","Validation evidence is included and directly relates to the project brief.","The notes identify trade-offs, limitations, and one next improvement without claiming mastery or job readiness."]'::jsonb),
('ph-spreadsheets', NULL, 2, 'phase', NULL, NULL, NULL, 'Spreadsheets and Excel Analysis', 'Build the spreadsheet fluency needed for quick analysis, cleaning, summaries, and stakeholder-friendly reporting.', 'trunk', 34, 'beginner', '{"generatedBy": "roadmap-platform", "visualRole": "trunk"}'::jsonb, true, false, '["Explain the purpose and practical use of Spreadsheets and Excel Analysis.", "Apply Spreadsheets and Excel Analysis to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Spreadsheets and Excel Analysis in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('grp-spreadsheet-foundations', 'ph-spreadsheets', 1, 'choice_group', NULL, 'complete_all', 3, 'Spreadsheet Foundations', 'Build the spreadsheet mechanics needed for clean and reliable analysis workbooks.', 'side', 6, 'beginner', '{"generatedBy": "roadmap-platform", "groupingPurpose": "phase_topic_group", "selectionType": "complete_all"}'::jsonb, true, false, '["Explain how the topics in Spreadsheet Foundations work together in a data analyst workflow.", "Apply the grouped spreadsheet foundations skills to a practical analysis task."]'::jsonb, '["Complete all required child nodes in Spreadsheet Foundations.", "Produce notes, examples, or project evidence that connects the grouped skills."]'::jsonb),
('grp-spreadsheet-analysis-reporting', 'ph-spreadsheets', 2, 'choice_group', NULL, 'complete_all', 4, 'Spreadsheet Analysis and Reporting', 'Group spreadsheet summarization, cleaning, transformation, and report-building skills.', 'side', 9, 'beginner', '{"generatedBy": "roadmap-platform", "groupingPurpose": "phase_topic_group", "selectionType": "complete_all"}'::jsonb, true, false, '["Explain how the topics in Spreadsheet Analysis and Reporting work together in a data analyst workflow.", "Apply the grouped spreadsheet analysis and reporting skills to a practical analysis task."]'::jsonb, '["Complete all required child nodes in Spreadsheet Analysis and Reporting.", "Produce notes, examples, or project evidence that connects the grouped skills."]'::jsonb),
('spreadsheet-structure', 'grp-spreadsheet-foundations', 1, 'topic', NULL, NULL, NULL, 'Workbook Structure and Tables', 'Organize raw data, analysis tabs, lookup tables, outputs, and documentation in a maintainable workbook.', 'choice', 3, 'beginner', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Workbook Structure and Tables.", "Apply Workbook Structure and Tables to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Workbook Structure and Tables in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('excel-formulas-functions', 'grp-spreadsheet-foundations', 2, 'topic', NULL, NULL, NULL, 'Excel Formulas and Functions', 'Use formulas for calculations, conditional logic, text operations, dates, error handling, and reusable analysis fields.', 'choice', 5, 'beginner', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Excel Formulas and Functions.", "Apply Excel Formulas and Functions to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Excel Formulas and Functions in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('lookup-and-reference-functions', 'grp-spreadsheet-foundations', 3, 'topic', NULL, NULL, NULL, 'Lookup and Reference Functions', 'Use lookup patterns to enrich datasets, validate values, and combine information from multiple sheets.', 'choice', 4, 'beginner', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Lookup and Reference Functions.", "Apply Lookup and Reference Functions to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Lookup and Reference Functions in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('pivot-tables', 'grp-spreadsheet-analysis-reporting', 1, 'topic', NULL, NULL, NULL, 'Pivot Tables and Summaries', 'Create pivot tables to summarize, segment, filter, and explore business data efficiently.', 'choice', 5, 'beginner', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Pivot Tables and Summaries.", "Apply Pivot Tables and Summaries to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Pivot Tables and Summaries in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('spreadsheet-cleaning', 'grp-spreadsheet-analysis-reporting', 2, 'topic', NULL, NULL, NULL, 'Spreadsheet Data Cleaning', 'Identify duplicates, inconsistent values, missing data, bad formats, and manual entry errors in spreadsheets.', 'choice', 4, 'beginner', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Spreadsheet Data Cleaning.", "Apply Spreadsheet Data Cleaning to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Spreadsheet Data Cleaning in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('power-query-basics', 'grp-spreadsheet-analysis-reporting', 3, 'topic', NULL, NULL, NULL, 'Power Query Basics', 'Import, reshape, merge, clean, and document repeatable spreadsheet transformation workflows.', 'choice', 5, 'beginner', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Power Query Basics.", "Apply Power Query Basics to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Power Query Basics in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('spreadsheet-charts', 'grp-spreadsheet-analysis-reporting', 4, 'topic', NULL, NULL, NULL, 'Spreadsheet Charts and Reports', 'Create basic charts and summary reports that communicate trends, comparisons, and exceptions clearly.', 'choice', 4, 'beginner', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Spreadsheet Charts and Reports.", "Apply Spreadsheet Charts and Reports to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Spreadsheet Charts and Reports in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('chk-spreadsheet-review', 'ph-spreadsheets', 90, 'checkpoint', 'assessment', NULL, NULL, 'Spreadsheet Analysis Review', 'Practical checkpoint for Spreadsheets and Excel Analysis. The learner completes a small diagnostic task or review artifact using Workbook Structure and Tables, Excel Formulas and Functions, Lookup and Reference Functions, Pivot Tables and Summaries and records observations that can guide further practice.', 'checkpoint', 2, 'beginner', '{"generatedBy": "roadmap-platform", "checkpointType": "assessment", "checkpointPurpose": "phase_review", "reviewMode": "assessment", "evidenceRequired": true, "claimLimit": "orientation_and_progress_review_only_not_certification"}'::jsonb, true, true, '["Apply selected concepts from Spreadsheets and Excel Analysis to a small practical scenario.", "Explain what worked, what failed, and which signals were used to debug or validate the result.", "Translate the result into a short next-action plan for continued learning."]'::jsonb, '["Complete a small practical task, lab, review exercise, or worked example related to this segment.", "Attach or describe the output, result, or evidence used for review.", "Explain at least one mistake, trade-off, or failure mode encountered during the task.", "Write the next practice action based on the result."]'::jsonb),
('proj-excel-sales-analysis', 'ph-spreadsheets', 100, 'project', NULL, NULL, NULL, 'Required Excel Sales Analysis Report', 'Milestone project: Analyze a sales dataset in Excel using cleaning steps, formulas, pivot tables, charts, and a short written recommendation.', 'side', 7, 'beginner', '{"project":true,"projectBrief":"Analyze a sales dataset in Excel using cleaning steps, formulas, pivot tables, charts, and a short written recommendation.","projectKind":"analytics_artifact","projectScope":"focused_milestone","projectPurpose":"Demonstrate the surrounding roadmap segment through a focused milestone artifact.","phaseContext":"Spreadsheets and Excel Analysis","deliverable":"a Excel Sales Analysis Report workbook, report, or dashboard with documented assumptions","skillsToPractice":["Excel","Spreadsheets","Data Storytelling"],"suggestedSteps":["Define the Excel Sales Analysis Report scenario, target user or reviewer, and the specific Spreadsheets and Excel Analysis concepts it will demonstrate.","Define the business question, audience, grain, metric formulas, and decision the artifact should support.","Prepare or clean the dataset and document assumptions, exclusions, and data-quality checks.","Build the report, dashboard, notebook, or workbook with the minimum views needed to answer the question.","Validate the numbers against source rows, spot checks, or reconciliation notes.","Write a short interpretation with caveats and recommended next actions.","Keep the scope focused enough to complete, review, and explain the a Excel Sales Analysis Report workbook, report, or dashboard with documented assumptions."],"expectedEvidence":["a Excel Sales Analysis Report workbook, report, or dashboard with documented assumptions.","README or notes that explain setup, usage, assumptions, and expected output.","Metric definitions, charts/tables, source checks, and a concise interpretation for the intended audience.","Validation evidence such as tests, screenshots, logs, query results, metrics, traces, or review notes.","Short reflection on trade-offs, limitations, and one reasonable next improvement."],"reviewFocus":["Does the artifact directly match the project brief?","Can another person run, inspect, or review the result from the provided notes?","Are assumptions, limitations, and trade-offs stated clearly?"],"scopeGuardrails":["Keep the implementation focused on the stated project brief.","Prefer a complete small artifact over a broad unfinished system.","Treat completion as reviewable evidence, not a certification claim."],"claimLimit":"Completion creates reviewable project evidence. It does not certify mastery or guarantee job readiness.","evidenceRequired":true,"optionalPractice":false,"milestone":true,"nodeKey":"proj-excel-sales-analysis"}'::jsonb, true, true, '["Apply Excel, Spreadsheets, Data Storytelling through a focused Excel Sales Analysis Report artifact.","Explain the main design, data, tooling, workflow, or review decisions behind the project.","Validate the result with evidence that another person can inspect.","Document assumptions, limitations, and a concrete follow-up improvement."]'::jsonb, '["The a Excel Sales Analysis Report workbook, report, or dashboard with documented assumptions is available for review.","The Excel Sales Analysis Report scope, setup path, input data or scenario, and expected output are documented.","The main workflow runs or can be inspected through a notebook, repository, dashboard, prototype, report, configuration, or case study.","Validation evidence is included and directly relates to the project brief.","The notes identify trade-offs, limitations, and one next improvement without claiming mastery or job readiness."]'::jsonb),
('ph-sql-databases', NULL, 3, 'phase', NULL, NULL, NULL, 'SQL and Relational Data', 'Learn to query, join, aggregate, validate, and debug relational data for analysis.', 'trunk', 42, 'beginner', '{"generatedBy": "roadmap-platform", "visualRole": "trunk"}'::jsonb, true, false, '["Explain the purpose and practical use of SQL and Relational Data.", "Apply SQL and Relational Data to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates SQL and Relational Data in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('grp-sql-query-foundations', 'ph-sql-databases', 1, 'choice_group', NULL, 'complete_all', 4, 'SQL Query Foundations', 'Group the relational model and core SQL query patterns used in analyst work.', 'side', 9, 'beginner', '{"generatedBy": "roadmap-platform", "groupingPurpose": "phase_topic_group", "selectionType": "complete_all"}'::jsonb, true, false, '["Explain how the topics in SQL Query Foundations work together in a data analyst workflow.", "Apply the grouped sql query foundations skills to a practical analysis task."]'::jsonb, '["Complete all required child nodes in SQL Query Foundations.", "Produce notes, examples, or project evidence that connects the grouped skills."]'::jsonb),
('grp-sql-analytical-techniques', 'ph-sql-databases', 2, 'choice_group', NULL, 'complete_all', 4, 'Analytical SQL Techniques', 'Group advanced analyst SQL patterns, data quality checks, and practical query debugging.', 'side', 9, 'beginner', '{"generatedBy": "roadmap-platform", "groupingPurpose": "phase_topic_group", "selectionType": "complete_all"}'::jsonb, true, false, '["Explain how the topics in Analytical SQL Techniques work together in a data analyst workflow.", "Apply the grouped analytical sql techniques skills to a practical analysis task."]'::jsonb, '["Complete all required child nodes in Analytical SQL Techniques.", "Produce notes, examples, or project evidence that connects the grouped skills."]'::jsonb),
('relational-data-models', 'grp-sql-query-foundations', 1, 'topic', NULL, NULL, NULL, 'Relational Data Models', 'Understand tables, rows, columns, keys, relationships, grain, and how business events become relational data.', 'choice', 4, 'beginner', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Relational Data Models.", "Apply Relational Data Models to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Relational Data Models in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('select-filter-sort', 'grp-sql-query-foundations', 2, 'topic', NULL, NULL, NULL, 'SELECT, Filtering, and Sorting', 'Write queries that select columns, filter rows, sort results, and limit outputs for focused analysis.', 'choice', 4, 'beginner', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of SELECT, Filtering, and Sorting.", "Apply SELECT, Filtering, and Sorting to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates SELECT, Filtering, and Sorting in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('joins-and-relationships', 'grp-sql-query-foundations', 3, 'topic', NULL, NULL, NULL, 'Joins and Relationships', 'Use inner, left, right, and full joins while checking row counts, duplicate inflation, and unmatched records.', 'choice', 6, 'beginner', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Joins and Relationships.", "Apply Joins and Relationships to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Joins and Relationships in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('aggregation-and-grouping', 'grp-sql-query-foundations', 4, 'topic', NULL, NULL, NULL, 'Aggregation and Grouping', 'Use GROUP BY, aggregate functions, HAVING, and conditional aggregation to create business summaries.', 'choice', 5, 'beginner', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Aggregation and Grouping.", "Apply Aggregation and Grouping to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Aggregation and Grouping in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('ctes-and-subqueries', 'grp-sql-analytical-techniques', 1, 'topic', NULL, NULL, NULL, 'CTEs and Subqueries', 'Break complex analytical queries into readable steps using CTEs and subqueries.', 'choice', 5, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of CTEs and Subqueries.", "Apply CTEs and Subqueries to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates CTEs and Subqueries in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('window-functions', 'grp-sql-analytical-techniques', 2, 'topic', NULL, NULL, NULL, 'Window Functions', 'Use ranking, running totals, moving averages, lag/lead, and partitioned calculations for analytical queries.', 'choice', 6, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Window Functions.", "Apply Window Functions to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Window Functions in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('sql-data-quality-checks', 'grp-sql-analytical-techniques', 3, 'topic', NULL, NULL, NULL, 'SQL Data Quality Checks', 'Write SQL checks for duplicates, nulls, invalid ranges, referential mismatches, and unexpected row counts.', 'choice', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of SQL Data Quality Checks.", "Apply SQL Data Quality Checks to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates SQL Data Quality Checks in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('query-debugging-performance', 'grp-sql-analytical-techniques', 4, 'topic', NULL, NULL, NULL, 'Query Debugging and Performance Basics', 'Debug query logic, inspect intermediate row counts, read simple execution plans, and avoid expensive analytical query mistakes.', 'choice', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Query Debugging and Performance Basics.", "Apply Query Debugging and Performance Basics to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Query Debugging and Performance Basics in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('chk-sql-review', 'ph-sql-databases', 90, 'checkpoint', 'assessment', NULL, NULL, 'SQL Analysis Review', 'Practical checkpoint for SQL and Relational Data. The learner completes a small diagnostic task or review artifact using Relational Data Models, SELECT, Filtering, and Sorting, Joins and Relationships, Aggregation and Grouping and records observations that can guide further practice.', 'checkpoint', 3, 'intermediate', '{"generatedBy": "roadmap-platform", "checkpointType": "assessment", "checkpointPurpose": "phase_review", "reviewMode": "assessment", "evidenceRequired": true, "claimLimit": "orientation_and_progress_review_only_not_certification"}'::jsonb, true, true, '["Apply selected concepts from SQL and Relational Data to a small practical scenario.", "Explain what worked, what failed, and which signals were used to debug or validate the result.", "Translate the result into a short next-action plan for continued learning."]'::jsonb, '["Complete a small practical task, lab, review exercise, or worked example related to this segment.", "Attach or describe the output, result, or evidence used for review.", "Explain at least one mistake, trade-off, or failure mode encountered during the task.", "Write the next practice action based on the result."]'::jsonb),
('proj-sql-business-analysis', 'ph-sql-databases', 100, 'project', NULL, NULL, NULL, 'Required SQL Business Analysis Project', 'Milestone project: Use SQL to answer a business question with joins, aggregations, window functions, data quality checks, and a concise analysis memo.', 'side', 9, 'intermediate', '{"project":true,"projectBrief":"Use SQL to answer a business question with joins, aggregations, window functions, data quality checks, and a concise analysis memo.","projectKind":"analytics_artifact","projectScope":"focused_milestone","projectPurpose":"Demonstrate the surrounding roadmap segment through a focused milestone artifact.","phaseContext":"SQL and Relational Data","deliverable":"an SQL Business Analysis Project workbook, report, or dashboard with documented assumptions","skillsToPractice":["SQL","Data Quality","Business Metrics"],"suggestedSteps":["Define the SQL Business Analysis Project scenario and list the specific items to demonstrate: SQL queries.","Define the business question, audience, grain, metric formulas, and decision the artifact should support.","Prepare or clean the dataset and document assumptions, exclusions, and data-quality checks.","Build the report, dashboard, notebook, or workbook with the minimum views needed to answer the question.","Validate the numbers against source rows, spot checks, or reconciliation notes.","Write a short interpretation with caveats and recommended next actions.","Keep the scope focused enough to complete, review, and explain the an SQL Business Analysis Project workbook, report, or dashboard with documented assumptions."],"expectedEvidence":["an SQL Business Analysis Project workbook, report, or dashboard with documented assumptions.","README or notes that explain setup, usage, assumptions, and expected output.","Metric definitions, charts/tables, source checks, and a concise interpretation for the intended audience.","Validation evidence such as tests, screenshots, logs, query results, metrics, traces, or review notes.","Short reflection on trade-offs, limitations, and one reasonable next improvement."],"reviewFocus":["Does the artifact directly match the project brief?","Can another person run, inspect, or review the result from the provided notes?","Are assumptions, limitations, and trade-offs stated clearly?"],"scopeGuardrails":["Keep the implementation focused on the stated project brief.","Prefer a complete small artifact over a broad unfinished system.","Treat completion as reviewable evidence, not a certification claim."],"claimLimit":"Completion creates reviewable project evidence. It does not certify mastery or guarantee job readiness.","evidenceRequired":true,"optionalPractice":false,"milestone":true,"nodeKey":"proj-sql-business-analysis"}'::jsonb, true, true, '["Apply SQL, Data Quality, Business Metrics through a focused SQL Business Analysis Project artifact.","Explain the main design, data, tooling, workflow, or review decisions behind the project.","Validate the result with evidence that another person can inspect.","Document assumptions, limitations, and a concrete follow-up improvement."]'::jsonb, '["The an SQL Business Analysis Project workbook, report, or dashboard with documented assumptions is available for review.","The SQL Business Analysis Project scope, setup path, input data or scenario, and expected output are documented.","The main workflow runs or can be inspected through a notebook, repository, dashboard, prototype, report, configuration, or case study.","Validation evidence is included and directly relates to the project brief.","The notes identify trade-offs, limitations, and one next improvement without claiming mastery or job readiness."]'::jsonb),
('ph-statistics', NULL, 4, 'phase', NULL, NULL, NULL, 'Statistics and Analytical Reasoning', 'Develop the statistical judgment needed to summarize data, compare groups, reason under uncertainty, and avoid misleading conclusions.', 'trunk', 38, 'beginner', '{"generatedBy": "roadmap-platform", "visualRole": "trunk"}'::jsonb, true, false, '["Explain the purpose and practical use of Statistics and Analytical Reasoning.", "Apply Statistics and Analytical Reasoning to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Statistics and Analytical Reasoning in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('grp-statistics-foundations', 'ph-statistics', 1, 'choice_group', NULL, 'complete_all', 4, 'Descriptive and Inferential Statistics', 'Group the statistical tools needed to summarize data and reason about uncertainty.', 'side', 9, 'beginner', '{"generatedBy": "roadmap-platform", "groupingPurpose": "phase_topic_group", "selectionType": "complete_all"}'::jsonb, true, false, '["Explain how the topics in Descriptive and Inferential Statistics work together in a data analyst workflow.", "Apply the grouped descriptive and inferential statistics skills to a practical analysis task."]'::jsonb, '["Complete all required child nodes in Descriptive and Inferential Statistics.", "Produce notes, examples, or project evidence that connects the grouped skills."]'::jsonb),
('grp-experiment-relationship-analysis', 'ph-statistics', 2, 'choice_group', NULL, 'complete_all', 3, 'Experiment and Relationship Analysis', 'Group practical methods for evaluating relationships, experiments, and directional evidence.', 'side', 7, 'beginner', '{"generatedBy": "roadmap-platform", "groupingPurpose": "phase_topic_group", "selectionType": "complete_all"}'::jsonb, true, false, '["Explain how the topics in Experiment and Relationship Analysis work together in a data analyst workflow.", "Apply the grouped experiment and relationship analysis skills to a practical analysis task."]'::jsonb, '["Complete all required child nodes in Experiment and Relationship Analysis.", "Produce notes, examples, or project evidence that connects the grouped skills."]'::jsonb),
('descriptive-statistics-analysis', 'grp-statistics-foundations', 1, 'topic', NULL, NULL, NULL, 'Descriptive Statistics', 'Use mean, median, variance, quantiles, distributions, and grouped summaries to describe datasets accurately.', 'choice', 4, 'beginner', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Descriptive Statistics.", "Apply Descriptive Statistics to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Descriptive Statistics in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('distributions-and-sampling', 'grp-statistics-foundations', 2, 'topic', NULL, NULL, NULL, 'Distributions and Sampling', 'Understand distributions, sampling bias, sample size, and how data collection affects conclusions.', 'choice', 5, 'beginner', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Distributions and Sampling.", "Apply Distributions and Sampling to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Distributions and Sampling in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('confidence-intervals', 'grp-statistics-foundations', 3, 'topic', NULL, NULL, NULL, 'Confidence Intervals', 'Estimate uncertainty and communicate ranges rather than overconfident point estimates.', 'choice', 4, 'beginner', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Confidence Intervals.", "Apply Confidence Intervals to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Confidence Intervals in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('hypothesis-testing-basics', 'grp-statistics-foundations', 4, 'topic', NULL, NULL, NULL, 'Hypothesis Testing Basics', 'Use hypothesis tests appropriately and explain p-values, practical significance, and common misuse.', 'choice', 5, 'beginner', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Hypothesis Testing Basics.", "Apply Hypothesis Testing Basics to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Hypothesis Testing Basics in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('correlation-causation', 'grp-experiment-relationship-analysis', 1, 'topic', NULL, NULL, NULL, 'Correlation vs Causation', 'Distinguish relationships from causal claims, identify confounders, and communicate limits of observational analysis.', 'choice', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Correlation vs Causation.", "Apply Correlation vs Causation to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Correlation vs Causation in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('ab-test-interpretation', 'grp-experiment-relationship-analysis', 2, 'topic', NULL, NULL, NULL, 'A/B Test Interpretation', 'Interpret controlled experiment results using metrics, guardrails, confidence, and practical business impact.', 'choice', 5, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of A/B Test Interpretation.", "Apply A/B Test Interpretation to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates A/B Test Interpretation in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('regression-awareness', 'grp-experiment-relationship-analysis', 3, 'topic', NULL, NULL, NULL, 'Regression Analysis Awareness', 'Understand simple regression outputs, assumptions, residuals, and where regression helps analysts explain relationships.', 'choice', 5, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Regression Analysis Awareness.", "Apply Regression Analysis Awareness to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Regression Analysis Awareness in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('chk-statistics-review', 'ph-statistics', 90, 'checkpoint', 'assessment', NULL, NULL, 'Statistics Review Gate', 'Practical checkpoint for Statistics and Analytical Reasoning. The learner completes a small diagnostic task or review artifact using Descriptive Statistics, Distributions and Sampling, Confidence Intervals, Hypothesis Testing Basics and records observations that can guide further practice.', 'checkpoint', 3, 'intermediate', '{"generatedBy": "roadmap-platform", "checkpointType": "assessment", "checkpointPurpose": "phase_review", "reviewMode": "assessment", "evidenceRequired": true, "claimLimit": "orientation_and_progress_review_only_not_certification"}'::jsonb, true, true, '["Apply selected concepts from Statistics and Analytical Reasoning to a small practical scenario.", "Explain what worked, what failed, and which signals were used to debug or validate the result.", "Translate the result into a short next-action plan for continued learning."]'::jsonb, '["Complete a small practical task, lab, review exercise, or worked example related to this segment.", "Attach or describe the output, result, or evidence used for review.", "Explain at least one mistake, trade-off, or failure mode encountered during the task.", "Write the next practice action based on the result."]'::jsonb),
('proj-statistical-insight-report', 'ph-statistics', 100, 'project', NULL, NULL, NULL, 'Required Statistical Insight Report', 'Milestone project: Analyze a dataset using descriptive statistics, uncertainty, group comparison, and a clear explanation of what can and cannot be concluded.', 'side', 7, 'intermediate', '{"project":true,"projectBrief":"Analyze a dataset using descriptive statistics, uncertainty, group comparison, and a clear explanation of what can and cannot be concluded.","projectKind":"analytics_artifact","projectScope":"focused_milestone","projectPurpose":"Demonstrate the surrounding roadmap segment through a focused milestone artifact.","phaseContext":"Statistics and Analytical Reasoning","deliverable":"a Statistical Insight Report workbook, report, or dashboard with documented assumptions","skillsToPractice":["Statistics","Data Storytelling"],"suggestedSteps":["Define the Statistical Insight Report scenario, target user or reviewer, and the specific Statistics and Analytical Reasoning concepts it will demonstrate.","Define the business question, audience, grain, metric formulas, and decision the artifact should support.","Prepare or clean the dataset and document assumptions, exclusions, and data-quality checks.","Build the report, dashboard, notebook, or workbook with the minimum views needed to answer the question.","Validate the numbers against source rows, spot checks, or reconciliation notes.","Write a short interpretation with caveats and recommended next actions.","Keep the scope focused enough to complete, review, and explain the a Statistical Insight Report workbook, report, or dashboard with documented assumptions."],"expectedEvidence":["a Statistical Insight Report workbook, report, or dashboard with documented assumptions.","README or notes that explain setup, usage, assumptions, and expected output.","Metric definitions, charts/tables, source checks, and a concise interpretation for the intended audience.","Validation evidence such as tests, screenshots, logs, query results, metrics, traces, or review notes.","Short reflection on trade-offs, limitations, and one reasonable next improvement."],"reviewFocus":["Does the artifact directly match the project brief?","Can another person run, inspect, or review the result from the provided notes?","Are assumptions, limitations, and trade-offs stated clearly?"],"scopeGuardrails":["Keep the implementation focused on the stated project brief.","Prefer a complete small artifact over a broad unfinished system.","Treat completion as reviewable evidence, not a certification claim."],"claimLimit":"Completion creates reviewable project evidence. It does not certify mastery or guarantee job readiness.","evidenceRequired":true,"optionalPractice":false,"milestone":true,"nodeKey":"proj-statistical-insight-report"}'::jsonb, true, true, '["Apply Statistics, Data Storytelling through a focused Statistical Insight Report artifact.","Explain the main design, data, tooling, workflow, or review decisions behind the project.","Validate the result with evidence that another person can inspect.","Document assumptions, limitations, and a concrete follow-up improvement."]'::jsonb, '["The a Statistical Insight Report workbook, report, or dashboard with documented assumptions is available for review.","The Statistical Insight Report scope, setup path, input data or scenario, and expected output are documented.","The main workflow runs or can be inspected through a notebook, repository, dashboard, prototype, report, configuration, or case study.","Validation evidence is included and directly relates to the project brief.","The notes identify trade-offs, limitations, and one next improvement without claiming mastery or job readiness."]'::jsonb),
('ph-data-cleaning-preparation', NULL, 5, 'phase', NULL, NULL, NULL, 'Data Cleaning and Preparation', 'Learn to profile, clean, validate, document, and prepare datasets without losing analytical context.', 'trunk', 38, 'intermediate', '{"generatedBy": "roadmap-platform", "visualRole": "trunk"}'::jsonb, true, false, '["Explain the purpose and practical use of Data Cleaning and Preparation.", "Apply Data Cleaning and Preparation to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Data Cleaning and Preparation in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('grp-data-quality-issues', 'ph-data-cleaning-preparation', 1, 'choice_group', NULL, 'complete_all', 4, 'Data Profiling and Quality Issues', 'Group the inspection skills used to identify messy, incomplete, duplicate, and suspicious data.', 'side', 8, 'intermediate', '{"generatedBy": "roadmap-platform", "groupingPurpose": "phase_topic_group", "selectionType": "complete_all"}'::jsonb, true, false, '["Explain how the topics in Data Profiling and Quality Issues work together in a data analyst workflow.", "Apply the grouped data profiling and quality issues skills to a practical analysis task."]'::jsonb, '["Complete all required child nodes in Data Profiling and Quality Issues.", "Produce notes, examples, or project evidence that connects the grouped skills."]'::jsonb),
('grp-reproducible-preparation', 'ph-data-cleaning-preparation', 2, 'choice_group', NULL, 'complete_all', 3, 'Validation and Reproducible Preparation', 'Group repeatable preparation, validation, and standardization practices.', 'side', 6, 'intermediate', '{"generatedBy": "roadmap-platform", "groupingPurpose": "phase_topic_group", "selectionType": "complete_all"}'::jsonb, true, false, '["Explain how the topics in Validation and Reproducible Preparation work together in a data analyst workflow.", "Apply the grouped validation and reproducible preparation skills to a practical analysis task."]'::jsonb, '["Complete all required child nodes in Validation and Reproducible Preparation.", "Produce notes, examples, or project evidence that connects the grouped skills."]'::jsonb),
('data-profiling', 'grp-data-quality-issues', 1, 'topic', NULL, NULL, NULL, 'Data Profiling', 'Inspect schema, row counts, distributions, uniqueness, missingness, ranges, and suspicious values before analysis.', 'choice', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Data Profiling.", "Apply Data Profiling to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Data Profiling in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('missing-data-handling', 'grp-data-quality-issues', 2, 'topic', NULL, NULL, NULL, 'Missing Data Handling', 'Identify why values are missing and choose appropriate handling strategies without hiding bias or uncertainty.', 'choice', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Missing Data Handling.", "Apply Missing Data Handling to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Missing Data Handling in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('duplicates-and-entity-resolution', 'grp-data-quality-issues', 3, 'topic', NULL, NULL, NULL, 'Duplicates and Entity Resolution', 'Detect duplicate records, conflicting identifiers, and entity matching issues that can distort metrics.', 'choice', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Duplicates and Entity Resolution.", "Apply Duplicates and Entity Resolution to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Duplicates and Entity Resolution in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('outliers-and-anomalies', 'grp-data-quality-issues', 4, 'topic', NULL, NULL, NULL, 'Outliers and Anomalies', 'Identify unusual values, decide whether they are errors or valid extremes, and document treatment decisions.', 'choice', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Outliers and Anomalies.", "Apply Outliers and Anomalies to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Outliers and Anomalies in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('data-type-and-format-standardization', 'grp-reproducible-preparation', 1, 'topic', NULL, NULL, NULL, 'Data Types and Format Standardization', 'Standardize dates, categories, currencies, text fields, and units so analysis calculations are reliable.', 'choice', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Data Types and Format Standardization.", "Apply Data Types and Format Standardization to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Data Types and Format Standardization in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('data-validation-rules', 'grp-reproducible-preparation', 2, 'topic', NULL, NULL, NULL, 'Data Validation Rules', 'Create checks for accepted values, nullability, uniqueness, referential logic, and business constraints.', 'choice', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Data Validation Rules.", "Apply Data Validation Rules to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Data Validation Rules in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('reproducible-cleaning-workflows', 'grp-reproducible-preparation', 3, 'topic', NULL, NULL, NULL, 'Reproducible Cleaning Workflows', 'Avoid one-off manual fixes by documenting transformations in SQL, Power Query, or Python scripts.', 'choice', 5, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Reproducible Cleaning Workflows.", "Apply Reproducible Cleaning Workflows to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Reproducible Cleaning Workflows in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('chk-data-cleaning-review', 'ph-data-cleaning-preparation', 90, 'checkpoint', 'review', NULL, NULL, 'Data Cleaning Review Gate', 'Review checkpoint for Data Cleaning and Preparation. The learner synthesizes completed topics such as Data Profiling, Missing Data Handling, Duplicates and Entity Resolution, Outliers and Anomalies, records evidence, and updates the learning plan before continuing.', 'checkpoint', 3, 'intermediate', '{"generatedBy": "roadmap-platform", "checkpointType": "review", "checkpointPurpose": "phase_review", "reviewMode": "review", "evidenceRequired": true, "claimLimit": "orientation_and_progress_review_only_not_certification"}'::jsonb, true, true, '["Explain how topics from Data Cleaning and Preparation connect to realistic data analysis work.", "Select evidence that shows attempted practice, reasoning, or implementation decisions.", "Identify which topics need review before moving to later roadmap areas."]'::jsonb, '["Write a concise review of the main ideas from this segment.", "Include one evidence artifact such as notes, a diagram, lab output, code, query results, screenshots, or a project review.", "Record open questions, weak areas, or follow-up actions.", "Mark the checkpoint complete only after the review artifact and next steps are captured."]'::jsonb),
('proj-cleaning-pipeline-report', 'ph-data-cleaning-preparation', 100, 'project', NULL, NULL, NULL, 'Required Data Cleaning Pipeline Report', 'Milestone project: Clean a messy dataset using SQL, Power Query, or Python; document quality issues, transformation logic, validation checks, and final analysis-ready output.', 'side', 8, 'intermediate', '{"project":true,"projectBrief":"Clean a messy dataset using SQL, Power Query, or Python; document quality issues, transformation logic, validation checks, and final analysis-ready output.","projectKind":"analytics_artifact","projectScope":"focused_milestone","projectPurpose":"Demonstrate the surrounding roadmap segment through a focused milestone artifact.","phaseContext":"Data Cleaning and Preparation","deliverable":"a Data Cleaning Pipeline Report workbook, report, or dashboard with documented assumptions","skillsToPractice":["Data Cleaning","Data Validation","Data Quality"],"suggestedSteps":["Define the Data Cleaning Pipeline Report scenario and list the specific items to demonstrate: SQL queries, pipeline stages.","Define the business question, audience, grain, metric formulas, and decision the artifact should support.","Prepare or clean the dataset and document assumptions, exclusions, and data-quality checks.","Build the report, dashboard, notebook, or workbook with the minimum views needed to answer the question.","Validate the numbers against source rows, spot checks, or reconciliation notes.","Write a short interpretation with caveats and recommended next actions.","Keep the scope focused enough to complete, review, and explain the a Data Cleaning Pipeline Report workbook, report, or dashboard with documented assumptions."],"expectedEvidence":["a Data Cleaning Pipeline Report workbook, report, or dashboard with documented assumptions.","README or notes that explain setup, usage, assumptions, and expected output.","Metric definitions, charts/tables, source checks, and a concise interpretation for the intended audience.","Validation evidence such as tests, screenshots, logs, query results, metrics, traces, or review notes.","Short reflection on trade-offs, limitations, and one reasonable next improvement."],"reviewFocus":["Does the artifact directly match the project brief?","Can another person run, inspect, or review the result from the provided notes?","Are assumptions, limitations, and trade-offs stated clearly?"],"scopeGuardrails":["Keep the implementation focused on the stated project brief.","Prefer a complete small artifact over a broad unfinished system.","Treat completion as reviewable evidence, not a certification claim."],"claimLimit":"Completion creates reviewable project evidence. It does not certify mastery or guarantee job readiness.","evidenceRequired":true,"optionalPractice":false,"milestone":true,"nodeKey":"proj-cleaning-pipeline-report"}'::jsonb, true, true, '["Apply Data Cleaning, Data Validation, Data Quality through a focused Data Cleaning Pipeline Report artifact.","Explain the main design, data, tooling, workflow, or review decisions behind the project.","Validate the result with evidence that another person can inspect.","Document assumptions, limitations, and a concrete follow-up improvement."]'::jsonb, '["The a Data Cleaning Pipeline Report workbook, report, or dashboard with documented assumptions is available for review.","The Data Cleaning Pipeline Report scope, setup path, input data or scenario, and expected output are documented.","The main workflow runs or can be inspected through a notebook, repository, dashboard, prototype, report, configuration, or case study.","Validation evidence is included and directly relates to the project brief.","The notes identify trade-offs, limitations, and one next improvement without claiming mastery or job readiness."]'::jsonb),
('ph-visualization-storytelling', NULL, 6, 'phase', NULL, NULL, NULL, 'Data Visualization and Storytelling', 'Turn analysis into clear charts, narratives, dashboards, and recommendations that help stakeholders act.', 'trunk', 38, 'intermediate', '{"generatedBy": "roadmap-platform", "visualRole": "trunk"}'::jsonb, true, false, '["Explain the purpose and practical use of Data Visualization and Storytelling.", "Apply Data Visualization and Storytelling to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Data Visualization and Storytelling in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('grp-visual-design-foundations', 'ph-visualization-storytelling', 1, 'choice_group', NULL, 'complete_all', 4, 'Visualization Design Foundations', 'Group visual design decisions that make charts accurate, accessible, and hard to misread.', 'side', 8, 'intermediate', '{"generatedBy": "roadmap-platform", "groupingPurpose": "phase_topic_group", "selectionType": "complete_all"}'::jsonb, true, false, '["Explain how the topics in Visualization Design Foundations work together in a data analyst workflow.", "Apply the grouped visualization design foundations skills to a practical analysis task."]'::jsonb, '["Complete all required child nodes in Visualization Design Foundations.", "Produce notes, examples, or project evidence that connects the grouped skills."]'::jsonb),
('grp-dashboard-storytelling-delivery', 'ph-visualization-storytelling', 2, 'choice_group', NULL, 'complete_all', 3, 'Dashboard and Storytelling Delivery', 'Group dashboard purpose, narrative structure, and stakeholder delivery skills.', 'side', 7, 'intermediate', '{"generatedBy": "roadmap-platform", "groupingPurpose": "phase_topic_group", "selectionType": "complete_all"}'::jsonb, true, false, '["Explain how the topics in Dashboard and Storytelling Delivery work together in a data analyst workflow.", "Apply the grouped dashboard and storytelling delivery skills to a practical analysis task."]'::jsonb, '["Complete all required child nodes in Dashboard and Storytelling Delivery.", "Produce notes, examples, or project evidence that connects the grouped skills."]'::jsonb),
('chart-selection', 'grp-visual-design-foundations', 1, 'topic', NULL, NULL, NULL, 'Chart Selection', 'Choose charts that match comparison, trend, distribution, relationship, composition, and ranking questions.', 'choice', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Chart Selection.", "Apply Chart Selection to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Chart Selection in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('visual-design-principles', 'grp-visual-design-foundations', 2, 'topic', NULL, NULL, NULL, 'Visual Design Principles', 'Use visual hierarchy, labels, color, whitespace, and chart simplicity to reduce cognitive load.', 'choice', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Visual Design Principles.", "Apply Visual Design Principles to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Visual Design Principles in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('accessibility-in-data-visualization', 'grp-visual-design-foundations', 3, 'topic', NULL, NULL, NULL, 'Accessibility in Data Visualization', 'Design charts and dashboards with readable labels, contrast, color-safe encodings, and inclusive presentation choices.', 'choice', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Accessibility in Data Visualization.", "Apply Accessibility in Data Visualization to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Accessibility in Data Visualization in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('dashboard-purpose-and-layout', 'grp-dashboard-storytelling-delivery', 1, 'topic', NULL, NULL, NULL, 'Dashboard Purpose and Layout', 'Design dashboard pages around audience, action, scan path, filters, and decision context.', 'choice', 5, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Dashboard Purpose and Layout.", "Apply Dashboard Purpose and Layout to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Dashboard Purpose and Layout in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('insight-narrative', 'grp-dashboard-storytelling-delivery', 2, 'topic', NULL, NULL, NULL, 'Insight Narrative', 'Write findings that connect evidence, context, recommendation, uncertainty, and next action.', 'choice', 5, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Insight Narrative.", "Apply Insight Narrative to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Insight Narrative in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('presentation-delivery', 'grp-dashboard-storytelling-delivery', 3, 'topic', NULL, NULL, NULL, 'Presentation Delivery', 'Present analysis in a way that answers likely stakeholder questions and separates facts, assumptions, and recommendations.', 'choice', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Presentation Delivery.", "Apply Presentation Delivery to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Presentation Delivery in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('misleading-visuals', 'grp-visual-design-foundations', 4, 'topic', NULL, NULL, NULL, 'Avoiding Misleading Visuals', 'Identify misleading axes, cherry-picked comparisons, overplotting, bad aggregation, and unsupported causal claims.', 'choice', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Avoiding Misleading Visuals.", "Apply Avoiding Misleading Visuals to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Avoiding Misleading Visuals in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('chk-visualization-review', 'ph-visualization-storytelling', 90, 'checkpoint', 'review', NULL, NULL, 'Visualization Review Gate', 'Review checkpoint for Data Visualization and Storytelling. The learner synthesizes completed topics such as Chart Selection, Visual Design Principles, Accessibility in Data Visualization, Avoiding Misleading Visuals, records evidence, and updates the learning plan before continuing.', 'checkpoint', 2, 'intermediate', '{"generatedBy": "roadmap-platform", "checkpointType": "review", "checkpointPurpose": "phase_review", "reviewMode": "review", "evidenceRequired": true, "claimLimit": "orientation_and_progress_review_only_not_certification"}'::jsonb, true, true, '["Explain how topics from Data Visualization and Storytelling connect to realistic data analysis work.", "Select evidence that shows attempted practice, reasoning, or implementation decisions.", "Identify which topics need review before moving to later roadmap areas."]'::jsonb, '["Write a concise review of the main ideas from this segment.", "Include one evidence artifact such as notes, a diagram, lab output, code, query results, screenshots, or a project review.", "Record open questions, weak areas, or follow-up actions.", "Mark the checkpoint complete only after the review artifact and next steps are captured."]'::jsonb),
('proj-executive-insight-deck', 'ph-visualization-storytelling', 100, 'project', NULL, NULL, NULL, 'Required Executive Insight Deck', 'Milestone project: Create a short insight deck with chart choices, narrative, recommendations, limitations, and a stakeholder-ready summary.', 'side', 7, 'intermediate', '{"project":true,"projectBrief":"Create a short insight deck with chart choices, narrative, recommendations, limitations, and a stakeholder-ready summary.","projectKind":"analytics_artifact","projectScope":"focused_milestone","projectPurpose":"Demonstrate the surrounding roadmap segment through a focused milestone artifact.","phaseContext":"Data Visualization and Storytelling","deliverable":"a Executive Insight Deck workbook, report, or dashboard with documented assumptions","skillsToPractice":["Data Storytelling","Data Visualization","Stakeholder Communication"],"suggestedSteps":["Define the Executive Insight Deck scenario, target user or reviewer, and the specific Data Visualization and Storytelling concepts it will demonstrate.","Define the business question, audience, grain, metric formulas, and decision the artifact should support.","Prepare or clean the dataset and document assumptions, exclusions, and data-quality checks.","Build the report, dashboard, notebook, or workbook with the minimum views needed to answer the question.","Validate the numbers against source rows, spot checks, or reconciliation notes.","Write a short interpretation with caveats and recommended next actions.","Keep the scope focused enough to complete, review, and explain the a Executive Insight Deck workbook, report, or dashboard with documented assumptions."],"expectedEvidence":["a Executive Insight Deck workbook, report, or dashboard with documented assumptions.","README or notes that explain setup, usage, assumptions, and expected output.","Metric definitions, charts/tables, source checks, and a concise interpretation for the intended audience.","Validation evidence such as tests, screenshots, logs, query results, metrics, traces, or review notes.","Short reflection on trade-offs, limitations, and one reasonable next improvement."],"reviewFocus":["Does the artifact directly match the project brief?","Can another person run, inspect, or review the result from the provided notes?","Are assumptions, limitations, and trade-offs stated clearly?"],"scopeGuardrails":["Keep the implementation focused on the stated project brief.","Prefer a complete small artifact over a broad unfinished system.","Treat completion as reviewable evidence, not a certification claim."],"claimLimit":"Completion creates reviewable project evidence. It does not certify mastery or guarantee job readiness.","evidenceRequired":true,"optionalPractice":false,"milestone":true,"nodeKey":"proj-executive-insight-deck"}'::jsonb, true, true, '["Apply Data Storytelling, Data Visualization, Stakeholder Communication through a focused Executive Insight Deck artifact.","Explain the main design, data, tooling, workflow, or review decisions behind the project.","Validate the result with evidence that another person can inspect.","Document assumptions, limitations, and a concrete follow-up improvement."]'::jsonb, '["The a Executive Insight Deck workbook, report, or dashboard with documented assumptions is available for review.","The Executive Insight Deck scope, setup path, input data or scenario, and expected output are documented.","The main workflow runs or can be inspected through a notebook, repository, dashboard, prototype, report, configuration, or case study.","Validation evidence is included and directly relates to the project brief.","The notes identify trade-offs, limitations, and one next improvement without claiming mastery or job readiness."]'::jsonb),
('ph-bi-tools', NULL, 7, 'phase', NULL, NULL, NULL, 'BI Tools and Dashboard Development', 'Choose a primary BI tool and build governed, interactive dashboards that answer business questions.', 'trunk', 42, 'intermediate', '{"generatedBy": "roadmap-platform", "visualRole": "trunk"}'::jsonb, true, false, '["Explain the purpose and practical use of BI Tools and Dashboard Development.", "Apply BI Tools and Dashboard Development to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates BI Tools and Dashboard Development in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('grp-dashboard-delivery-workflow', 'ph-bi-tools', 1, 'choice_group', NULL, 'complete_all', 4, 'Dashboard Delivery Workflow', 'Group BI dashboard modeling, interactivity, performance, and documentation skills after the learner selects a primary BI tool.', 'side', 8, 'intermediate', '{"generatedBy": "roadmap-platform", "groupingPurpose": "phase_topic_group", "selectionType": "complete_all"}'::jsonb, true, false, '["Explain how the topics in Dashboard Delivery Workflow work together in a data analyst workflow.", "Apply the grouped dashboard delivery workflow skills to a practical analysis task."]'::jsonb, '["Complete all required child nodes in Dashboard Delivery Workflow.", "Produce notes, examples, or project evidence that connects the grouped skills."]'::jsonb),
('grp-choose-bi-tool', 'ph-bi-tools', 1, 'choice_group', NULL, 'choose_one', 1, 'Choose a Primary BI Tool', 'Select one BI tool path for deeper practice while keeping awareness of the others.', 'side', 8, 'intermediate', '{"generatedBy": "roadmap-platform", "choiceReason": "Most learners specialize in one BI tool first."}'::jsonb, true, false, '["Explain the purpose and practical use of Choose a Primary BI Tool.", "Apply Choose a Primary BI Tool to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Choose a Primary BI Tool in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('power-bi-path', 'grp-choose-bi-tool', 1, 'choice_option', NULL, NULL, NULL, 'Power BI Path', 'Learn Power BI reports, semantic models, Power Query, DAX basics, publishing, sharing, and dashboard iteration.', 'choice', 8, 'intermediate', '{"generatedBy": "roadmap-platform", "choiceOption": true}'::jsonb, true, true, '["Explain the purpose and practical use of Power BI Path.", "Apply Power BI Path to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Power BI Path in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('tableau-path', 'grp-choose-bi-tool', 2, 'choice_option', NULL, NULL, NULL, 'Tableau Path', 'Learn Tableau data connections, worksheet authoring, calculated fields, dashboards, filters, and publishing workflows.', 'choice', 8, 'intermediate', '{"generatedBy": "roadmap-platform", "choiceOption": true}'::jsonb, true, true, '["Explain the purpose and practical use of Tableau Path.", "Apply Tableau Path to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Tableau Path in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('looker-studio-path', 'grp-choose-bi-tool', 3, 'choice_option', NULL, NULL, NULL, 'Looker Studio Path', 'Learn Looker Studio data sources, calculated fields, interactive reports, sharing, and lightweight dashboard publishing.', 'choice', 6, 'intermediate', '{"generatedBy": "roadmap-platform", "choiceOption": true}'::jsonb, true, true, '["Explain the purpose and practical use of Looker Studio Path.", "Apply Looker Studio Path to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Looker Studio Path in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('looker-awareness-path', 'grp-choose-bi-tool', 4, 'choice_option', NULL, NULL, NULL, 'Looker Awareness Path', 'Understand governed BI concepts using Looker models, explores, dimensions, measures, and shared definitions.', 'choice', 6, 'intermediate', '{"generatedBy": "roadmap-platform", "choiceOption": true}'::jsonb, true, true, '["Explain the purpose and practical use of Looker Awareness Path.", "Apply Looker Awareness Path to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Looker Awareness Path in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('dashboard-data-modeling', 'grp-dashboard-delivery-workflow', 1, 'topic', NULL, NULL, NULL, 'Dashboard Data Modeling', 'Shape data into useful dimensions, measures, calculated fields, relationships, and grain for dashboard use.', 'choice', 5, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Dashboard Data Modeling.", "Apply Dashboard Data Modeling to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Dashboard Data Modeling in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('dashboard-interactivity', 'grp-dashboard-delivery-workflow', 2, 'topic', NULL, NULL, NULL, 'Dashboard Filters and Interactivity', 'Use filters, parameters, slicers, drilldowns, and tooltips without making dashboards confusing.', 'choice', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Dashboard Filters and Interactivity.", "Apply Dashboard Filters and Interactivity to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Dashboard Filters and Interactivity in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('dashboard-performance-basics', 'grp-dashboard-delivery-workflow', 3, 'topic', NULL, NULL, NULL, 'Dashboard Performance Basics', 'Reduce dashboard load time by simplifying visuals, filtering data, optimizing models, and using the right level of detail.', 'choice', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Dashboard Performance Basics.", "Apply Dashboard Performance Basics to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Dashboard Performance Basics in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('dashboard-documentation', 'grp-dashboard-delivery-workflow', 4, 'topic', NULL, NULL, NULL, 'Dashboard Documentation', 'Document metric definitions, filters, refresh schedule, data caveats, ownership, and intended dashboard use.', 'choice', 3, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Dashboard Documentation.", "Apply Dashboard Documentation to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Dashboard Documentation in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('chk-dashboard-review', 'ph-bi-tools', 90, 'checkpoint', 'review', NULL, NULL, 'Dashboard Development Review Gate', 'Review checkpoint for BI Tools and Dashboard Development. The learner synthesizes completed topics such as Dashboard Data Modeling, Dashboard Filters and Interactivity, Dashboard Performance Basics, Dashboard Documentation, records evidence, and updates the learning plan before continuing.', 'checkpoint', 3, 'intermediate', '{"generatedBy": "roadmap-platform", "checkpointType": "review", "checkpointPurpose": "phase_review", "reviewMode": "review", "evidenceRequired": true, "claimLimit": "orientation_and_progress_review_only_not_certification"}'::jsonb, true, true, '["Explain how topics from BI Tools and Dashboard Development connect to realistic data analysis work.", "Select evidence that shows attempted practice, reasoning, or implementation decisions.", "Identify which topics need review before moving to later roadmap areas."]'::jsonb, '["Write a concise review of the main ideas from this segment.", "Include one evidence artifact such as notes, a diagram, lab output, code, query results, screenshots, or a project review.", "Record open questions, weak areas, or follow-up actions.", "Mark the checkpoint complete only after the review artifact and next steps are captured."]'::jsonb),
('proj-bi-dashboard', 'ph-bi-tools', 100, 'project', NULL, NULL, NULL, 'Required Interactive BI Dashboard', 'Milestone project: Build an interactive dashboard in one selected BI tool with documented metrics, filters, audience, caveats, and a stakeholder-ready walkthrough.', 'side', 10, 'intermediate', '{"project":true,"projectBrief":"Build an interactive dashboard in one selected BI tool with documented metrics, filters, audience, caveats, and a stakeholder-ready walkthrough.","projectKind":"analytics_artifact","projectScope":"focused_milestone","projectPurpose":"Demonstrate the surrounding roadmap segment through a focused milestone artifact.","phaseContext":"BI Tools and Dashboard Development","deliverable":"a Interactive BI Dashboard workbook, report, or dashboard with documented assumptions","skillsToPractice":["Dashboard Design","Data Storytelling","Business Metrics"],"suggestedSteps":["Define the Interactive BI Dashboard scenario and list the specific items to demonstrate: dashboard decisions, metrics.","Define the business question, audience, grain, metric formulas, and decision the artifact should support.","Prepare or clean the dataset and document assumptions, exclusions, and data-quality checks.","Build the report, dashboard, notebook, or workbook with the minimum views needed to answer the question.","Validate the numbers against source rows, spot checks, or reconciliation notes.","Write a short interpretation with caveats and recommended next actions.","Keep the scope focused enough to complete, review, and explain the a Interactive BI Dashboard workbook, report, or dashboard with documented assumptions."],"expectedEvidence":["a Interactive BI Dashboard workbook, report, or dashboard with documented assumptions.","README or notes that explain setup, usage, assumptions, and expected output.","Metric definitions, charts/tables, source checks, and a concise interpretation for the intended audience.","Validation evidence such as tests, screenshots, logs, query results, metrics, traces, or review notes.","Short reflection on trade-offs, limitations, and one reasonable next improvement."],"reviewFocus":["Does the artifact directly match the project brief?","Can another person run, inspect, or review the result from the provided notes?","Are assumptions, limitations, and trade-offs stated clearly?"],"scopeGuardrails":["Keep the implementation focused on the stated project brief.","Prefer a complete small artifact over a broad unfinished system.","Treat completion as reviewable evidence, not a certification claim."],"claimLimit":"Completion creates reviewable project evidence. It does not certify mastery or guarantee job readiness.","evidenceRequired":true,"optionalPractice":false,"milestone":true,"nodeKey":"proj-bi-dashboard"}'::jsonb, true, true, '["Apply Dashboard Design, Data Storytelling, Business Metrics through a focused Interactive BI Dashboard artifact.","Explain the main design, data, tooling, workflow, or review decisions behind the project.","Validate the result with evidence that another person can inspect.","Document assumptions, limitations, and a concrete follow-up improvement."]'::jsonb, '["The a Interactive BI Dashboard workbook, report, or dashboard with documented assumptions is available for review.","The Interactive BI Dashboard scope, setup path, input data or scenario, and expected output are documented.","The main workflow runs or can be inspected through a notebook, repository, dashboard, prototype, report, configuration, or case study.","Validation evidence is included and directly relates to the project brief.","The notes identify trade-offs, limitations, and one next improvement without claiming mastery or job readiness."]'::jsonb),
('ph-python-analytics', NULL, 8, 'phase', NULL, NULL, NULL, 'Python for Data Analysis', 'Use Python notebooks and libraries to automate, analyze, visualize, and document repeatable analytical workflows.', 'trunk', 40, 'intermediate', '{"generatedBy": "roadmap-platform", "visualRole": "trunk"}'::jsonb, true, false, '["Explain the purpose and practical use of Python for Data Analysis.", "Apply Python for Data Analysis to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Python for Data Analysis in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('grp-python-analysis-foundations', 'ph-python-analytics', 1, 'choice_group', NULL, 'complete_all', 3, 'Python Analysis Foundations', 'Group the Python and notebook basics needed before deeper data analysis work.', 'side', 6, 'intermediate', '{"generatedBy": "roadmap-platform", "groupingPurpose": "phase_topic_group", "selectionType": "complete_all"}'::jsonb, true, false, '["Explain how the topics in Python Analysis Foundations work together in a data analyst workflow.", "Apply the grouped python analysis foundations skills to a practical analysis task."]'::jsonb, '["Complete all required child nodes in Python Analysis Foundations.", "Produce notes, examples, or project evidence that connects the grouped skills."]'::jsonb),
('grp-python-data-workflows', 'ph-python-analytics', 2, 'choice_group', NULL, 'complete_all', 4, 'Python Data Workflows', 'Group pandas, visualization, API extraction, and automation workflows for analyst productivity.', 'side', 10, 'intermediate', '{"generatedBy": "roadmap-platform", "groupingPurpose": "phase_topic_group", "selectionType": "complete_all"}'::jsonb, true, false, '["Explain how the topics in Python Data Workflows work together in a data analyst workflow.", "Apply the grouped python data workflows skills to a practical analysis task."]'::jsonb, '["Complete all required child nodes in Python Data Workflows.", "Produce notes, examples, or project evidence that connects the grouped skills."]'::jsonb),
('python-basics-for-analysts', 'grp-python-analysis-foundations', 1, 'topic', NULL, NULL, NULL, 'Python Basics for Analysts', 'Use variables, functions, control flow, data structures, files, and packages for analysis tasks.', 'choice', 5, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Python Basics for Analysts.", "Apply Python Basics for Analysts to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Python Basics for Analysts in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('notebook-workflows', 'grp-python-analysis-foundations', 2, 'topic', NULL, NULL, NULL, 'Notebook Workflows', 'Use notebooks for exploration while keeping cells ordered, assumptions documented, and outputs reproducible.', 'choice', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Notebook Workflows.", "Apply Notebook Workflows to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Notebook Workflows in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('numpy-for-analysis', 'grp-python-analysis-foundations', 3, 'topic', NULL, NULL, NULL, 'NumPy for Analysis', 'Use arrays, vectorized operations, masks, and numerical operations that support pandas and analytical computing.', 'choice', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of NumPy for Analysis.", "Apply NumPy for Analysis to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates NumPy for Analysis in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('pandas-data-analysis', 'grp-python-data-workflows', 1, 'topic', NULL, NULL, NULL, 'pandas Data Analysis', 'Load, filter, join, group, reshape, clean, and summarize tabular data with pandas.', 'choice', 6, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of pandas Data Analysis.", "Apply pandas Data Analysis to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates pandas Data Analysis in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('python-visualization', 'grp-python-data-workflows', 2, 'topic', NULL, NULL, NULL, 'Python Visualization', 'Create exploratory and explanatory charts with Matplotlib and seaborn.', 'choice', 5, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Python Visualization.", "Apply Python Visualization to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Python Visualization in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('api-data-extraction', 'grp-python-data-workflows', 3, 'topic', NULL, NULL, NULL, 'API Data Extraction', 'Pull JSON or CSV data from APIs, inspect responses, handle pagination, and save reproducible extracts.', 'choice', 5, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of API Data Extraction.", "Apply API Data Extraction to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates API Data Extraction in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('automation-scripts', 'grp-python-data-workflows', 4, 'topic', NULL, NULL, NULL, 'Analysis Automation Scripts', 'Convert repeated notebook or spreadsheet tasks into scripts with parameters, outputs, and basic logging.', 'choice', 5, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Analysis Automation Scripts.", "Apply Analysis Automation Scripts to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Analysis Automation Scripts in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('chk-python-analytics-review', 'ph-python-analytics', 90, 'checkpoint', 'assessment', NULL, NULL, 'Python Analytics Review Gate', 'Practical checkpoint for Python for Data Analysis. The learner completes a small diagnostic task or review artifact using Python Basics for Analysts, Notebook Workflows, NumPy for Analysis, pandas Data Analysis and records observations that can guide further practice.', 'checkpoint', 2, 'intermediate', '{"generatedBy": "roadmap-platform", "checkpointType": "assessment", "checkpointPurpose": "phase_review", "reviewMode": "assessment", "evidenceRequired": true, "claimLimit": "orientation_and_progress_review_only_not_certification"}'::jsonb, true, true, '["Apply selected concepts from Python for Data Analysis to a small practical scenario.", "Explain what worked, what failed, and which signals were used to debug or validate the result.", "Translate the result into a short next-action plan for continued learning."]'::jsonb, '["Complete a small practical task, lab, review exercise, or worked example related to this segment.", "Attach or describe the output, result, or evidence used for review.", "Explain at least one mistake, trade-off, or failure mode encountered during the task.", "Write the next practice action based on the result."]'::jsonb),
('proj-python-eda-notebook', 'ph-python-analytics', 100, 'project', NULL, NULL, NULL, 'Required Python EDA Notebook', 'Milestone project: Build a clean notebook that loads data, performs EDA, handles quality issues, visualizes findings, and exports a concise summary.', 'side', 8, 'intermediate', '{"project":true,"projectBrief":"Build a clean notebook that loads data, performs EDA, handles quality issues, visualizes findings, and exports a concise summary.","projectKind":"analytics_artifact","projectScope":"focused_milestone","projectPurpose":"Demonstrate the surrounding roadmap segment through a focused milestone artifact.","phaseContext":"Python for Data Analysis","deliverable":"a Python EDA Notebook workbook, report, or dashboard with documented assumptions","skillsToPractice":["Python","Exploratory Data Analysis","Data Visualization"],"suggestedSteps":["Define the Python EDA Notebook scenario, target user or reviewer, and the specific Python for Data Analysis concepts it will demonstrate.","Define the business question, audience, grain, metric formulas, and decision the artifact should support.","Prepare or clean the dataset and document assumptions, exclusions, and data-quality checks.","Build the report, dashboard, notebook, or workbook with the minimum views needed to answer the question.","Validate the numbers against source rows, spot checks, or reconciliation notes.","Write a short interpretation with caveats and recommended next actions.","Keep the scope focused enough to complete, review, and explain the a Python EDA Notebook workbook, report, or dashboard with documented assumptions."],"expectedEvidence":["a Python EDA Notebook workbook, report, or dashboard with documented assumptions.","README or notes that explain setup, usage, assumptions, and expected output.","Metric definitions, charts/tables, source checks, and a concise interpretation for the intended audience.","Validation evidence such as tests, screenshots, logs, query results, metrics, traces, or review notes.","Short reflection on trade-offs, limitations, and one reasonable next improvement."],"reviewFocus":["Does the artifact directly match the project brief?","Can another person run, inspect, or review the result from the provided notes?","Are assumptions, limitations, and trade-offs stated clearly?"],"scopeGuardrails":["Keep the implementation focused on the stated project brief.","Prefer a complete small artifact over a broad unfinished system.","Treat completion as reviewable evidence, not a certification claim."],"claimLimit":"Completion creates reviewable project evidence. It does not certify mastery or guarantee job readiness.","evidenceRequired":true,"optionalPractice":false,"milestone":true,"nodeKey":"proj-python-eda-notebook"}'::jsonb, true, true, '["Apply Python, Exploratory Data Analysis, Data Visualization through a focused Python EDA Notebook artifact.","Explain the main design, data, tooling, workflow, or review decisions behind the project.","Validate the result with evidence that another person can inspect.","Document assumptions, limitations, and a concrete follow-up improvement."]'::jsonb, '["The a Python EDA Notebook workbook, report, or dashboard with documented assumptions is available for review.","The Python EDA Notebook scope, setup path, input data or scenario, and expected output are documented.","The main workflow runs or can be inspected through a notebook, repository, dashboard, prototype, report, configuration, or case study.","Validation evidence is included and directly relates to the project brief.","The notes identify trade-offs, limitations, and one next improvement without claiming mastery or job readiness."]'::jsonb),
('ph-domain-analytics', NULL, 9, 'phase', NULL, NULL, NULL, 'Business Domain Analytics', 'Apply analytical techniques to common business domains so insights are grounded in realistic metrics and decisions.', 'trunk', 34, 'intermediate', '{"generatedBy": "roadmap-platform", "visualRole": "trunk"}'::jsonb, true, false, '["Explain the purpose and practical use of Business Domain Analytics.", "Apply Business Domain Analytics to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Business Domain Analytics in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('grp-product-customer-analytics', 'ph-domain-analytics', 1, 'choice_group', NULL, 'complete_all', 2, 'Product and Customer Analytics Patterns', 'Group core product and customer analysis patterns that apply across many business domains.', 'side', 5, 'intermediate', '{"generatedBy": "roadmap-platform", "groupingPurpose": "phase_topic_group", "selectionType": "complete_all"}'::jsonb, true, false, '["Explain how the topics in Product and Customer Analytics Patterns work together in a data analyst workflow.", "Apply the grouped product and customer analytics patterns skills to a practical analysis task."]'::jsonb, '["Complete all required child nodes in Product and Customer Analytics Patterns.", "Produce notes, examples, or project evidence that connects the grouped skills."]'::jsonb),
('grp-domain-tracks', 'ph-domain-analytics', 1, 'choice_group', NULL, 'choose_many', 2, 'Choose Business Domain Tracks', 'Complete at least two domain tracks to practice adapting analysis methods to different business contexts.', 'side', 10, 'intermediate', '{"generatedBy": "roadmap-platform", "choiceReason": "Data analysts often specialize by business domain."}'::jsonb, true, false, '["Explain the purpose and practical use of Choose Business Domain Tracks.", "Apply Choose Business Domain Tracks to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Choose Business Domain Tracks in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('product-analytics-track', 'grp-domain-tracks', 1, 'choice_option', NULL, NULL, NULL, 'Product Analytics Track', 'Analyze activation, engagement, retention, funnels, cohorts, experiments, and feature impact.', 'choice', 5, 'intermediate', '{"generatedBy": "roadmap-platform", "choiceOption": true}'::jsonb, true, true, '["Explain the purpose and practical use of Product Analytics Track.", "Apply Product Analytics Track to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Product Analytics Track in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('marketing-analytics-track', 'grp-domain-tracks', 2, 'choice_option', NULL, NULL, NULL, 'Marketing Analytics Track', 'Analyze campaign performance, acquisition channels, conversion rates, attribution limitations, and customer segments.', 'choice', 5, 'intermediate', '{"generatedBy": "roadmap-platform", "choiceOption": true}'::jsonb, true, true, '["Explain the purpose and practical use of Marketing Analytics Track.", "Apply Marketing Analytics Track to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Marketing Analytics Track in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('sales-analytics-track', 'grp-domain-tracks', 3, 'choice_option', NULL, NULL, NULL, 'Sales Analytics Track', 'Analyze pipeline, conversion, revenue, quota attainment, territory performance, and sales operations metrics.', 'choice', 5, 'intermediate', '{"generatedBy": "roadmap-platform", "choiceOption": true}'::jsonb, true, true, '["Explain the purpose and practical use of Sales Analytics Track.", "Apply Sales Analytics Track to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Sales Analytics Track in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('finance-analytics-track', 'grp-domain-tracks', 4, 'choice_option', NULL, NULL, NULL, 'Finance Analytics Track', 'Analyze budgets, variance, revenue, margin, cost drivers, and financial reporting caveats.', 'choice', 5, 'intermediate', '{"generatedBy": "roadmap-platform", "choiceOption": true}'::jsonb, true, true, '["Explain the purpose and practical use of Finance Analytics Track.", "Apply Finance Analytics Track to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Finance Analytics Track in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('operations-analytics-track', 'grp-domain-tracks', 5, 'choice_option', NULL, NULL, NULL, 'Operations Analytics Track', 'Analyze process throughput, cycle time, capacity, defects, inventory, and operational bottlenecks.', 'choice', 5, 'intermediate', '{"generatedBy": "roadmap-platform", "choiceOption": true}'::jsonb, true, true, '["Explain the purpose and practical use of Operations Analytics Track.", "Apply Operations Analytics Track to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Operations Analytics Track in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('cohort-analysis', 'grp-product-customer-analytics', 1, 'topic', NULL, NULL, NULL, 'Cohort Analysis', 'Group users or entities by start period or behavior and compare retention, usage, or value over time.', 'choice', 5, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Cohort Analysis.", "Apply Cohort Analysis to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Cohort Analysis in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('funnel-analysis', 'grp-product-customer-analytics', 2, 'topic', NULL, NULL, NULL, 'Funnel Analysis', 'Analyze step-by-step conversion, drop-off points, segmentation, and data instrumentation caveats.', 'choice', 5, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Funnel Analysis.", "Apply Funnel Analysis to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Funnel Analysis in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('chk-domain-analytics-review', 'ph-domain-analytics', 90, 'checkpoint', 'review', NULL, NULL, 'Domain Analytics Review Gate', 'Review checkpoint for Business Domain Analytics. The learner synthesizes completed topics such as Cohort Analysis, Funnel Analysis, Product Analytics Track, Marketing Analytics Track, records evidence, and updates the learning plan before continuing.', 'checkpoint', 2, 'intermediate', '{"generatedBy": "roadmap-platform", "checkpointType": "review", "checkpointPurpose": "phase_review", "reviewMode": "review", "evidenceRequired": true, "claimLimit": "orientation_and_progress_review_only_not_certification"}'::jsonb, true, true, '["Explain how topics from Business Domain Analytics connect to realistic data analysis work.", "Select evidence that shows attempted practice, reasoning, or implementation decisions.", "Identify which topics need review before moving to later roadmap areas."]'::jsonb, '["Write a concise review of the main ideas from this segment.", "Include one evidence artifact such as notes, a diagram, lab output, code, query results, screenshots, or a project review.", "Record open questions, weak areas, or follow-up actions.", "Mark the checkpoint complete only after the review artifact and next steps are captured."]'::jsonb),
('proj-domain-analytics-case-study', 'ph-domain-analytics', 100, 'project', NULL, NULL, NULL, 'Required Domain Analytics Case Study', 'Milestone project: Choose one business domain and produce a case study with metric definitions, SQL or Python analysis, visual evidence, and recommendations.', 'side', 8, 'intermediate', '{"project":true,"projectBrief":"Choose one business domain and produce a case study with metric definitions, SQL or Python analysis, visual evidence, and recommendations.","projectKind":"analytics_artifact","projectScope":"focused_milestone","projectPurpose":"Demonstrate the surrounding roadmap segment through a focused milestone artifact.","phaseContext":"Business Domain Analytics","deliverable":"a Domain Analytics Case Study workbook, report, or dashboard with documented assumptions","skillsToPractice":["Business Metrics","Data Storytelling"],"suggestedSteps":["Define the Domain Analytics Case Study scenario and list the specific items to demonstrate: SQL queries.","Define the business question, audience, grain, metric formulas, and decision the artifact should support.","Prepare or clean the dataset and document assumptions, exclusions, and data-quality checks.","Build the report, dashboard, notebook, or workbook with the minimum views needed to answer the question.","Validate the numbers against source rows, spot checks, or reconciliation notes.","Write a short interpretation with caveats and recommended next actions.","Keep the scope focused enough to complete, review, and explain the a Domain Analytics Case Study workbook, report, or dashboard with documented assumptions."],"expectedEvidence":["a Domain Analytics Case Study workbook, report, or dashboard with documented assumptions.","README or notes that explain setup, usage, assumptions, and expected output.","Metric definitions, charts/tables, source checks, and a concise interpretation for the intended audience.","Validation evidence such as tests, screenshots, logs, query results, metrics, traces, or review notes.","Short reflection on trade-offs, limitations, and one reasonable next improvement."],"reviewFocus":["Does the artifact directly match the project brief?","Can another person run, inspect, or review the result from the provided notes?","Are assumptions, limitations, and trade-offs stated clearly?"],"scopeGuardrails":["Keep the implementation focused on the stated project brief.","Prefer a complete small artifact over a broad unfinished system.","Treat completion as reviewable evidence, not a certification claim."],"claimLimit":"Completion creates reviewable project evidence. It does not certify mastery or guarantee job readiness.","evidenceRequired":true,"optionalPractice":false,"milestone":true,"nodeKey":"proj-domain-analytics-case-study"}'::jsonb, true, true, '["Apply Business Metrics, Data Storytelling through a focused Domain Analytics Case Study artifact.","Explain the main design, data, tooling, workflow, or review decisions behind the project.","Validate the result with evidence that another person can inspect.","Document assumptions, limitations, and a concrete follow-up improvement."]'::jsonb, '["The a Domain Analytics Case Study workbook, report, or dashboard with documented assumptions is available for review.","The Domain Analytics Case Study scope, setup path, input data or scenario, and expected output are documented.","The main workflow runs or can be inspected through a notebook, repository, dashboard, prototype, report, configuration, or case study.","Validation evidence is included and directly relates to the project brief.","The notes identify trade-offs, limitations, and one next improvement without claiming mastery or job readiness."]'::jsonb),
('ph-governance-collaboration', NULL, 10, 'phase', NULL, NULL, NULL, 'Governance, Privacy, and Analytics Collaboration', 'Learn the professional practices that keep analysis trusted, documented, privacy-aware, and maintainable.', 'trunk', 30, 'intermediate', '{"generatedBy": "roadmap-platform", "visualRole": "trunk"}'::jsonb, true, false, '["Explain the purpose and practical use of Governance, Privacy, and Analytics Collaboration.", "Apply Governance, Privacy, and Analytics Collaboration to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Governance, Privacy, and Analytics Collaboration in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('grp-analytics-governance-foundations', 'ph-governance-collaboration', 1, 'choice_group', NULL, 'complete_all', 3, 'Analytics Governance Foundations', 'Group shared definitions, lineage, privacy, and governance practices that make analytics trustworthy.', 'side', 6, 'intermediate', '{"generatedBy": "roadmap-platform", "groupingPurpose": "phase_topic_group", "selectionType": "complete_all"}'::jsonb, true, false, '["Explain how the topics in Analytics Governance Foundations work together in a data analyst workflow.", "Apply the grouped analytics governance foundations skills to a practical analysis task."]'::jsonb, '["Complete all required child nodes in Analytics Governance Foundations.", "Produce notes, examples, or project evidence that connects the grouped skills."]'::jsonb),
('grp-analytics-collaboration-delivery', 'ph-governance-collaboration', 2, 'choice_group', NULL, 'complete_all', 3, 'Collaboration and Delivery Workflow', 'Group collaboration, documentation, prioritization, and delivery habits for analytics teams.', 'side', 6, 'intermediate', '{"generatedBy": "roadmap-platform", "groupingPurpose": "phase_topic_group", "selectionType": "complete_all"}'::jsonb, true, false, '["Explain how the topics in Collaboration and Delivery Workflow work together in a data analyst workflow.", "Apply the grouped collaboration and delivery workflow skills to a practical analysis task."]'::jsonb, '["Complete all required child nodes in Collaboration and Delivery Workflow.", "Produce notes, examples, or project evidence that connects the grouped skills."]'::jsonb),
('metric-definitions', 'grp-analytics-governance-foundations', 1, 'topic', NULL, NULL, NULL, 'Metric Definitions and Semantic Consistency', 'Define metrics precisely, document grain and filters, and prevent conflicting dashboard definitions.', 'choice', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Metric Definitions and Semantic Consistency.", "Apply Metric Definitions and Semantic Consistency to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Metric Definitions and Semantic Consistency in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('data-catalogs-lineage', 'grp-analytics-governance-foundations', 2, 'topic', NULL, NULL, NULL, 'Data Catalogs and Lineage', 'Understand where data comes from, who owns it, how it changes, and how to trace analysis outputs back to sources.', 'choice', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Data Catalogs and Lineage.", "Apply Data Catalogs and Lineage to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Data Catalogs and Lineage in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('privacy-sensitive-data', 'grp-analytics-governance-foundations', 3, 'topic', NULL, NULL, NULL, 'Privacy and Sensitive Data Handling', 'Identify PII, reduce unnecessary exposure, and apply responsible handling practices in analysis outputs.', 'choice', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Privacy and Sensitive Data Handling.", "Apply Privacy and Sensitive Data Handling to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Privacy and Sensitive Data Handling in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('version-control-for-analysis', 'grp-analytics-collaboration-delivery', 1, 'topic', NULL, NULL, NULL, 'Version Control for Analysis', 'Use Git and GitHub to manage SQL, notebooks, dashboard documentation, and project history.', 'choice', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Version Control for Analysis.", "Apply Version Control for Analysis to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Version Control for Analysis in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('analytics-documentation', 'grp-analytics-collaboration-delivery', 2, 'topic', NULL, NULL, NULL, 'Analytics Documentation', 'Write README files, metric notes, data dictionaries, caveats, and dashboard usage guidance.', 'choice', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Analytics Documentation.", "Apply Analytics Documentation to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Analytics Documentation in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('analytics-backlog-and-prioritization', 'grp-analytics-collaboration-delivery', 3, 'topic', NULL, NULL, NULL, 'Analytics Backlog and Prioritization', 'Prioritize requests using impact, urgency, effort, data availability, and decision value.', 'choice', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Analytics Backlog and Prioritization.", "Apply Analytics Backlog and Prioritization to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Analytics Backlog and Prioritization in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('chk-governance-review', 'ph-governance-collaboration', 90, 'checkpoint', 'review', NULL, NULL, 'Governance and Collaboration Review Gate', 'Practical checkpoint for Governance, Privacy, and Analytics Collaboration. The learner completes a small diagnostic task or review artifact using Metric Definitions and Semantic Consistency, Data Catalogs and Lineage, Privacy and Sensitive Data Handling, Version Control for Analysis and records observations that can guide further practice.', 'checkpoint', 2, 'intermediate', '{"generatedBy": "roadmap-platform", "checkpointType": "review", "checkpointPurpose": "phase_review", "reviewMode": "assessment", "evidenceRequired": true, "claimLimit": "orientation_and_progress_review_only_not_certification"}'::jsonb, true, true, '["Apply selected concepts from Governance, Privacy, and Analytics Collaboration to a small practical scenario.", "Explain what worked, what failed, and which signals were used to debug or validate the result.", "Translate the result into a short next-action plan for continued learning."]'::jsonb, '["Complete a small practical task, lab, review exercise, or worked example related to this segment.", "Attach or describe the output, result, or evidence used for review.", "Explain at least one mistake, trade-off, or failure mode encountered during the task.", "Write the next practice action based on the result."]'::jsonb),
('proj-governed-metric-dictionary', 'ph-governance-collaboration', 100, 'project', NULL, NULL, NULL, 'Required Governed Metric Dictionary', 'Milestone project: Create a small governed metric dictionary with definitions, SQL logic, owners, caveats, dashboard links, and privacy notes.', 'side', 6, 'intermediate', '{"project":true,"projectBrief":"Create a small governed metric dictionary with definitions, SQL logic, owners, caveats, dashboard links, and privacy notes.","projectKind":"analytics_artifact","projectScope":"focused_milestone","projectPurpose":"Demonstrate the surrounding roadmap segment through a focused milestone artifact.","phaseContext":"Governance, Privacy, and Analytics Collaboration","deliverable":"a Governed Metric Dictionary workbook, report, or dashboard with documented assumptions","skillsToPractice":["Data Governance","Business Metrics","Stakeholder Communication"],"suggestedSteps":["Define the Governed Metric Dictionary scenario and list the specific items to demonstrate: SQL queries, dashboard decisions.","Define the business question, audience, grain, metric formulas, and decision the artifact should support.","Prepare or clean the dataset and document assumptions, exclusions, and data-quality checks.","Build the report, dashboard, notebook, or workbook with the minimum views needed to answer the question.","Validate the numbers against source rows, spot checks, or reconciliation notes.","Write a short interpretation with caveats and recommended next actions.","Keep the scope focused enough to complete, review, and explain the a Governed Metric Dictionary workbook, report, or dashboard with documented assumptions."],"expectedEvidence":["a Governed Metric Dictionary workbook, report, or dashboard with documented assumptions.","README or notes that explain setup, usage, assumptions, and expected output.","Metric definitions, charts/tables, source checks, and a concise interpretation for the intended audience.","Validation evidence such as tests, screenshots, logs, query results, metrics, traces, or review notes.","Short reflection on trade-offs, limitations, and one reasonable next improvement."],"reviewFocus":["Does the artifact directly match the project brief?","Can another person run, inspect, or review the result from the provided notes?","Are assumptions, limitations, and trade-offs stated clearly?"],"scopeGuardrails":["Keep the implementation focused on the stated project brief.","Prefer a complete small artifact over a broad unfinished system.","Treat completion as reviewable evidence, not a certification claim."],"claimLimit":"Completion creates reviewable project evidence. It does not certify mastery or guarantee job readiness.","evidenceRequired":true,"optionalPractice":false,"milestone":true,"nodeKey":"proj-governed-metric-dictionary"}'::jsonb, true, true, '["Apply Data Governance, Business Metrics, Stakeholder Communication through a focused Governed Metric Dictionary artifact.","Explain the main design, data, tooling, workflow, or review decisions behind the project.","Validate the result with evidence that another person can inspect.","Document assumptions, limitations, and a concrete follow-up improvement."]'::jsonb, '["The a Governed Metric Dictionary workbook, report, or dashboard with documented assumptions is available for review.","The Governed Metric Dictionary scope, setup path, input data or scenario, and expected output are documented.","The main workflow runs or can be inspected through a notebook, repository, dashboard, prototype, report, configuration, or case study.","Validation evidence is included and directly relates to the project brief.","The notes identify trade-offs, limitations, and one next improvement without claiming mastery or job readiness."]'::jsonb),
('ph-advanced-analytics-awareness', NULL, 11, 'phase', NULL, NULL, NULL, 'Advanced Analytics Awareness', 'Build enough advanced analytics literacy to collaborate with data scientists and avoid overclaiming from basic analyses.', 'trunk', 32, 'advanced', '{"generatedBy": "roadmap-platform", "visualRole": "trunk"}'::jsonb, true, false, '["Explain the purpose and practical use of Advanced Analytics Awareness.", "Apply Advanced Analytics Awareness to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Advanced Analytics Awareness in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('grp-predictive-statistical-awareness', 'ph-advanced-analytics-awareness', 1, 'choice_group', NULL, 'complete_all', 4, 'Predictive and Statistical Awareness', 'Group advanced analytical methods that a data analyst should understand without becoming a full data scientist.', 'side', 9, 'advanced', '{"generatedBy": "roadmap-platform", "groupingPurpose": "phase_topic_group", "selectionType": "complete_all"}'::jsonb, true, false, '["Explain how the topics in Predictive and Statistical Awareness work together in a data analyst workflow.", "Apply the grouped predictive and statistical awareness skills to a practical analysis task."]'::jsonb, '["Complete all required child nodes in Predictive and Statistical Awareness.", "Produce notes, examples, or project evidence that connects the grouped skills."]'::jsonb),
('grp-specialized-ai-analytics-awareness', 'ph-advanced-analytics-awareness', 2, 'choice_group', NULL, 'complete_all', 2, 'Specialized and AI-Assisted Analysis Awareness', 'Group specialized analysis modes and AI-assisted workflows that can extend analyst productivity.', 'side', 4, 'advanced', '{"generatedBy": "roadmap-platform", "groupingPurpose": "phase_topic_group", "selectionType": "complete_all"}'::jsonb, true, false, '["Explain how the topics in Specialized and AI-Assisted Analysis Awareness work together in a data analyst workflow.", "Apply the grouped specialized and ai-assisted analysis awareness skills to a practical analysis task."]'::jsonb, '["Complete all required child nodes in Specialized and AI-Assisted Analysis Awareness.", "Produce notes, examples, or project evidence that connects the grouped skills."]'::jsonb),
('forecasting-awareness', 'grp-predictive-statistical-awareness', 1, 'topic', NULL, NULL, NULL, 'Forecasting Awareness', 'Understand trend, seasonality, baseline forecasts, forecast errors, and when forecasting assumptions are fragile.', 'choice', 5, 'advanced', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Forecasting Awareness.", "Apply Forecasting Awareness to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Forecasting Awareness in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('segmentation-and-clustering-awareness', 'grp-predictive-statistical-awareness', 2, 'topic', NULL, NULL, NULL, 'Segmentation and Clustering Awareness', 'Understand analyst-friendly segmentation and clustering concepts, validation issues, and business interpretation risks.', 'choice', 5, 'advanced', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Segmentation and Clustering Awareness.", "Apply Segmentation and Clustering Awareness to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Segmentation and Clustering Awareness in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('predictive-modeling-awareness', 'grp-predictive-statistical-awareness', 3, 'topic', NULL, NULL, NULL, 'Predictive Modeling Awareness', 'Understand supervised prediction, train/test split, leakage, model metrics, and why analysts should be cautious with claims.', 'choice', 5, 'advanced', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Predictive Modeling Awareness.", "Apply Predictive Modeling Awareness to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Predictive Modeling Awareness in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('anomaly-detection-awareness', 'grp-predictive-statistical-awareness', 4, 'topic', NULL, NULL, NULL, 'Anomaly Detection Awareness', 'Identify unusual behavior in metrics and understand when anomaly alerts require business or data quality investigation.', 'choice', 4, 'advanced', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Anomaly Detection Awareness.", "Apply Anomaly Detection Awareness to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Anomaly Detection Awareness in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('geospatial-analysis-awareness', 'grp-specialized-ai-analytics-awareness', 1, 'topic', NULL, NULL, NULL, 'Geospatial Analysis Awareness', 'Understand when location data, maps, regional aggregation, and privacy constraints matter in analysis.', 'choice', 4, 'advanced', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Geospatial Analysis Awareness.", "Apply Geospatial Analysis Awareness to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Geospatial Analysis Awareness in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('ai-assisted-analysis-awareness', 'grp-specialized-ai-analytics-awareness', 2, 'topic', NULL, NULL, NULL, 'AI-Assisted Analysis Awareness', 'Use AI tools cautiously for SQL, Python, summaries, and chart ideas while validating correctness and protecting sensitive data.', 'choice', 4, 'advanced', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of AI-Assisted Analysis Awareness.", "Apply AI-Assisted Analysis Awareness to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates AI-Assisted Analysis Awareness in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('chk-advanced-awareness-review', 'ph-advanced-analytics-awareness', 90, 'checkpoint', 'review', NULL, NULL, 'Advanced Analytics Awareness Review Gate', 'Review checkpoint for Advanced Analytics Awareness. The learner synthesizes completed topics such as Forecasting Awareness, Segmentation and Clustering Awareness, Predictive Modeling Awareness, Anomaly Detection Awareness, records evidence, and updates the learning plan before continuing.', 'checkpoint', 2, 'advanced', '{"generatedBy": "roadmap-platform", "checkpointType": "review", "checkpointPurpose": "phase_review", "reviewMode": "review", "evidenceRequired": true, "claimLimit": "orientation_and_progress_review_only_not_certification"}'::jsonb, true, true, '["Explain how topics from Advanced Analytics Awareness connect to realistic data analysis work.", "Select evidence that shows attempted practice, reasoning, or implementation decisions.", "Identify which topics need review before moving to later roadmap areas."]'::jsonb, '["Write a concise review of the main ideas from this segment.", "Include one evidence artifact such as notes, a diagram, lab output, code, query results, screenshots, or a project review.", "Record open questions, weak areas, or follow-up actions.", "Mark the checkpoint complete only after the review artifact and next steps are captured."]'::jsonb),
('proj-advanced-analytics-brief', 'ph-advanced-analytics-awareness', 100, 'project', NULL, NULL, NULL, 'Optional Advanced Analytics Brief', 'Optional practice project: Write a brief comparing one advanced method with a simpler baseline and explain when the advanced method is or is not justified.', 'side', 4, 'advanced', '{"project":true,"projectBrief":"Write a brief comparing one advanced method with a simpler baseline and explain when the advanced method is or is not justified.","projectKind":"analytics_artifact","projectScope":"small_practice_artifact","projectPurpose":"Practice the surrounding roadmap segment through a small reviewable artifact.","phaseContext":"Advanced Analytics Awareness","deliverable":"a Advanced Analytics Brief workbook, report, or dashboard with documented assumptions","skillsToPractice":["Statistics","Data Storytelling"],"suggestedSteps":["Define the Advanced Analytics Brief scenario, target user or reviewer, and the specific Advanced Analytics Awareness concepts it will demonstrate.","Define the business question, audience, grain, metric formulas, and decision the artifact should support.","Prepare or clean the dataset and document assumptions, exclusions, and data-quality checks.","Build the report, dashboard, notebook, or workbook with the minimum views needed to answer the question.","Validate the numbers against source rows, spot checks, or reconciliation notes.","Write a short interpretation with caveats and recommended next actions."],"expectedEvidence":["a Advanced Analytics Brief workbook, report, or dashboard with documented assumptions.","README or notes that explain setup, usage, assumptions, and expected output.","Metric definitions, charts/tables, source checks, and a concise interpretation for the intended audience.","Validation evidence such as tests, screenshots, logs, query results, metrics, traces, or review notes.","Short reflection on trade-offs, limitations, and one reasonable next improvement."],"reviewFocus":["Does the artifact directly match the project brief?","Can another person run, inspect, or review the result from the provided notes?","Are assumptions, limitations, and trade-offs stated clearly?"],"scopeGuardrails":["Keep the implementation focused on the stated project brief.","Prefer a complete small artifact over a broad unfinished system.","Treat completion as reviewable evidence, not a certification claim."],"claimLimit":"Completion creates reviewable project evidence. It does not certify mastery or guarantee job readiness.","evidenceRequired":true,"optionalPractice":true,"milestone":false,"nodeKey":"proj-advanced-analytics-brief"}'::jsonb, false, true, '["Apply Statistics, Data Storytelling through a focused Advanced Analytics Brief artifact.","Explain the main design, data, tooling, workflow, or review decisions behind the project.","Validate the result with evidence that another person can inspect.","Document assumptions, limitations, and a concrete follow-up improvement."]'::jsonb, '["The a Advanced Analytics Brief workbook, report, or dashboard with documented assumptions is available for review.","The Advanced Analytics Brief scope, setup path, input data or scenario, and expected output are documented.","The main workflow runs or can be inspected through a notebook, repository, dashboard, prototype, report, configuration, or case study.","Validation evidence is included and directly relates to the project brief.","The notes identify trade-offs, limitations, and one next improvement without claiming mastery or job readiness."]'::jsonb),
('ph-portfolio-capstone', NULL, 12, 'phase', NULL, NULL, NULL, 'Portfolio, Interview Readiness, and Capstone', 'Package analysis skills into portfolio projects, interview stories, and a capstone that demonstrates job-ready data analyst competence.', 'trunk', 42, 'advanced', '{"generatedBy": "roadmap-platform", "visualRole": "trunk"}'::jsonb, true, false, '["Explain the purpose and practical use of Portfolio, Interview Readiness, and Capstone.", "Apply Portfolio, Interview Readiness, and Capstone to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Portfolio, Interview Readiness, and Capstone in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('grp-portfolio-packaging', 'ph-portfolio-capstone', 1, 'choice_group', NULL, 'complete_all', 4, 'Portfolio Packaging and Positioning', 'Group the artifacts that make analysis projects clear, credible, and portfolio-ready.', 'side', 7, 'advanced', '{"generatedBy": "roadmap-platform", "groupingPurpose": "phase_topic_group", "selectionType": "complete_all"}'::jsonb, true, false, '["Explain how the topics in Portfolio Packaging and Positioning work together in a data analyst workflow.", "Apply the grouped portfolio packaging and positioning skills to a practical analysis task."]'::jsonb, '["Complete all required child nodes in Portfolio Packaging and Positioning.", "Produce notes, examples, or project evidence that connects the grouped skills."]'::jsonb),
('grp-analytics-interview-readiness', 'ph-portfolio-capstone', 2, 'choice_group', NULL, 'complete_all', 2, 'Analytics Interview Readiness', 'Group interview practice for SQL, analytical reasoning, stakeholder cases, and business interpretation.', 'side', 5, 'advanced', '{"generatedBy": "roadmap-platform", "groupingPurpose": "phase_topic_group", "selectionType": "complete_all"}'::jsonb, true, false, '["Explain how the topics in Analytics Interview Readiness work together in a data analyst workflow.", "Apply the grouped analytics interview readiness skills to a practical analysis task."]'::jsonb, '["Complete all required child nodes in Analytics Interview Readiness.", "Produce notes, examples, or project evidence that connects the grouped skills."]'::jsonb),
('portfolio-project-selection', 'grp-portfolio-packaging', 1, 'topic', NULL, NULL, NULL, 'Portfolio Project Selection', 'Choose projects that show business framing, data cleaning, SQL or Python analysis, visualization, and communication.', 'choice', 4, 'advanced', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Portfolio Project Selection.", "Apply Portfolio Project Selection to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Portfolio Project Selection in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('case-study-writing', 'grp-portfolio-packaging', 2, 'topic', NULL, NULL, NULL, 'Case Study Writing', 'Write project narratives that explain context, question, data, method, insights, limitations, and business recommendation.', 'choice', 4, 'advanced', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Case Study Writing.", "Apply Case Study Writing to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Case Study Writing in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('sql-interview-practice', 'grp-analytics-interview-readiness', 1, 'topic', NULL, NULL, NULL, 'SQL Interview Practice', 'Practice joins, aggregations, windows, edge cases, and explaining query logic under interview constraints.', 'choice', 5, 'advanced', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of SQL Interview Practice.", "Apply SQL Interview Practice to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates SQL Interview Practice in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('analytics-case-interviews', 'grp-analytics-interview-readiness', 2, 'topic', NULL, NULL, NULL, 'Analytics Case Interviews', 'Practice clarifying ambiguous business problems, identifying metrics, structuring analysis, and presenting recommendations.', 'choice', 5, 'advanced', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Analytics Case Interviews.", "Apply Analytics Case Interviews to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Analytics Case Interviews in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('dashboard-portfolio-polish', 'grp-portfolio-packaging', 3, 'topic', NULL, NULL, NULL, 'Dashboard Portfolio Polish', 'Polish dashboards for readability, documentation, sharing, performance, and reviewer understanding.', 'choice', 4, 'advanced', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Dashboard Portfolio Polish.", "Apply Dashboard Portfolio Polish to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Dashboard Portfolio Polish in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('resume-and-project-positioning', 'grp-portfolio-packaging', 4, 'topic', NULL, NULL, NULL, 'Resume and Project Positioning', 'Describe analyst projects with concrete business questions, tools, methods, outcomes, and trade-offs.', 'choice', 3, 'advanced', '{"generatedBy": "roadmap-platform", "curriculumRole": "topic"}'::jsonb, true, true, '["Explain the purpose and practical use of Resume and Project Positioning.", "Apply Resume and Project Positioning to a realistic data analyst workflow."]'::jsonb, '["Produce evidence that demonstrates Resume and Project Positioning in context.", "Document assumptions, decisions, and results clearly enough for review."]'::jsonb),
('chk-final-readiness-review', 'ph-portfolio-capstone', 90, 'checkpoint', 'gate', NULL, NULL, 'Final Data Analyst Readiness Review', 'Final roadmap checkpoint for Portfolio, Interview Readiness, and Capstone. The learner reviews portfolio evidence, unresolved gaps, and next learning priorities across the data analysis path.', 'checkpoint', 3, 'advanced', '{"generatedBy": "roadmap-platform", "checkpointType": "gate", "checkpointPurpose": "phase_review", "reviewMode": "final_review", "evidenceRequired": true, "claimLimit": "orientation_and_progress_review_only_not_certification"}'::jsonb, true, true, '["Connect completed roadmap work to a coherent data analysis learning narrative.", "Review portfolio or project evidence and identify which artifacts are ready to share.", "Define remaining gaps and future specialization priorities."]'::jsonb, '["Summarize completed phases, projects, and major skill groups.", "Attach or reference portfolio evidence, project links, diagrams, reports, tests, or notes where available.", "List remaining gaps or specialization topics for future learning.", "State that the checkpoint is a planning review, not proof of job readiness."]'::jsonb),
('proj-data-analyst-capstone', 'ph-portfolio-capstone', 100, 'project', NULL, NULL, NULL, 'Required Data Analyst Capstone Project', 'Capstone project: Complete an end-to-end data analyst project with data cleaning, SQL or Python analysis, dashboard or insight deck, metric definitions, stakeholder recommendation, and portfolio-ready documentation.', 'side', 14, 'advanced', '{"project":true,"projectBrief":"Complete an end-to-end data analyst project with data cleaning, SQL or Python analysis, dashboard or insight deck, metric definitions, stakeholder recommendation, and portfolio-ready documentation.","projectKind":"portfolio_capstone","projectScope":"portfolio_capstone","projectPurpose":"Integrate several roadmap areas into a portfolio-ready case study.","phaseContext":"Portfolio, Interview Readiness, and Capstone","deliverable":"a portfolio-ready Data Analyst Capstone Project case study and integrated artifact","skillsToPractice":["SQL","Data Storytelling","Dashboard Design"],"suggestedSteps":["Define the Data Analyst Capstone Project scenario and list the specific items to demonstrate: SQL queries, dashboard decisions.","Choose a coherent product or case-study scenario that can integrate the strongest projects from the roadmap.","Define the architecture, data or workflow model, user path, and review evidence before implementation.","Build the core artifact end-to-end with scoped features rather than disconnected demos.","Add validation evidence appropriate to the role, such as tests, metrics, query checks, playtest notes, traces, screenshots, or incident notes.","Prepare a case study explaining decisions, trade-offs, limitations, and what would need further review for real deployment.","Package the repository or artifact so another person can reproduce or inspect the result.","Create a review checklist that connects the artifact to the relevant skills and roadmap segment.","Write a limitation and risk section so the project is presented as reviewable evidence, not a certification claim."],"expectedEvidence":["a portfolio-ready Data Analyst Capstone Project case study and integrated artifact.","README or notes that explain setup, usage, assumptions, and expected output.","Case study page or write-up that connects the artifact to role-relevant decisions and evidence.","Validation evidence such as tests, screenshots, logs, query results, metrics, traces, or review notes.","Short reflection on trade-offs, limitations, and one reasonable next improvement.","Architecture, review checklist, and limitation section suitable for portfolio discussion."],"reviewFocus":["Does the artifact directly match the project brief?","Can another person run, inspect, or review the result from the provided notes?","Are assumptions, limitations, and trade-offs stated clearly?"],"scopeGuardrails":["Keep the implementation focused on the stated project brief.","Prefer a complete small artifact over a broad unfinished system.","Treat completion as reviewable evidence, not a certification claim."],"claimLimit":"Completion creates reviewable project evidence. It does not certify mastery or guarantee job readiness.","evidenceRequired":true,"optionalPractice":false,"milestone":true,"nodeKey":"proj-data-analyst-capstone"}'::jsonb, true, true, '["Apply SQL, Data Storytelling, Dashboard Design through a focused Data Analyst Capstone Project artifact.","Integrate multiple roadmap areas into one coherent case study rather than isolated exercises.","Explain the main design, data, tooling, workflow, or review decisions behind the project.","Validate the result with evidence that another person can inspect.","Document assumptions, limitations, and a concrete follow-up improvement."]'::jsonb, '["The a portfolio-ready Data Analyst Capstone Project case study and integrated artifact is available for review.","The Data Analyst Capstone Project scope, setup path, input data or scenario, and expected output are documented.","The main workflow runs or can be inspected through a notebook, repository, dashboard, prototype, report, configuration, or case study.","The case study connects implementation decisions to relevant roadmap skills and explains why alternatives were not chosen.","Validation evidence is included and directly relates to the project brief.","The notes identify trade-offs, limitations, and one next improvement without claiming mastery or job readiness.","The project is packaged so a reviewer can understand the artifact without live mentoring."]'::jsonb),
('proj-public-portfolio-package', 'ph-portfolio-capstone', 101, 'project', NULL, NULL, NULL, 'Required Public Portfolio Package', 'Capstone project: Publish selected analyst projects with clean READMEs, screenshots or dashboard links, methodology notes, limitations, and interview-ready talking points.', 'side', 6, 'advanced', '{"project":true,"projectBrief":"Publish selected analyst projects with clean READMEs, screenshots or dashboard links, methodology notes, limitations, and interview-ready talking points.","projectKind":"portfolio_capstone","projectScope":"portfolio_capstone","projectPurpose":"Integrate several roadmap areas into a portfolio-ready case study.","phaseContext":"Portfolio, Interview Readiness, and Capstone","deliverable":"a portfolio-ready Public Portfolio Package case study and integrated artifact","skillsToPractice":["GitHub","Technical Case Studies","Interview Readiness"],"suggestedSteps":["Define the Public Portfolio Package scenario and list the specific items to demonstrate: dashboard decisions.","Choose a coherent product or case-study scenario that can integrate the strongest projects from the roadmap.","Define the architecture, data or workflow model, user path, and review evidence before implementation.","Build the core artifact end-to-end with scoped features rather than disconnected demos.","Add validation evidence appropriate to the role, such as tests, metrics, query checks, playtest notes, traces, screenshots, or incident notes.","Prepare a case study explaining decisions, trade-offs, limitations, and what would need further review for real deployment.","Package the repository or artifact so another person can reproduce or inspect the result.","Create a review checklist that connects the artifact to the relevant skills and roadmap segment.","Write a limitation and risk section so the project is presented as reviewable evidence, not a certification claim."],"expectedEvidence":["a portfolio-ready Public Portfolio Package case study and integrated artifact.","README or notes that explain setup, usage, assumptions, and expected output.","Case study page or write-up that connects the artifact to role-relevant decisions and evidence.","Validation evidence such as tests, screenshots, logs, query results, metrics, traces, or review notes.","Short reflection on trade-offs, limitations, and one reasonable next improvement.","Architecture, review checklist, and limitation section suitable for portfolio discussion."],"reviewFocus":["Does the artifact directly match the project brief?","Can another person run, inspect, or review the result from the provided notes?","Are assumptions, limitations, and trade-offs stated clearly?"],"scopeGuardrails":["Keep the implementation focused on the stated project brief.","Prefer a complete small artifact over a broad unfinished system.","Treat completion as reviewable evidence, not a certification claim."],"claimLimit":"Completion creates reviewable project evidence. It does not certify mastery or guarantee job readiness.","evidenceRequired":true,"optionalPractice":false,"milestone":true,"nodeKey":"proj-public-portfolio-package"}'::jsonb, true, true, '["Apply GitHub, Technical Case Studies, Interview Readiness through a focused Public Portfolio Package artifact.","Integrate multiple roadmap areas into one coherent case study rather than isolated exercises.","Explain the main design, data, tooling, workflow, or review decisions behind the project.","Validate the result with evidence that another person can inspect.","Document assumptions, limitations, and a concrete follow-up improvement."]'::jsonb, '["The a portfolio-ready Public Portfolio Package case study and integrated artifact is available for review.","The Public Portfolio Package scope, setup path, input data or scenario, and expected output are documented.","The main workflow runs or can be inspected through a notebook, repository, dashboard, prototype, report, configuration, or case study.","The case study connects implementation decisions to relevant roadmap skills and explains why alternatives were not chosen.","Validation evidence is included and directly relates to the project brief.","The notes identify trade-offs, limitations, and one next improvement without claiming mastery or job readiness.","The project is packaged so a reviewer can understand the artifact without live mentoring."]'::jsonb);

INSERT INTO public.roadmap_node (roadmap_node_id, roadmap_version_id, parent_node_id, slug, node_type, checkpoint_type, selection_type, required_count, title, description, order_index, layout_role, estimated_hours, difficulty_level, metadata, is_required, is_trackable, learning_outcomes, completion_criteria)
SELECT sn.node_id, m.roadmap_version_id, parent.node_id, sn.node_key, sn.node_type, sn.checkpoint_type, sn.selection_type, sn.required_count, sn.title, sn.description, sn.order_index, sn.layout_role, sn.estimated_hours, sn.difficulty_level, sn.metadata, sn.is_required, sn.is_trackable, sn.learning_outcomes, sn.completion_criteria
FROM seed_node sn
CROSS JOIN seed_roadmap_map m
LEFT JOIN seed_node parent ON parent.node_key = sn.parent_key
ON CONFLICT (roadmap_version_id, slug) DO UPDATE SET
    parent_node_id = EXCLUDED.parent_node_id,
    node_type = EXCLUDED.node_type,
    checkpoint_type = EXCLUDED.checkpoint_type,
    selection_type = EXCLUDED.selection_type,
    required_count = EXCLUDED.required_count,
    title = EXCLUDED.title,
    description = EXCLUDED.description,
    order_index = EXCLUDED.order_index,
    layout_role = EXCLUDED.layout_role,
    estimated_hours = EXCLUDED.estimated_hours,
    difficulty_level = EXCLUDED.difficulty_level,
    metadata = EXCLUDED.metadata,
    is_required = EXCLUDED.is_required,
    is_trackable = EXCLUDED.is_trackable,
    learning_outcomes = EXCLUDED.learning_outcomes,
    completion_criteria = EXCLUDED.completion_criteria;

-- Node-skill mappings.
DROP TABLE IF EXISTS seed_node_skill;
CREATE TEMP TABLE seed_node_skill (
    node_key text NOT NULL,
    skill_slug text NOT NULL,
    PRIMARY KEY (node_key, skill_slug)
) ON COMMIT DROP;
INSERT INTO seed_node_skill VALUES


('data-analyst-role', 'data-analyst-role-and-responsibilities'),
('data-analyst-role', 'business-metrics'),
('data-analyst-role', 'data-storytelling'),
('analytics-lifecycle', 'analytics-lifecycle'),
('analytics-lifecycle', 'stakeholder-communication'),
('analytics-lifecycle', 'data-quality'),
('business-questions-and-kpis', 'business-questions-and-kpis'),
('business-questions-and-kpis', 'business-metrics'),
('business-questions-and-kpis', 'stakeholder-communication'),
('data-literacy-and-ethics', 'data-literacy'),
('data-literacy-and-ethics', 'data-governance'),
('data-literacy-and-ethics', 'privacy-and-data-protection'),
('requirements-intake', 'analytics-requirements-intake'),
('requirements-intake', 'stakeholder-communication'),
('analytical-thinking-basics', 'analytical-thinking'),
('analytical-thinking-basics', 'business-metrics'),
('analytical-thinking-basics', 'descriptive-statistics'),
('chk-da-foundations-review', 'data-analyst-foundations-review'),
('proj-analytics-question-brief', 'stakeholder-communication'),
('proj-analytics-question-brief', 'business-metrics'),
('spreadsheet-structure', 'workbook-structure-and-tables'),
('spreadsheet-structure', 'spreadsheets'),
('spreadsheet-structure', 'excel'),
('excel-formulas-functions', 'excel-formulas-and-functions'),
('excel-formulas-functions', 'excel'),
('excel-formulas-functions', 'spreadsheets'),
('lookup-and-reference-functions', 'lookup-and-reference-functions'),
('lookup-and-reference-functions', 'excel'),
('pivot-tables', 'pivot-tables-and-summaries'),
('pivot-tables', 'excel'),
('pivot-tables', 'descriptive-statistics'),
('spreadsheet-cleaning', 'spreadsheet-data-cleaning'),
('spreadsheet-cleaning', 'data-cleaning'),
('spreadsheet-cleaning', 'data-quality'),
('spreadsheet-cleaning', 'excel'),
('power-query-basics', 'power-query'),
('power-query-basics', 'excel'),
('power-query-basics', 'data-cleaning'),
('spreadsheet-charts', 'spreadsheet-charts-and-reports'),
('spreadsheet-charts', 'data-visualization'),
('spreadsheet-charts', 'data-storytelling'),
('spreadsheet-charts', 'excel'),
('chk-spreadsheet-review', 'spreadsheet-analysis-review'),
('proj-excel-sales-analysis', 'excel'),
('proj-excel-sales-analysis', 'spreadsheets'),
('proj-excel-sales-analysis', 'data-storytelling'),
('relational-data-models', 'relational-data-models'),
('relational-data-models', 'relational-databases'),
('relational-data-models', 'sql'),
('select-filter-sort', 'select-filtering-and-sorting'),
('select-filter-sort', 'sql'),
('joins-and-relationships', 'joins-and-relationships'),
('joins-and-relationships', 'sql'),
('joins-and-relationships', 'relational-databases'),
('aggregation-and-grouping', 'aggregation-and-grouping'),
('aggregation-and-grouping', 'sql'),
('aggregation-and-grouping', 'descriptive-statistics'),
('ctes-and-subqueries', 'ctes-and-subqueries'),
('ctes-and-subqueries', 'sql'),
('window-functions', 'window-functions'),
('window-functions', 'sql'),
('sql-data-quality-checks', 'sql-data-quality-checks'),
('sql-data-quality-checks', 'sql'),
('sql-data-quality-checks', 'data-quality'),
('sql-data-quality-checks', 'data-validation'),
('query-debugging-performance', 'query-debugging-and-performance'),
('query-debugging-performance', 'sql'),
('query-debugging-performance', 'performance-analysis'),
('chk-sql-review', 'sql-analysis-review'),
('proj-sql-business-analysis', 'sql'),
('proj-sql-business-analysis', 'data-quality'),
('proj-sql-business-analysis', 'business-metrics'),
('descriptive-statistics-analysis', 'descriptive-statistics'),
('descriptive-statistics-analysis', 'statistics'),
('distributions-and-sampling', 'distributions-and-sampling'),
('distributions-and-sampling', 'statistics'),
('confidence-intervals', 'confidence-intervals'),
('confidence-intervals', 'statistics'),
('hypothesis-testing-basics', 'hypothesis-testing'),
('hypothesis-testing-basics', 'statistics'),
('correlation-causation', 'correlation-vs-causation'),
('correlation-causation', 'statistics'),
('correlation-causation', 'data-storytelling'),
('ab-test-interpretation', 'a-b-test-interpretation'),
('ab-test-interpretation', 'a-b-testing'),
('ab-test-interpretation', 'statistics'),
('regression-awareness', 'regression-analysis'),
('regression-awareness', 'statistics'),
('proj-statistical-insight-report', 'statistics'),
('proj-statistical-insight-report', 'data-storytelling'),
('data-profiling', 'data-profiling'),
('data-profiling', 'exploratory-data-analysis'),
('data-profiling', 'data-quality'),
('missing-data-handling', 'missing-data-handling'),
('missing-data-handling', 'data-cleaning'),
('missing-data-handling', 'data-quality'),
('duplicates-and-entity-resolution', 'duplicates-and-entity-resolution'),
('duplicates-and-entity-resolution', 'data-cleaning'),
('duplicates-and-entity-resolution', 'data-quality'),
('outliers-and-anomalies', 'outliers-and-anomalies'),
('outliers-and-anomalies', 'exploratory-data-analysis'),
('outliers-and-anomalies', 'data-quality'),
('data-type-and-format-standardization', 'data-types-and-format-standardization'),
('data-type-and-format-standardization', 'data-cleaning'),
('data-validation-rules', 'data-validation-rules'),
('data-validation-rules', 'data-validation'),
('data-validation-rules', 'data-quality'),
('reproducible-cleaning-workflows', 'reproducible-cleaning-workflows'),
('reproducible-cleaning-workflows', 'data-cleaning'),
('reproducible-cleaning-workflows', 'git'),
('proj-cleaning-pipeline-report', 'data-cleaning'),
('proj-cleaning-pipeline-report', 'data-validation'),
('proj-cleaning-pipeline-report', 'data-quality'),
('chart-selection', 'chart-selection'),
('chart-selection', 'data-visualization'),
('visual-design-principles', 'visual-design-principles'),
('visual-design-principles', 'data-visualization'),
('visual-design-principles', 'data-storytelling'),
('accessibility-in-data-visualization', 'accessibility-in-data-visualization'),
('accessibility-in-data-visualization', 'accessibility'),
('accessibility-in-data-visualization', 'data-visualization'),
('dashboard-purpose-and-layout', 'dashboard-purpose-and-layout'),
('dashboard-purpose-and-layout', 'dashboard-design'),
('dashboard-purpose-and-layout', 'stakeholder-communication'),
('insight-narrative', 'insight-narrative'),
('insight-narrative', 'data-storytelling'),
('insight-narrative', 'stakeholder-communication'),
('presentation-delivery', 'presentation-delivery'),
('presentation-delivery', 'stakeholder-communication'),
('presentation-delivery', 'data-storytelling'),
('misleading-visuals', 'avoiding-misleading-visuals'),
('misleading-visuals', 'data-visualization'),
('misleading-visuals', 'statistics'),
('proj-executive-insight-deck', 'data-storytelling'),
('proj-executive-insight-deck', 'data-visualization'),
('proj-executive-insight-deck', 'stakeholder-communication'),
('grp-choose-bi-tool', 'dashboard-design'),
('power-bi-path', 'power-bi'),
('power-bi-path', 'dashboard-design'),
('tableau-path', 'tableau'),
('tableau-path', 'dashboard-design'),
('looker-studio-path', 'looker-studio'),
('looker-studio-path', 'dashboard-design'),
('looker-awareness-path', 'looker'),
('looker-awareness-path', 'dashboard-design'),
('looker-awareness-path', 'data-governance'),
('dashboard-data-modeling', 'dashboard-data-modeling'),
('dashboard-data-modeling', 'dashboard-design'),
('dashboard-data-modeling', 'relational-databases'),
('dashboard-interactivity', 'dashboard-filters-and-interactivity'),
('dashboard-interactivity', 'dashboard-design'),
('dashboard-performance-basics', 'dashboard-performance'),
('dashboard-performance-basics', 'performance-analysis'),
('dashboard-performance-basics', 'dashboard-design'),
('dashboard-documentation', 'dashboard-documentation'),
('dashboard-documentation', 'data-governance'),
('dashboard-documentation', 'stakeholder-communication'),
('proj-bi-dashboard', 'dashboard-design'),
('proj-bi-dashboard', 'data-storytelling'),
('proj-bi-dashboard', 'business-metrics'),
('python-basics-for-analysts', 'python-basics-for-analysts'),
('python-basics-for-analysts', 'python'),
('notebook-workflows', 'notebook-workflows'),
('notebook-workflows', 'python'),
('numpy-for-analysis', 'numpy-for-analysis'),
('numpy-for-analysis', 'python'),
('pandas-data-analysis', 'pandas-data-analysis'),
('pandas-data-analysis', 'python'),
('pandas-data-analysis', 'data-cleaning'),
('pandas-data-analysis', 'exploratory-data-analysis'),
('python-visualization', 'python-visualization'),
('python-visualization', 'python'),
('python-visualization', 'data-visualization'),
('api-data-extraction', 'api-data-extraction'),
('api-data-extraction', 'python'),
('api-data-extraction', 'api'),
('automation-scripts', 'analysis-automation-scripts'),
('automation-scripts', 'python'),
('automation-scripts', 'git'),
('proj-python-eda-notebook', 'python'),
('proj-python-eda-notebook', 'exploratory-data-analysis'),
('proj-python-eda-notebook', 'data-visualization'),
('grp-domain-tracks', 'business-metrics'),
('product-analytics-track', 'product-analytics-track'),
('product-analytics-track', 'business-metrics'),
('product-analytics-track', 'a-b-testing'),
('marketing-analytics-track', 'marketing-analytics-track'),
('marketing-analytics-track', 'business-metrics'),
('marketing-analytics-track', 'data-visualization'),
('sales-analytics-track', 'sales-analytics-track'),
('sales-analytics-track', 'business-metrics'),
('sales-analytics-track', 'dashboard-design'),
('finance-analytics-track', 'finance-analytics-track'),
('finance-analytics-track', 'business-metrics'),
('finance-analytics-track', 'statistics'),
('operations-analytics-track', 'operations-analytics-track'),
('operations-analytics-track', 'business-metrics'),
('operations-analytics-track', 'data-quality'),
('cohort-analysis', 'cohort-analysis'),
('cohort-analysis', 'business-metrics'),
('cohort-analysis', 'sql'),
('funnel-analysis', 'funnel-analysis'),
('funnel-analysis', 'business-metrics'),
('funnel-analysis', 'data-visualization'),
('proj-domain-analytics-case-study', 'business-metrics'),
('proj-domain-analytics-case-study', 'data-storytelling'),
('metric-definitions', 'metric-definitions-and-semantic-consistency'),
('metric-definitions', 'business-metrics'),
('metric-definitions', 'data-governance'),
('data-catalogs-lineage', 'data-catalogs-and-lineage'),
('data-catalogs-lineage', 'data-catalogs'),
('data-catalogs-lineage', 'data-lineage'),
('privacy-sensitive-data', 'privacy-and-sensitive-data-handling'),
('privacy-sensitive-data', 'pii-and-sensitive-data'),
('privacy-sensitive-data', 'privacy-and-data-protection'),
('version-control-for-analysis', 'version-control-for-analysis'),
('version-control-for-analysis', 'git'),
('version-control-for-analysis', 'github'),
('analytics-documentation', 'analytics-documentation'),
('analytics-documentation', 'stakeholder-communication'),
('analytics-documentation', 'data-governance'),
('analytics-backlog-and-prioritization', 'analytics-backlog-and-prioritization'),
('analytics-backlog-and-prioritization', 'stakeholder-communication'),
('analytics-backlog-and-prioritization', 'business-metrics'),
('proj-governed-metric-dictionary', 'data-governance'),
('proj-governed-metric-dictionary', 'business-metrics'),
('proj-governed-metric-dictionary', 'stakeholder-communication'),
('forecasting-awareness', 'forecasting'),
('forecasting-awareness', 'statistics'),
('segmentation-and-clustering-awareness', 'segmentation-and-clustering'),
('segmentation-and-clustering-awareness', 'clustering'),
('segmentation-and-clustering-awareness', 'data-storytelling'),
('predictive-modeling-awareness', 'predictive-modeling'),
('predictive-modeling-awareness', 'model-evaluation'),
('predictive-modeling-awareness', 'error-analysis'),
('anomaly-detection-awareness', 'anomaly-detection'),
('anomaly-detection-awareness', 'statistics'),
('anomaly-detection-awareness', 'data-quality'),
('geospatial-analysis-awareness', 'geospatial-analysis'),
('geospatial-analysis-awareness', 'data-visualization'),
('geospatial-analysis-awareness', 'privacy-and-data-protection'),
('ai-assisted-analysis-awareness', 'ai-assisted-analysis'),
('ai-assisted-analysis-awareness', 'responsible-ai-and-governance'),
('ai-assisted-analysis-awareness', 'privacy-and-data-protection'),
('proj-advanced-analytics-brief', 'statistics'),
('proj-advanced-analytics-brief', 'data-storytelling'),
('portfolio-project-selection', 'portfolio-communication'),
('case-study-writing', 'technical-case-studies'),
('case-study-writing', 'data-storytelling'),
('sql-interview-practice', 'sql-interview-practice'),
('sql-interview-practice', 'sql'),
('sql-interview-practice', 'interview-readiness'),
('analytics-case-interviews', 'analytics-case-interviews'),
('analytics-case-interviews', 'stakeholder-communication'),
('analytics-case-interviews', 'business-metrics'),
('analytics-case-interviews', 'interview-readiness'),
('dashboard-portfolio-polish', 'portfolio-communication'),
('dashboard-portfolio-polish', 'dashboard-design'),
('dashboard-portfolio-polish', 'data-storytelling'),
('resume-and-project-positioning', 'resume-and-project-positioning'),
('resume-and-project-positioning', 'interview-readiness'),
('proj-data-analyst-capstone', 'sql'),
('proj-data-analyst-capstone', 'data-storytelling'),
('proj-data-analyst-capstone', 'dashboard-design'),
('proj-public-portfolio-package', 'github'),
('proj-public-portfolio-package', 'technical-case-studies'),
('proj-public-portfolio-package', 'interview-readiness'),
('data-source-types', 'data-source-types'),
('data-source-types', 'data-governance'),
('data-source-types', 'business-metrics'),
('analytical-deliverable-types', 'analytical-deliverable-types'),
('analytical-deliverable-types', 'stakeholder-communication'),
('analytical-deliverable-types', 'data-storytelling'),
('stakeholder-interview-questions', 'stakeholder-interview-questions'),
('stakeholder-interview-questions', 'stakeholder-communication'),
('stakeholder-interview-questions', 'business-metrics'),
('spreadsheet-data-validation', 'spreadsheet-data-validation-rules'),
('spreadsheet-data-validation', 'excel'),
('spreadsheet-data-validation', 'data-validation'),
('conditional-formatting-analysis', 'conditional-formatting-for-analysis'),
('conditional-formatting-analysis', 'excel'),
('conditional-formatting-analysis', 'data-visualization'),
('spreadsheet-lookup-patterns', 'spreadsheet-lookup-patterns'),
('spreadsheet-lookup-patterns', 'excel'),
('spreadsheet-lookup-patterns', 'data-cleaning'),
('sql-date-time-analysis', 'sql-date-and-time-analysis'),
('sql-date-time-analysis', 'sql'),
('sql-date-time-analysis', 'time-series-analysis'),
('cohort-retention-sql', 'cohort-and-retention-sql'),
('cohort-retention-sql', 'sql'),
('cohort-retention-sql', 'cohort-analysis'),
('cohort-retention-sql', 'retention-analysis'),
('analyst-dimensional-modeling', 'dimensional-modeling-for-analysts'),
('analyst-dimensional-modeling', 'data-modeling'),
('analyst-dimensional-modeling', 'relational-databases'),
('analyst-dimensional-modeling', 'business-metrics'),
('outliers-and-robust-summaries', 'outliers-and-robust-summaries'),
('outliers-and-robust-summaries', 'statistics'),
('outliers-and-robust-summaries', 'data-quality'),
('sample-size-and-power-basics', 'sample-size-and-power'),
('sample-size-and-power-basics', 'statistics'),
('sample-size-and-power-basics', 'a-b-testing'),
('practical-vs-statistical-significance', 'practical-vs-statistical-significance'),
('practical-vs-statistical-significance', 'statistics'),
('practical-vs-statistical-significance', 'business-metrics'),
('practical-vs-statistical-significance', 'data-storytelling'),
('text-cleaning-and-standardization', 'text-cleaning-and-standardization'),
('text-cleaning-and-standardization', 'data-cleaning'),
('text-cleaning-and-standardization', 'python'),
('schema-drift-and-source-changes', 'schema-drift-and-source-changes'),
('schema-drift-and-source-changes', 'data-validation'),
('schema-drift-and-source-changes', 'data-contracts'),
('data-dictionary-authoring', 'data-dictionary-authoring'),
('data-dictionary-authoring', 'data-governance'),
('data-dictionary-authoring', 'stakeholder-communication'),
('chart-accessibility', 'chart-accessibility'),
('chart-accessibility', 'accessibility'),
('chart-accessibility', 'data-visualization'),
('small-multiples-and-faceting', 'small-multiples-and-faceting'),
('small-multiples-and-faceting', 'data-visualization'),
('small-multiples-and-faceting', 'data-storytelling'),
('executive-summary-writing', 'executive-summary-writing'),
('executive-summary-writing', 'data-storytelling'),
('executive-summary-writing', 'stakeholder-communication'),
('semantic-models-and-metrics', 'semantic-models-and-reusable-metrics'),
('semantic-models-and-metrics', 'semantic-layer'),
('semantic-models-and-metrics', 'business-metrics'),
('semantic-models-and-metrics', 'data-modeling'),
('dax-and-calculated-measures', 'dax-and-calculated-measures'),
('dax-and-calculated-measures', 'power-bi'),
('dax-and-calculated-measures', 'business-metrics'),
('dashboard-performance-and-refresh', 'dashboard-performance-and-refresh'),
('dashboard-performance-and-refresh', 'dashboard-design'),
('dashboard-performance-and-refresh', 'performance-analysis'),
('dashboard-quality-assurance-topic', 'dashboard-quality-assurance'),
('dashboard-quality-assurance-topic', 'data-validation'),
('pandas-time-series-analysis', 'pandas-time-series-analysis'),
('pandas-time-series-analysis', 'python'),
('pandas-time-series-analysis', 'time-series-analysis'),
('python-report-automation', 'python-report-automation'),
('python-report-automation', 'python'),
('python-report-automation', 'exploratory-data-analysis'),
('public-data-api-extraction', 'public-data-api-extraction'),
('public-data-api-extraction', 'python'),
('public-data-api-extraction', 'api'),
('interactive-python-data-apps', 'interactive-python-data-apps'),
('interactive-python-data-apps', 'python'),
('interactive-python-data-apps', 'dashboard-design'),
('interactive-python-data-apps', 'data-visualization'),
('ecommerce-funnel-analysis', 'e-commerce-funnel-analysis'),
('ecommerce-funnel-analysis', 'funnel-analysis'),
('ecommerce-funnel-analysis', 'business-metrics'),
('customer-retention-analysis', 'customer-retention-analysis'),
('customer-retention-analysis', 'retention-analysis'),
('customer-retention-analysis', 'cohort-analysis'),
('revenue-and-pricing-analysis', 'revenue-and-pricing-analysis'),
('revenue-and-pricing-analysis', 'business-metrics'),
('revenue-and-pricing-analysis', 'statistics'),
('analytics-qa-review-process', 'analytics-qa-review-process'),
('analytics-qa-review-process', 'dashboard-quality-assurance'),
('analytics-qa-review-process', 'data-validation'),
('data-contracts-awareness', 'data-contracts'),
('data-contracts-awareness', 'data-governance'),
('dashboard-ownership-and-maintenance', 'dashboard-ownership-and-maintenance'),
('dashboard-ownership-and-maintenance', 'dashboard-design'),
('dashboard-ownership-and-maintenance', 'data-governance'),
('time-series-decomposition-awareness', 'time-series-decomposition'),
('time-series-decomposition-awareness', 'time-series-analysis'),
('time-series-decomposition-awareness', 'statistics'),
('causal-inference-caution', 'causal-inference-caution'),
('causal-inference-caution', 'statistics'),
('causal-inference-caution', 'data-storytelling'),
('survey-analysis-basics', 'survey-analysis'),
('survey-analysis-basics', 'statistics'),
('analyst-take-home-challenges', 'data-analyst-take-home-challenge-practice'),
('analyst-take-home-challenges', 'stakeholder-communication'),
('portfolio-dashboard-case-study', 'portfolio-communication'),
('portfolio-dashboard-case-study', 'dashboard-design'),
('portfolio-dashboard-case-study', 'technical-case-studies'),
('portfolio-dashboard-case-study', 'data-storytelling'),
('capstone-presentation-walkthrough', 'stakeholder-communication'),
('capstone-presentation-walkthrough', 'data-storytelling'),
('proj-spreadsheet-quality-audit', 'excel'),
('proj-spreadsheet-quality-audit', 'data-quality'),
('proj-spreadsheet-quality-audit', 'data-validation'),
('proj-sql-cohort-retention-analysis', 'sql'),
('proj-sql-cohort-retention-analysis', 'cohort-analysis'),
('proj-sql-cohort-retention-analysis', 'retention-analysis'),
('proj-dashboard-qa-checklist', 'dashboard-quality-assurance'),
('proj-dashboard-qa-checklist', 'dashboard-design'),
('proj-python-report-automation', 'python'),
('proj-python-report-automation', 'exploratory-data-analysis'),
('proj-ecommerce-funnel-case-study', 'funnel-analysis'),
('proj-ecommerce-funnel-case-study', 'business-metrics'),
('proj-ecommerce-funnel-case-study', 'data-storytelling'),
('proj-survey-analysis-report', 'survey-analysis'),
('proj-survey-analysis-report', 'statistics'),
('proj-survey-analysis-report', 'data-storytelling'),
('proj-public-data-api-mini-project', 'python'),
('proj-public-data-api-mini-project', 'api'),
('proj-portfolio-presentation-video', 'stakeholder-communication'),
('chk-statistics-review', 'data-visualization'),
('chk-data-cleaning-review', 'data-cleaning'),
('chk-visualization-review', 'data-visualization'),
('chk-dashboard-review', 'dashboard-design'),
('chk-python-analytics-review', 'python'),
('chk-domain-analytics-review', 'data-visualization'),
('chk-governance-review', 'data-visualization'),
('chk-advanced-awareness-review', 'data-visualization'),
('chk-final-readiness-review', 'data-visualization'),
('chk-final-readiness-review', 'sql'),
('chk-final-readiness-review', 'data-storytelling'),
('chk-final-readiness-review', 'portfolio-communication'),
('chk-final-readiness-review', 'career-planning')
ON CONFLICT (node_key, skill_slug) DO NOTHING;

INSERT INTO public.roadmap_node_skill (roadmap_node_id, skill_id)
SELECT DISTINCT
    rn.roadmap_node_id, resolved_skill.skill_id
FROM seed_node_skill sns
JOIN seed_roadmap_map m
    ON true
JOIN public.roadmap_node rn
    ON rn.roadmap_version_id = m.roadmap_version_id
   AND rn.slug = sns.node_key
LEFT JOIN seed_skill ss
    ON ss.slug = sns.skill_slug
JOIN LATERAL (
    SELECT s.skill_id
    FROM public.skill s
    WHERE s.slug = sns.skill_slug
       OR (ss.name IS NOT NULL AND s.name = ss.name)
    ORDER BY
        CASE WHEN s.slug = sns.skill_slug THEN 0 ELSE 1 END
    LIMIT 1
) resolved_skill ON true
ON CONFLICT (roadmap_node_id, skill_id) DO NOTHING;

WITH duplicate_node_skill AS (
    SELECT
        rns.roadmap_node_skill_id,
        row_number() OVER (
            PARTITION BY rns.roadmap_node_id, rns.skill_id
            ORDER BY rns.roadmap_node_skill_id
        ) AS duplicate_rank
    FROM public.roadmap_node_skill rns
    JOIN public.roadmap_node rn ON rn.roadmap_node_id = rns.roadmap_node_id
    JOIN seed_roadmap_map m ON m.roadmap_version_id = rn.roadmap_version_id
)
DELETE FROM public.roadmap_node_skill rns
USING duplicate_node_skill dns
WHERE rns.roadmap_node_skill_id = dns.roadmap_node_skill_id
  AND dns.duplicate_rank > 1;

-- Node-resource mappings.
DROP TABLE IF EXISTS seed_node_resource;
CREATE TEMP TABLE seed_node_resource (
    node_key text NOT NULL,
    resource_key text NOT NULL,
    order_index int NOT NULL,
    PRIMARY KEY (node_key, resource_key)
) ON COMMIT DROP;
INSERT INTO seed_node_resource (node_key, resource_key, order_index)
SELECT NULL::text, NULL::text, NULL::int WHERE false
ON CONFLICT (node_key, resource_key) DO NOTHING;

WITH resolved_node_resource AS (
    SELECT DISTINCT ON (rn.roadmap_node_id, lower(lr.url))
        rn.roadmap_node_id,
        lr.learning_resource_id
    FROM seed_node_resource snr
    JOIN seed_node sn ON sn.node_key = snr.node_key
    JOIN seed_roadmap_map m ON true
    JOIN public.roadmap_node rn
        ON rn.roadmap_version_id = m.roadmap_version_id
       AND rn.slug = sn.node_key
    JOIN seed_resource sr ON sr.resource_key = snr.resource_key
    JOIN LATERAL (
        SELECT lr.learning_resource_id, lr.url
        FROM public.learning_resource lr
        WHERE lr.url = sr.url
        ORDER BY lr.learning_resource_id
        LIMIT 1
    ) lr ON true
    ORDER BY rn.roadmap_node_id, lower(lr.url), snr.order_index
)
INSERT INTO public.roadmap_node_resource (roadmap_node_id, learning_resource_id)
SELECT roadmap_node_id, learning_resource_id
FROM resolved_node_resource
ON CONFLICT (roadmap_node_id, learning_resource_id) DO NOTHING;

WITH resolved_resource_skill AS (
    SELECT DISTINCT
        lr.learning_resource_id,
        s.skill_id
    FROM seed_node_resource snr
    JOIN seed_node_skill sns ON sns.node_key = snr.node_key
    JOIN seed_resource sr ON sr.resource_key = snr.resource_key
    JOIN LATERAL (
        SELECT lr.learning_resource_id
        FROM public.learning_resource lr
        WHERE lr.url = sr.url
        ORDER BY lr.learning_resource_id
        LIMIT 1
    ) lr ON true
    JOIN public.skill s ON s.slug = sns.skill_slug
)
INSERT INTO public.learning_resource_skill (learning_resource_id, skill_id)
SELECT learning_resource_id, skill_id
FROM resolved_resource_skill
ON CONFLICT (learning_resource_id, skill_id) DO NOTHING;

WITH duplicate_node_resource AS (
    SELECT
        rnr.roadmap_node_resource_id,
        row_number() OVER (
            PARTITION BY rnr.roadmap_node_id, lower(lr.url)
            ORDER BY rnr.roadmap_node_resource_id
        ) AS duplicate_rank
    FROM public.roadmap_node_resource rnr
    JOIN public.roadmap_node rn ON rn.roadmap_node_id = rnr.roadmap_node_id
    JOIN public.learning_resource lr ON lr.learning_resource_id = rnr.learning_resource_id
    JOIN seed_roadmap_map m ON m.roadmap_version_id = rn.roadmap_version_id
)
DELETE FROM public.roadmap_node_resource rnr
USING duplicate_node_resource dnr
WHERE rnr.roadmap_node_resource_id = dnr.roadmap_node_resource_id
  AND dnr.duplicate_rank > 1;

WITH duplicate_resource_skill AS (
    SELECT
        lrs.learning_resource_skill_id,
        row_number() OVER (
            PARTITION BY lower(lr.url), lrs.skill_id
            ORDER BY lrs.learning_resource_skill_id
        ) AS duplicate_rank
    FROM public.learning_resource_skill lrs
    JOIN public.learning_resource lr ON lr.learning_resource_id = lrs.learning_resource_id
    JOIN seed_resource sr ON sr.url = lr.url
)
DELETE FROM public.learning_resource_skill lrs
USING duplicate_resource_skill drs
WHERE lrs.learning_resource_skill_id = drs.learning_resource_skill_id
  AND drs.duplicate_rank > 1;

-- Roadmap edges.
DROP TABLE IF EXISTS seed_edge;
CREATE TEMP TABLE seed_edge (from_key text NOT NULL, to_key text NOT NULL, edge_type text NOT NULL, dependency_type text NOT NULL, condition jsonb NOT NULL) ON COMMIT DROP;
INSERT INTO seed_edge VALUES
('ph-da-foundations', 'chk-da-foundations-review', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('ph-da-foundations', 'proj-analytics-question-brief', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('chk-da-foundations-review', 'proj-analytics-question-brief', 'unlock', 'required', '{"rule": "gate_unlock"}'::jsonb),
('ph-spreadsheets', 'chk-spreadsheet-review', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('ph-spreadsheets', 'proj-excel-sales-analysis', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('chk-spreadsheet-review', 'proj-excel-sales-analysis', 'unlock', 'required', '{"rule": "gate_unlock"}'::jsonb),
('ph-sql-databases', 'chk-sql-review', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('ph-sql-databases', 'proj-sql-business-analysis', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('chk-sql-review', 'proj-sql-business-analysis', 'unlock', 'required', '{"rule": "gate_unlock"}'::jsonb),
('ph-statistics', 'chk-statistics-review', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('ph-statistics', 'proj-statistical-insight-report', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('chk-statistics-review', 'proj-statistical-insight-report', 'unlock', 'required', '{"rule": "gate_unlock"}'::jsonb),
('ph-data-cleaning-preparation', 'chk-data-cleaning-review', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('ph-data-cleaning-preparation', 'proj-cleaning-pipeline-report', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('chk-data-cleaning-review', 'proj-cleaning-pipeline-report', 'unlock', 'required', '{"rule": "gate_unlock"}'::jsonb),
('ph-visualization-storytelling', 'chk-visualization-review', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('ph-visualization-storytelling', 'proj-executive-insight-deck', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('chk-visualization-review', 'proj-executive-insight-deck', 'unlock', 'required', '{"rule": "gate_unlock"}'::jsonb),
('ph-bi-tools', 'grp-choose-bi-tool', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('grp-choose-bi-tool', 'power-bi-path', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-choose-bi-tool', 'tableau-path', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-choose-bi-tool', 'looker-studio-path', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-choose-bi-tool', 'looker-awareness-path', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('ph-bi-tools', 'chk-dashboard-review', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('ph-bi-tools', 'proj-bi-dashboard', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('chk-dashboard-review', 'proj-bi-dashboard', 'unlock', 'required', '{"rule": "gate_unlock"}'::jsonb),
('ph-python-analytics', 'chk-python-analytics-review', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('ph-python-analytics', 'proj-python-eda-notebook', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('chk-python-analytics-review', 'proj-python-eda-notebook', 'unlock', 'required', '{"rule": "gate_unlock"}'::jsonb),
('ph-domain-analytics', 'grp-domain-tracks', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('grp-domain-tracks', 'product-analytics-track', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-domain-tracks', 'marketing-analytics-track', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-domain-tracks', 'sales-analytics-track', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-domain-tracks', 'finance-analytics-track', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-domain-tracks', 'operations-analytics-track', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('ph-domain-analytics', 'chk-domain-analytics-review', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('ph-domain-analytics', 'proj-domain-analytics-case-study', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('chk-domain-analytics-review', 'proj-domain-analytics-case-study', 'unlock', 'required', '{"rule": "gate_unlock"}'::jsonb),
('ph-governance-collaboration', 'chk-governance-review', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('ph-governance-collaboration', 'proj-governed-metric-dictionary', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('chk-governance-review', 'proj-governed-metric-dictionary', 'unlock', 'required', '{"rule": "gate_unlock"}'::jsonb),
('ph-advanced-analytics-awareness', 'chk-advanced-awareness-review', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('ph-advanced-analytics-awareness', 'proj-advanced-analytics-brief', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('ph-portfolio-capstone', 'chk-final-readiness-review', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('ph-portfolio-capstone', 'proj-data-analyst-capstone', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('chk-final-readiness-review', 'proj-data-analyst-capstone', 'unlock', 'required', '{"rule": "gate_unlock"}'::jsonb),
('ph-portfolio-capstone', 'proj-public-portfolio-package', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('chk-final-readiness-review', 'proj-public-portfolio-package', 'unlock', 'required', '{"rule": "gate_unlock"}'::jsonb),
('ph-da-foundations', 'ph-spreadsheets', 'sequence', 'required', '{"rule": "source_completed"}'::jsonb),
('ph-spreadsheets', 'ph-sql-databases', 'sequence', 'required', '{"rule": "source_completed"}'::jsonb),
('ph-sql-databases', 'ph-statistics', 'sequence', 'required', '{"rule": "source_completed"}'::jsonb),
('ph-statistics', 'ph-data-cleaning-preparation', 'sequence', 'required', '{"rule": "source_completed"}'::jsonb),
('ph-data-cleaning-preparation', 'ph-visualization-storytelling', 'sequence', 'required', '{"rule": "source_completed"}'::jsonb),
('ph-visualization-storytelling', 'ph-bi-tools', 'sequence', 'required', '{"rule": "source_completed"}'::jsonb),
('ph-bi-tools', 'ph-python-analytics', 'sequence', 'required', '{"rule": "source_completed"}'::jsonb),
('ph-python-analytics', 'ph-domain-analytics', 'sequence', 'required', '{"rule": "source_completed"}'::jsonb),
('ph-domain-analytics', 'ph-governance-collaboration', 'sequence', 'required', '{"rule": "source_completed"}'::jsonb),
('ph-governance-collaboration', 'ph-advanced-analytics-awareness', 'sequence', 'required', '{"rule": "source_completed"}'::jsonb),
('ph-advanced-analytics-awareness', 'ph-portfolio-capstone', 'sequence', 'required', '{"rule": "source_completed"}'::jsonb),
('joins-and-relationships', 'sql-data-quality-checks', 'dependency', 'required', '{"rule": "source_completed"}'::jsonb),
('sql-data-quality-checks', 'proj-sql-business-analysis', 'dependency', 'required', '{"rule": "source_completed"}'::jsonb),
('descriptive-statistics-analysis', 'hypothesis-testing-basics', 'dependency', 'required', '{"rule": "source_completed"}'::jsonb),
('data-profiling', 'data-validation-rules', 'dependency', 'required', '{"rule": "source_completed"}'::jsonb),
('chart-selection', 'dashboard-purpose-and-layout', 'dependency', 'required', '{"rule": "source_completed"}'::jsonb),
('dashboard-purpose-and-layout', 'proj-bi-dashboard', 'dependency', 'required', '{"rule": "source_completed"}'::jsonb),
('pandas-data-analysis', 'proj-python-eda-notebook', 'dependency', 'required', '{"rule": "source_completed"}'::jsonb),
('metric-definitions', 'proj-governed-metric-dictionary', 'dependency', 'required', '{"rule": "source_completed"}'::jsonb),
('case-study-writing', 'proj-data-analyst-capstone', 'dependency', 'required', '{"rule": "source_completed"}'::jsonb),
('ph-da-foundations', 'grp-da-role-context', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('grp-da-role-context', 'data-analyst-role', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-da-role-context', 'analytics-lifecycle', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-da-role-context', 'requirements-intake', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('ph-da-foundations', 'grp-da-thinking-ethics', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('grp-da-thinking-ethics', 'business-questions-and-kpis', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-da-thinking-ethics', 'data-literacy-and-ethics', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-da-thinking-ethics', 'analytical-thinking-basics', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('ph-spreadsheets', 'grp-spreadsheet-foundations', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('grp-spreadsheet-foundations', 'spreadsheet-structure', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-spreadsheet-foundations', 'excel-formulas-functions', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-spreadsheet-foundations', 'lookup-and-reference-functions', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('ph-spreadsheets', 'grp-spreadsheet-analysis-reporting', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('grp-spreadsheet-analysis-reporting', 'pivot-tables', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-spreadsheet-analysis-reporting', 'spreadsheet-cleaning', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-spreadsheet-analysis-reporting', 'power-query-basics', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-spreadsheet-analysis-reporting', 'spreadsheet-charts', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('ph-sql-databases', 'grp-sql-query-foundations', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('grp-sql-query-foundations', 'relational-data-models', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-sql-query-foundations', 'select-filter-sort', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-sql-query-foundations', 'joins-and-relationships', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-sql-query-foundations', 'aggregation-and-grouping', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('ph-sql-databases', 'grp-sql-analytical-techniques', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('grp-sql-analytical-techniques', 'ctes-and-subqueries', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-sql-analytical-techniques', 'window-functions', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-sql-analytical-techniques', 'sql-data-quality-checks', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-sql-analytical-techniques', 'query-debugging-performance', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('ph-statistics', 'grp-statistics-foundations', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('grp-statistics-foundations', 'descriptive-statistics-analysis', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-statistics-foundations', 'distributions-and-sampling', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-statistics-foundations', 'confidence-intervals', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-statistics-foundations', 'hypothesis-testing-basics', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('ph-statistics', 'grp-experiment-relationship-analysis', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('grp-experiment-relationship-analysis', 'correlation-causation', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-experiment-relationship-analysis', 'ab-test-interpretation', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-experiment-relationship-analysis', 'regression-awareness', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('ph-data-cleaning-preparation', 'grp-data-quality-issues', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('grp-data-quality-issues', 'data-profiling', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-data-quality-issues', 'missing-data-handling', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-data-quality-issues', 'duplicates-and-entity-resolution', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-data-quality-issues', 'outliers-and-anomalies', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('ph-data-cleaning-preparation', 'grp-reproducible-preparation', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('grp-reproducible-preparation', 'data-type-and-format-standardization', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-reproducible-preparation', 'data-validation-rules', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-reproducible-preparation', 'reproducible-cleaning-workflows', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('ph-visualization-storytelling', 'grp-visual-design-foundations', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('grp-visual-design-foundations', 'chart-selection', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-visual-design-foundations', 'visual-design-principles', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-visual-design-foundations', 'accessibility-in-data-visualization', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-visual-design-foundations', 'misleading-visuals', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('ph-visualization-storytelling', 'grp-dashboard-storytelling-delivery', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('grp-dashboard-storytelling-delivery', 'dashboard-purpose-and-layout', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-dashboard-storytelling-delivery', 'insight-narrative', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-dashboard-storytelling-delivery', 'presentation-delivery', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('ph-bi-tools', 'grp-dashboard-delivery-workflow', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('grp-dashboard-delivery-workflow', 'dashboard-data-modeling', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-dashboard-delivery-workflow', 'dashboard-interactivity', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-dashboard-delivery-workflow', 'dashboard-performance-basics', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-dashboard-delivery-workflow', 'dashboard-documentation', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('ph-python-analytics', 'grp-python-analysis-foundations', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('grp-python-analysis-foundations', 'python-basics-for-analysts', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-python-analysis-foundations', 'notebook-workflows', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-python-analysis-foundations', 'numpy-for-analysis', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('ph-python-analytics', 'grp-python-data-workflows', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('grp-python-data-workflows', 'pandas-data-analysis', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-python-data-workflows', 'python-visualization', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-python-data-workflows', 'api-data-extraction', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-python-data-workflows', 'automation-scripts', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('ph-domain-analytics', 'grp-product-customer-analytics', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('grp-product-customer-analytics', 'cohort-analysis', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-product-customer-analytics', 'funnel-analysis', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('ph-governance-collaboration', 'grp-analytics-governance-foundations', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('grp-analytics-governance-foundations', 'metric-definitions', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-analytics-governance-foundations', 'data-catalogs-lineage', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-analytics-governance-foundations', 'privacy-sensitive-data', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('ph-governance-collaboration', 'grp-analytics-collaboration-delivery', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('grp-analytics-collaboration-delivery', 'version-control-for-analysis', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-analytics-collaboration-delivery', 'analytics-documentation', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-analytics-collaboration-delivery', 'analytics-backlog-and-prioritization', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('ph-advanced-analytics-awareness', 'grp-predictive-statistical-awareness', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('grp-predictive-statistical-awareness', 'forecasting-awareness', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-predictive-statistical-awareness', 'segmentation-and-clustering-awareness', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-predictive-statistical-awareness', 'predictive-modeling-awareness', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-predictive-statistical-awareness', 'anomaly-detection-awareness', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('ph-advanced-analytics-awareness', 'grp-specialized-ai-analytics-awareness', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('grp-specialized-ai-analytics-awareness', 'geospatial-analysis-awareness', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-specialized-ai-analytics-awareness', 'ai-assisted-analysis-awareness', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('ph-portfolio-capstone', 'grp-portfolio-packaging', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('grp-portfolio-packaging', 'portfolio-project-selection', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-portfolio-packaging', 'case-study-writing', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-portfolio-packaging', 'dashboard-portfolio-polish', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-portfolio-packaging', 'resume-and-project-positioning', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('ph-portfolio-capstone', 'grp-analytics-interview-readiness', 'contains', 'required', '{"rule": "parent_contains_child"}'::jsonb),
('grp-analytics-interview-readiness', 'sql-interview-practice', 'choice', 'required', '{"rule": "selection_child"}'::jsonb),
('grp-analytics-interview-readiness', 'analytics-case-interviews', 'choice', 'required', '{"rule": "selection_child"}'::jsonb)
;

INSERT INTO public.roadmap_edge (roadmap_version_id, from_node_id, to_node_id, edge_type, dependency_type, condition)
SELECT DISTINCT ON (m.roadmap_version_id, source.roadmap_node_id, target.roadmap_node_id, se.edge_type)
       m.roadmap_version_id, source.roadmap_node_id, target.roadmap_node_id, se.edge_type, se.dependency_type, se.condition
FROM seed_edge se
CROSS JOIN seed_roadmap_map m
JOIN public.roadmap_node source ON source.roadmap_version_id = m.roadmap_version_id AND source.slug = se.from_key
JOIN public.roadmap_node target ON target.roadmap_version_id = m.roadmap_version_id AND target.slug = se.to_key
ORDER BY m.roadmap_version_id, source.roadmap_node_id, target.roadmap_node_id, se.edge_type, se.dependency_type
ON CONFLICT (roadmap_version_id, from_node_id, to_node_id, edge_type) DO UPDATE SET dependency_type = EXCLUDED.dependency_type,
    condition = EXCLUDED.condition;

COMMIT;


-- ============================================================
-- Data Analyst Roadmap curated expansion
-- Adds deeper but still relevant analyst topics, optional projects,
-- targeted resources, and safer project requirement balance.
-- ============================================================

BEGIN;
CREATE EXTENSION IF NOT EXISTS pgcrypto;

DROP TABLE IF EXISTS seed_edge;
DROP TABLE IF EXISTS seed_node_resource;
DROP TABLE IF EXISTS seed_node_skill;
DROP TABLE IF EXISTS seed_node;
DROP TABLE IF EXISTS seed_resource;
DROP TABLE IF EXISTS seed_skill;
DROP TABLE IF EXISTS seed_roadmap_map;

CREATE TEMP TABLE seed_roadmap_map AS
SELECT r.roadmap_id, rv.roadmap_version_id
FROM public.roadmap r
JOIN public.roadmap_version rv ON rv.roadmap_id = r.roadmap_id
WHERE r.title = 'Data Analyst Roadmap'
ORDER BY rv.created_at DESC
LIMIT 1;

UPDATE public.roadmap_version rv
SET title = 'Data Analyst Roadmap v1',
    description = 'A deeper but still curated data analyst roadmap with strong coverage of spreadsheets, SQL, statistics, data cleaning, dashboards, Python analytics, domain analytics, governance, advanced analytics awareness, and portfolio-ready projects.',
    estimated_total_hours = 410
FROM seed_roadmap_map m
WHERE rv.roadmap_version_id = m.roadmap_version_id;

-- Rebalance required projects: keep major milestones required, make practice-style projects optional.
UPDATE public.roadmap_node rn
SET is_required = false,
    title = regexp_replace(title, '^Required', 'Optional'),
    metadata = jsonb_set(COALESCE(metadata, '{}'::jsonb), '{projectType}', '"practice"'::jsonb, true)
FROM seed_roadmap_map m
WHERE rn.roadmap_version_id = m.roadmap_version_id
  AND rn.slug IN (
      'proj-statistical-insight-report',
      'proj-cleaning-pipeline-report',
      'proj-executive-insight-deck',
      'proj-domain-analytics-case-study',
      'proj-governed-metric-dictionary'
  );

UPDATE public.roadmap_edge e
SET dependency_type = 'optional',
    condition = jsonb_set(COALESCE(e.condition, '{}'::jsonb), '{requirementRebalance}', 'true'::jsonb, true)
FROM seed_roadmap_map m
JOIN public.roadmap_node target ON target.roadmap_version_id = m.roadmap_version_id
WHERE e.roadmap_version_id = m.roadmap_version_id
  AND target.roadmap_node_id = e.to_node_id
  AND target.slug IN (
      'proj-statistical-insight-report',
      'proj-cleaning-pipeline-report',
      'proj-executive-insight-deck',
      'proj-domain-analytics-case-study',
      'proj-governed-metric-dictionary'
  );

DROP TABLE IF EXISTS seed_skill;
CREATE TEMP TABLE seed_skill (slug text PRIMARY KEY, name text NOT NULL, category text NOT NULL, description text NOT NULL) ON COMMIT DROP;
-- No roadmap-specific seed_skill rows remain after skill standardization.

INSERT INTO public.skill (name, slug, category, description, is_active)
SELECT svs.name, svs.slug, svs.category, svs.description, true
FROM seed_skill svs
WHERE NOT EXISTS (SELECT 1 FROM public.skill s WHERE s.slug = svs.slug OR s.name = svs.name);

DROP TABLE IF EXISTS seed_resource;
CREATE TEMP TABLE seed_resource (resource_key text PRIMARY KEY, title text NOT NULL, url text NOT NULL, resource_type text NOT NULL, description text NOT NULL, provider text, difficulty_level text) ON COMMIT DROP;
INSERT INTO seed_resource VALUES
('google-data-analytics-certificate', 'Google Data Analytics Professional Certificate', 'https://www.coursera.org/professional-certificates/google-data-analytics', 'course', 'Structured beginner-friendly certificate covering data analysis process, spreadsheets, SQL, visualization, and capstone work.', 'Coursera / Google', 'beginner'),
('ibm-data-analyst-certificate', 'IBM Data Analyst Professional Certificate', 'https://www.coursera.org/professional-certificates/ibm-data-analyst', 'course', 'Professional certificate covering spreadsheets, SQL, Python, visualization, and analyst portfolio projects.', 'Coursera / IBM', 'beginner'),
('coursera-wharton-business-analytics', 'Business Analytics Specialization', 'https://www.coursera.org/specializations/business-analytics', 'course', 'Business analytics course sequence focused on decision-making with data.', 'Coursera / Wharton', 'intermediate'),
('excel-data-validation', 'Apply Data Validation to Cells', 'https://support.microsoft.com/en-us/office/apply-data-validation-to-cells-29fecbcc-d1b9-42c1-9d76-eff3ce5f7249', 'documentation', 'Official guide to spreadsheet data validation rules and controlled inputs.', 'Microsoft Support', 'beginner'),
('great-expectations-docs', 'Great Expectations Documentation', 'https://docs.greatexpectations.io/docs/', 'documentation', 'Data validation documentation for expectations, checks, and quality rules.', 'Great Expectations', 'intermediate'),
('dbt-tests', 'dbt Data Tests', 'https://docs.getdbt.com/docs/build/data-tests', 'documentation', 'Official dbt documentation for validating transformed analytical datasets.', 'dbt Labs', 'intermediate'),
('excel-conditional-formatting', 'Use Conditional Formatting to Highlight Information', 'https://support.microsoft.com/en-us/office/use-conditional-formatting-to-highlight-information-fed60dfa-1d3f-4e13-9ecb-f1951ff89d7f', 'documentation', 'Official guide to conditional formatting for spreadsheet review and reporting.', 'Microsoft Support', 'beginner'),
('microsoft-excel-help', 'Microsoft Excel Help and Learning', 'https://support.microsoft.com/en-us/excel', 'documentation', 'Official Microsoft Excel help covering formulas, tables, pivots, charts, and workbook workflows.', 'Microsoft Support', 'beginner'),
('excel-formulas-overview', 'Overview of Formulas in Excel', 'https://support.microsoft.com/en-us/office/overview-of-formulas-in-excel-ecfdc708-9162-49e8-b993-c311f47ca173', 'documentation', 'Official guide to Excel formulas and calculation concepts.', 'Microsoft Support', 'beginner'),
('excel-xlookup', 'XLOOKUP Function', 'https://support.microsoft.com/en-us/office/xlookup-function-b7fd680e-6d10-43e6-84f9-88eae8bf5929', 'documentation', 'Official Microsoft reference for XLOOKUP, useful for analyst lookup workflows.', 'Microsoft Support', 'beginner'),
('cur-pandas-merge', 'Merge, join, concatenate and compare - pandas', 'https://pandas.pydata.org/docs/user_guide/merging.html', 'documentation', 'pandas guide for joining and combining datasets.', 'pandas', 'intermediate'),
('postgres-datetime-functions', 'PostgreSQL Date/Time Functions and Operators', 'https://www.postgresql.org/docs/current/functions-datetime.html', 'documentation', 'Official PostgreSQL reference for date and time analysis in SQL.', 'PostgreSQL', 'intermediate'),
('postgres-window-functions', 'PostgreSQL Window Functions', 'https://www.postgresql.org/docs/current/tutorial-window.html', 'documentation', 'Official PostgreSQL tutorial for analytical window functions.', 'PostgreSQL', 'intermediate'),
('kaggle-advanced-sql', 'Kaggle Learn: Advanced SQL', 'https://www.kaggle.com/learn/advanced-sql', 'course', 'Hands-on SQL course covering joins, unions, analytic functions, and nested queries.', 'Kaggle', 'intermediate'),
('ibm-data-governance', 'What is Data Governance?', 'https://www.ibm.com/topics/data-governance', 'article', 'Conceptual article explaining data governance roles, policies, and value.', 'IBM', 'beginner'),
('data-dbt-intro', 'What is dbt?', 'https://docs.getdbt.com/docs/introduction', 'documentation', 'dbt documentation explaining analytics engineering and transformation workflows.', 'dbt Labs', 'beginner'),
('dbt-docs', 'dbt Documentation', 'https://docs.getdbt.com/docs/introduction', 'documentation', 'Official dbt documentation for analytics engineering concepts, transformations, tests, and documentation.', 'dbt Labs', 'intermediate'),
('khan-statistics', 'Khan Academy Statistics and Probability', 'https://www.khanacademy.org/math/statistics-probability', 'course', 'Beginner-friendly statistics and probability lessons for analysts.', 'Khan Academy', 'beginner'),
('data-khan-statistics', 'Khan Academy Statistics and Probability', 'https://www.khanacademy.org/math/statistics-probability', 'course', 'Beginner-friendly course for probability, sampling, distributions, and statistics.', 'Khan Academy', 'beginner'),
('cur-khan-statistics', 'Khan Academy Statistics and Probability', 'https://www.khanacademy.org/math/statistics-probability', 'course', 'Statistics and probability course for foundational concepts.', 'Khan Academy', 'beginner'),
('google-ab-testing-course', 'Google Analytics Academy', 'https://analytics.google.com/analytics/academy/', 'course', 'Google Analytics learning resources useful for product and marketing analytics context.', 'Google', 'beginner'),
('openintro-statistics', 'OpenIntro Statistics', 'https://www.openintro.org/book/os/', 'book', 'Free statistics textbook with practical examples and exercises.', 'OpenIntro', 'beginner'),
('statistics-how-to-power', 'Statistical Power', 'https://www.statisticshowto.com/probability-and-statistics/statistics-definitions/statistical-power/', 'article', 'Focused explanation of statistical power for experiment interpretation.', 'Statistics How To', 'intermediate'),
('sqlbolt', 'SQLBolt Interactive Lessons', 'https://sqlbolt.com/', 'practice', 'Interactive SQL practice lessons for SELECT, filtering, joins, aggregates, and table operations.', 'SQLBolt', 'beginner'),
('data-sqlbolt', 'SQLBolt Interactive Lessons', 'https://sqlbolt.com/', 'practice', 'Interactive SQL practice lessons for SELECT, filtering, joins, aggregates, and table operations.', 'SQLBolt', 'beginner'),
('mode-sql-tutorial', 'Mode SQL Tutorial', 'https://mode.com/sql-tutorial/', 'course', 'SQL tutorial focused on analytical querying and business data analysis.', 'Mode', 'beginner'),
('w3c-accessibility-intro', 'Introduction to Web Accessibility', 'https://www.w3.org/WAI/fundamentals/accessibility-intro/', 'article', 'W3C introduction to accessibility principles that also apply to shared dashboards and reports.', 'W3C WAI', 'beginner'),
('wcag-understanding', 'Understanding WCAG 2.2', 'https://www.w3.org/WAI/WCAG22/Understanding/', 'documentation', 'Detailed accessibility guidance useful for dashboard readability and accessible reporting.', 'W3C WAI', 'intermediate'),
('cur-mdn-accessibility', 'Accessibility - MDN', 'https://developer.mozilla.org/en-US/docs/Learn_web_development/Core/Accessibility', 'documentation', 'MDN guide for accessible web content.', 'MDN Web Docs', 'beginner'),
('storytelling-with-data-blog', 'Storytelling with Data Blog', 'https://www.storytellingwithdata.com/blog', 'article', 'Practical articles on communicating data clearly through charts and narrative.', 'Storytelling with Data', 'beginner'),
('data-to-viz', 'From Data to Viz', 'https://www.data-to-viz.com/', 'article', 'Practical guide for choosing charts based on data shape and analytical purpose.', 'From Data to Viz', 'beginner'),
('data-kaggle-visualization', 'Kaggle Learn: Data Visualization', 'https://www.kaggle.com/learn/data-visualization', 'course', 'Hands-on Kaggle course for practical data visualization.', 'Kaggle Learn', 'beginner'),
('power-bi-docs', 'Power BI Documentation', 'https://learn.microsoft.com/en-us/power-bi/', 'documentation', 'Official Power BI documentation for reports, semantic models, dashboards, and administration.', 'Microsoft Learn', 'beginner'),
('power-bi-training', 'Power BI Training', 'https://learn.microsoft.com/en-us/training/powerplatform/power-bi', 'course', 'Microsoft Learn modules for Power BI concepts, reports, modeling, and dashboard development.', 'Microsoft Learn', 'beginner'),
('power-bi-dax', 'DAX Overview', 'https://learn.microsoft.com/en-us/dax/dax-overview', 'documentation', 'Official Microsoft overview of DAX formulas used in Power BI models.', 'Microsoft Learn', 'intermediate'),
('tableau-performance-recording', 'Create a Performance Recording', 'https://help.tableau.com/current/pro/desktop/en-us/perf_record_create_desktop.htm', 'documentation', 'Official Tableau documentation for diagnosing dashboard performance.', 'Tableau', 'advanced'),
('power-bi-guidance', 'Power BI Guidance Documentation', 'https://learn.microsoft.com/en-us/power-bi/guidance/', 'documentation', 'Official Power BI implementation, modeling, and performance guidance.', 'Microsoft Learn', 'intermediate'),
('cur-dbt-tests', 'Data tests - dbt', 'https://docs.getdbt.com/docs/build/data-tests', 'documentation', 'dbt documentation for data tests and quality checks.', 'dbt Labs', 'intermediate'),
('pandas-timeseries', 'pandas Time Series / Date Functionality', 'https://pandas.pydata.org/docs/user_guide/timeseries.html', 'documentation', 'Official pandas guide for time-indexed data and date operations.', 'pandas', 'intermediate'),
('statsmodels-time-series', 'statsmodels Time Series Analysis', 'https://www.statsmodels.org/stable/tsa.html', 'documentation', 'Official statsmodels time series analysis documentation.', 'statsmodels', 'advanced'),
('prophet-docs', 'Prophet Documentation', 'https://facebook.github.io/prophet/', 'documentation', 'Forecasting documentation for practical analyst-level time series modeling.', 'Prophet', 'intermediate'),
('census-data-api', 'Census Data API User Guide', 'https://www.census.gov/data/developers/guidance/api-user-guide.html', 'documentation', 'Official Census API guide useful for public-data analysis practice.', 'U.S. Census Bureau', 'intermediate'),
('world-bank-api', 'World Bank API Documentation', 'https://datahelpdesk.worldbank.org/knowledgebase/topics/125589-developer-information', 'documentation', 'Official World Bank developer documentation for public economic datasets.', 'World Bank', 'intermediate'),
('python-docs', 'Python Tutorial', 'https://docs.python.org/3/tutorial/index.html', 'documentation', 'Official Python tutorial covering language basics, data structures, modules, and exceptions.', 'Python', 'beginner'),
('streamlit-docs', 'Streamlit Documentation', 'https://docs.streamlit.io/', 'documentation', 'Official documentation for building small data apps and portfolio demos.', 'Streamlit', 'intermediate'),
('plotly-python', 'Plotly Python Documentation', 'https://plotly.com/python/', 'documentation', 'Official Plotly documentation for interactive Python charts and dashboards.', 'Plotly', 'intermediate'),
('google-analytics-events', 'Google Analytics Events', 'https://support.google.com/analytics/answer/9322688', 'documentation', 'Official GA4 guide for understanding event-based product and marketing data.', 'Google Help', 'intermediate'),
('shopify-analytics', 'Shopify Analytics', 'https://help.shopify.com/en/manual/reports-and-analytics/shopify-reports', 'documentation', 'Official Shopify guide for e-commerce reporting and analytics concepts.', 'Shopify Help Center', 'beginner'),
('stripe-analytics-docs', 'Stripe Sigma Documentation', 'https://docs.stripe.com/stripe-data/query-billing-data', 'documentation', 'Official Stripe documentation for querying billing and revenue data examples.', 'Stripe Docs', 'intermediate'),
('scipy-stats', 'SciPy Statistical Functions', 'https://docs.scipy.org/doc/scipy/reference/stats.html', 'documentation', 'Official SciPy statistical functions reference for Python analysis.', 'SciPy', 'intermediate'),
('statsmodels-docs', 'statsmodels Documentation', 'https://www.statsmodels.org/stable/index.html', 'documentation', 'Python statistical modeling documentation for regression, tests, and time series analysis.', 'statsmodels', 'intermediate'),
('nngroup-dashboards', 'Dashboards: Making Charts and Graphs Easier to Understand', 'https://www.nngroup.com/articles/dashboards/', 'article', 'User experience article on making dashboards easier to interpret.', 'Nielsen Norman Group', 'intermediate'),
('tableau-help', 'Tableau Desktop and Web Authoring Help', 'https://help.tableau.com/current/pro/desktop/en-us/', 'documentation', 'Official Tableau authoring documentation for charts, dashboards, calculations, and publishing.', 'Tableau', 'intermediate'),
('tableau-visual-best-practices', 'Tableau Visual Best Practices', 'https://help.tableau.com/current/pro/desktop/en-us/visual_best_practices.htm', 'documentation', 'Official Tableau guidance for clear and effective visual analysis.', 'Tableau', 'intermediate'),
('ops-grafana-fundamentals', 'Grafana Fundamentals', 'https://grafana.com/docs/grafana/latest/fundamentals/', 'documentation', 'Grafana documentation explaining dashboards, panels, data sources, and visualization workflow.', 'Grafana', 'beginner'),
('python-tutorial', 'Python Tutorial', 'https://docs.python.org/3/tutorial/', 'documentation', 'Official Python tutorial for programming fundamentals.', 'Python', 'beginner'),
('common-python-tutorial', 'The Python Tutorial', 'https://docs.python.org/3/tutorial/index.html', 'documentation', 'Official Python tutorial for syntax, functions, modules, errors, and standard language features.', 'Python', 'beginner'),
('cur-python-tutorial', 'The Python Tutorial', 'https://docs.python.org/3/tutorial/index.html', 'documentation', 'Official Python tutorial for syntax, functions, modules, and standard language features.', 'Python', 'beginner'),
('nngroup-analytics-reports', 'Analytics and User Experience Articles', 'https://www.nngroup.com/topic/analytics/', 'article', 'Credible UX analytics articles for interpreting user behavior data.', 'Nielsen Norman Group', 'intermediate'),
('sup-azure-api-design', 'API Design Best Practices', 'https://learn.microsoft.com/en-us/azure/architecture/best-practices/api-design', 'documentation', 'Microsoft guidance for REST API design, resource modeling, versioning, pagination, and errors.', 'Microsoft Learn', 'intermediate'),
('sup-google-code-review', 'Google Engineering Practices: Code Review', 'https://google.github.io/eng-practices/review/', 'documentation', 'Google guidance for code reviews, pull requests, reviewer expectations, and review quality.', 'Google Engineering Practices', 'intermediate'),
('sup-owasp-api-top10', 'OWASP API Security Top 10', 'https://owasp.org/API-Security/editions/2023/en/0x00-header/', 'documentation', 'OWASP API security risks covering authorization, authentication, inventory, and unsafe API consumption.', 'OWASP', 'intermediate'),
('sup-postgres-explain', 'PostgreSQL EXPLAIN', 'https://www.postgresql.org/docs/current/using-explain.html', 'documentation', 'PostgreSQL documentation for reading query plans and diagnosing SQL performance.', 'PostgreSQL', 'intermediate'),
('sup-pytest-docs', 'pytest Documentation', 'https://docs.pytest.org/en/stable/', 'documentation', 'Official pytest documentation for Python unit tests, fixtures, assertions, and test organization.', 'pytest', 'beginner'),
('sup-python-unittest', 'unittest: Unit testing framework', 'https://docs.python.org/3/library/unittest.html', 'documentation', 'Official Python unittest documentation for test cases, fixtures, assertions, and test suites.', 'Python', 'beginner'),
('sup-tableau-help', 'Tableau Desktop and Web Authoring Help', 'https://help.tableau.com/current/pro/desktop/en-us/', 'documentation', 'Official Tableau authoring documentation for visual analysis, dashboards, calculations, and publishing.', 'Tableau', 'intermediate'),
('sup-scipy-stats', 'SciPy Statistical Functions', 'https://docs.scipy.org/doc/scipy/reference/stats.html', 'documentation', 'Official SciPy statistics reference for distributions, hypothesis tests, and statistical functions.', 'SciPy', 'intermediate'),
('nist-privacy-framework', 'NIST Privacy Framework', 'https://www.nist.gov/privacy-framework', 'documentation', 'NIST framework for privacy risk management and handling sensitive data responsibly.', 'NIST', 'intermediate'),
('github-readme-docs-extra', 'About READMEs', 'https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-readmes', 'documentation', 'GitHub documentation for presenting projects clearly in repositories.', 'GitHub Docs', 'beginner'),
('google-tech-writing-one-extra', 'Technical Writing One', 'https://developers.google.com/tech-writing/one', 'course', 'Google technical writing course for concise project summaries and readiness review documentation.', 'Google Developers', 'beginner'),
('qe-deepeval-docs', 'DeepEval Documentation', 'https://docs.confident-ai.com/', 'documentation', 'DeepEval documentation for LLM test cases, metrics, and evaluation workflows.', 'Confident AI', 'intermediate'),
('qe-mongodb-docs', 'MongoDB Manual', 'https://www.mongodb.com/docs/manual/', 'documentation', 'Official MongoDB manual for document modeling, queries, indexes, and operations.', 'MongoDB', 'beginner'),
('qe-sklearn-user-guide', 'scikit-learn User Guide', 'https://scikit-learn.org/stable/user_guide.html', 'documentation', 'Official scikit-learn guide for supervised learning, preprocessing, metrics, and model selection.', 'scikit-learn', 'beginner'),
('qe-kaggle-data-viz', 'Kaggle Learn: Data Visualization', 'https://www.kaggle.com/learn/data-visualization', 'course', 'Hands-on micro-course for practical data visualization with Python.', 'Kaggle Learn', 'beginner'),
('qe-owasp-web-security-testing', 'OWASP Web Security Testing Guide', 'https://owasp.org/www-project-web-security-testing-guide/', 'documentation', 'OWASP guide for testing common web application security risks.', 'OWASP', 'intermediate'),
('qe-aspnet-minimal-api', 'ASP.NET Core Minimal APIs', 'https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis', 'documentation', 'Microsoft Learn documentation for building HTTP APIs with ASP.NET Core minimal APIs.', 'Microsoft Learn', 'intermediate'),
('qe-opentelemetry-observability', 'OpenTelemetry Observability Primer', 'https://opentelemetry.io/docs/concepts/observability-primer/', 'documentation', 'OpenTelemetry primer explaining telemetry signals, observability, metrics, logs, and traces.', 'OpenTelemetry', 'beginner'),
('qe-kaggle-data-cleaning', 'Kaggle Learn: Data Cleaning', 'https://www.kaggle.com/learn/data-cleaning', 'course', 'Hands-on micro-course for missing values, parsing dates, encodings, and inconsistent data.', 'Kaggle Learn', 'beginner'),
('qe-github-readme', 'About READMEs', 'https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/about-readmes', 'documentation', 'GitHub documentation for repository README files used to explain setup, evidence, and project context.', 'GitHub Docs', 'beginner'),
('qe-google-sre-monitoring', 'Monitoring Distributed Systems', 'https://sre.google/sre-book/monitoring-distributed-systems/', 'book', 'Google SRE book chapter on monitoring principles for distributed systems.', 'Google SRE', 'intermediate'),
('qe-github-pages', 'GitHub Pages Documentation', 'https://docs.github.com/en/pages', 'documentation', 'GitHub Pages documentation for publishing project websites and portfolio pages.', 'GitHub Docs', 'beginner');

UPDATE public.learning_resource lr
SET title = sr.title,
    resource_type = sr.resource_type,
    description = sr.description,
    provider = sr.provider,
    difficulty_level = sr.difficulty_level,
    language_code = 'en',
    verification_status = 'verified',
    updated_at = now()
FROM (SELECT DISTINCT ON (url) * FROM seed_resource ORDER BY url, resource_key) sr
WHERE lr.url = sr.url;

INSERT INTO public.learning_resource (title, url, resource_type, description, provider, difficulty_level, language_code, verification_status)
SELECT sr.title, sr.url, sr.resource_type, sr.description, sr.provider, sr.difficulty_level, 'en', 'verified'
FROM (SELECT DISTINCT ON (url) * FROM seed_resource ORDER BY url, resource_key) sr
WHERE NOT EXISTS (SELECT 1 FROM public.learning_resource lr WHERE lr.url = sr.url);

DROP TABLE IF EXISTS seed_node;
CREATE TEMP TABLE seed_node (
    node_key text PRIMARY KEY,
    parent_key text NOT NULL,
    order_index int NOT NULL,
    node_type text NOT NULL,
    checkpoint_type text,
    selection_type text,
    required_count int,
    title text NOT NULL,
    description text NOT NULL,
    layout_role text NOT NULL,
    estimated_hours int,
    difficulty_level text,
    metadata jsonb,
    is_required boolean,
    is_trackable boolean,
    learning_outcomes jsonb,
    completion_criteria jsonb,
    node_id uuid DEFAULT gen_random_uuid()
) ON COMMIT DROP;
INSERT INTO seed_node
(node_key, parent_key, order_index, node_type, checkpoint_type, selection_type, required_count, title, description, layout_role, estimated_hours, difficulty_level, metadata, is_required, is_trackable, learning_outcomes, completion_criteria)
VALUES
('data-source-types', 'ph-da-foundations', 20, 'topic', NULL, NULL, NULL, 'Data Source Types', 'Compare transactional systems, event logs, surveys, spreadsheets, third-party data, and public datasets so the learner can choose appropriate analysis methods.', 'side', 3, 'beginner', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Data Source Types in a data analyst workflow.", "Use Data Source Types to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Data Source Types.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('analytical-deliverable-types', 'ph-da-foundations', 21, 'topic', NULL, NULL, NULL, 'Analytical Deliverable Types', 'Distinguish ad hoc analysis, recurring reports, dashboards, metric dictionaries, insight decks, and decision memos.', 'side', 3, 'beginner', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Analytical Deliverable Types in a data analyst workflow.", "Use Analytical Deliverable Types to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Analytical Deliverable Types.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('stakeholder-interview-questions', 'ph-da-foundations', 22, 'topic', NULL, NULL, NULL, 'Stakeholder Interview Questions', 'Ask focused questions that uncover decision context, definitions, constraints, success criteria, and risk before doing analysis.', 'side', 3, 'beginner', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Stakeholder Interview Questions in a data analyst workflow.", "Use Stakeholder Interview Questions to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Stakeholder Interview Questions.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('spreadsheet-data-validation', 'ph-spreadsheets', 20, 'topic', NULL, NULL, NULL, 'Spreadsheet Data Validation Rules', 'Use validation lists, allowed ranges, input rules, and error prompts to reduce manual spreadsheet data quality issues.', 'side', 3, 'beginner', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Spreadsheet Data Validation Rules in a data analyst workflow.", "Use Spreadsheet Data Validation Rules to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Spreadsheet Data Validation Rules.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('conditional-formatting-analysis', 'ph-spreadsheets', 21, 'topic', NULL, NULL, NULL, 'Conditional Formatting for Analysis', 'Use conditional formatting to highlight outliers, thresholds, missing values, priority records, and exceptions without hiding raw data.', 'side', 3, 'beginner', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Conditional Formatting for Analysis in a data analyst workflow.", "Use Conditional Formatting for Analysis to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Conditional Formatting for Analysis.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('spreadsheet-lookup-patterns', 'ph-spreadsheets', 22, 'topic', NULL, NULL, NULL, 'Spreadsheet Lookup Patterns', 'Use XLOOKUP-style patterns to enrich records, audit mismatches, and avoid brittle manual copy-paste analysis.', 'side', 3, 'beginner', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Spreadsheet Lookup Patterns in a data analyst workflow.", "Use Spreadsheet Lookup Patterns to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Spreadsheet Lookup Patterns.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('sql-date-time-analysis', 'ph-sql-databases', 20, 'topic', NULL, NULL, NULL, 'SQL Date and Time Analysis', 'Use date extraction, intervals, truncation, rolling periods, and calendar logic for time-based business analysis.', 'side', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply SQL Date and Time Analysis in a data analyst workflow.", "Use SQL Date and Time Analysis to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating SQL Date and Time Analysis.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('cohort-retention-sql', 'ph-sql-databases', 21, 'topic', NULL, NULL, NULL, 'Cohort and Retention SQL', 'Build cohort tables and retention calculations using signup periods, activity windows, joins, and window functions.', 'side', 5, 'intermediate', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Cohort and Retention SQL in a data analyst workflow.", "Use Cohort and Retention SQL to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Cohort and Retention SQL.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('analyst-dimensional-modeling', 'ph-sql-databases', 22, 'topic', NULL, NULL, NULL, 'Dimensional Modeling for Analysts', 'Understand facts, dimensions, grain, slowly changing attributes, and why BI-ready schemas reduce metric confusion.', 'side', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Dimensional Modeling for Analysts in a data analyst workflow.", "Use Dimensional Modeling for Analysts to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Dimensional Modeling for Analysts.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('outliers-and-robust-summaries', 'ph-statistics', 20, 'topic', NULL, NULL, NULL, 'Outliers and Robust Summaries', 'Identify outliers, compare robust statistics, and decide whether unusual values are valid signals, quality issues, or segmentation needs.', 'side', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Outliers and Robust Summaries in a data analyst workflow.", "Use Outliers and Robust Summaries to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Outliers and Robust Summaries.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('sample-size-and-power-basics', 'ph-statistics', 21, 'topic', NULL, NULL, NULL, 'Sample Size and Power Basics', 'Explain how small samples and underpowered comparisons affect experiment interpretation and business confidence.', 'side', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Sample Size and Power Basics in a data analyst workflow.", "Use Sample Size and Power Basics to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Sample Size and Power Basics.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('practical-vs-statistical-significance', 'ph-statistics', 22, 'topic', NULL, NULL, NULL, 'Practical vs Statistical Significance', 'Separate statistically detectable changes from changes that matter enough to justify product or business action.', 'side', 3, 'intermediate', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Practical vs Statistical Significance in a data analyst workflow.", "Use Practical vs Statistical Significance to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Practical vs Statistical Significance.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('text-cleaning-and-standardization', 'ph-data-cleaning-preparation', 20, 'topic', NULL, NULL, NULL, 'Text Cleaning and Standardization', 'Clean inconsistent names, categories, casing, spacing, punctuation, and labels before grouping or joining data.', 'side', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Text Cleaning and Standardization in a data analyst workflow.", "Use Text Cleaning and Standardization to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Text Cleaning and Standardization.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('schema-drift-and-source-changes', 'ph-data-cleaning-preparation', 21, 'topic', NULL, NULL, NULL, 'Schema Drift and Source Changes', 'Detect changed columns, renamed fields, type shifts, and upstream source changes before they break reports.', 'side', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Schema Drift and Source Changes in a data analyst workflow.", "Use Schema Drift and Source Changes to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Schema Drift and Source Changes.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('data-dictionary-authoring', 'ph-data-cleaning-preparation', 22, 'topic', NULL, NULL, NULL, 'Data Dictionary Authoring', 'Document column definitions, units, allowed values, owners, caveats, and example records so analysis is reviewable.', 'side', 3, 'intermediate', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Data Dictionary Authoring in a data analyst workflow.", "Use Data Dictionary Authoring to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Data Dictionary Authoring.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('chart-accessibility', 'ph-visualization-storytelling', 20, 'topic', NULL, NULL, NULL, 'Chart Accessibility', 'Design charts with readable labels, contrast, alt text, ordering, and non-color-only encodings for wider accessibility.', 'side', 3, 'intermediate', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Chart Accessibility in a data analyst workflow.", "Use Chart Accessibility to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Chart Accessibility.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('small-multiples-and-faceting', 'ph-visualization-storytelling', 21, 'topic', NULL, NULL, NULL, 'Small Multiples and Faceting', 'Use repeated chart panels to compare categories, regions, or time periods without overloading a single chart.', 'side', 3, 'intermediate', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Small Multiples and Faceting in a data analyst workflow.", "Use Small Multiples and Faceting to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Small Multiples and Faceting.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('executive-summary-writing', 'ph-visualization-storytelling', 22, 'topic', NULL, NULL, NULL, 'Executive Summary Writing', 'Write concise summaries that state the answer, evidence, recommendation, uncertainty, and next action clearly.', 'side', 3, 'intermediate', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Executive Summary Writing in a data analyst workflow.", "Use Executive Summary Writing to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Executive Summary Writing.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('semantic-models-and-metrics', 'ph-bi-tools', 20, 'topic', NULL, NULL, NULL, 'Semantic Models and Reusable Metrics', 'Define reusable measures, dimensions, relationships, and business logic so dashboards stay consistent across audiences.', 'side', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Semantic Models and Reusable Metrics in a data analyst workflow.", "Use Semantic Models and Reusable Metrics to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Semantic Models and Reusable Metrics.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('dax-and-calculated-measures', 'ph-bi-tools', 21, 'topic', NULL, NULL, NULL, 'DAX and Calculated Measures', 'Create basic DAX measures and understand filter context enough to debug common Power BI calculation mistakes.', 'side', 5, 'intermediate', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply DAX and Calculated Measures in a data analyst workflow.", "Use DAX and Calculated Measures to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating DAX and Calculated Measures.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('dashboard-performance-and-refresh', 'ph-bi-tools', 22, 'topic', NULL, NULL, NULL, 'Dashboard Performance and Refresh', 'Improve dashboard load time, data refresh reliability, model size, and visual complexity without losing analytical value.', 'side', 4, 'advanced', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Dashboard Performance and Refresh in a data analyst workflow.", "Use Dashboard Performance and Refresh to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Dashboard Performance and Refresh.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('dashboard-quality-assurance-topic', 'ph-bi-tools', 23, 'topic', NULL, NULL, NULL, 'Dashboard Quality Assurance', 'Test dashboard filters, row counts, metric definitions, permissions, freshness, visual labels, and stakeholder acceptance criteria.', 'side', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Dashboard Quality Assurance in a data analyst workflow.", "Use Dashboard Quality Assurance to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Dashboard Quality Assurance.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('pandas-time-series-analysis', 'ph-python-analytics', 20, 'topic', NULL, NULL, NULL, 'pandas Time Series Analysis', 'Use pandas date parsing, resampling, rolling windows, and period comparisons for time-based analysis.', 'side', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply pandas Time Series Analysis in a data analyst workflow.", "Use pandas Time Series Analysis to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating pandas Time Series Analysis.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('python-report-automation', 'ph-python-analytics', 21, 'topic', NULL, NULL, NULL, 'Python Report Automation', 'Automate repeated data pulls, cleaning steps, charts, exports, and lightweight reporting scripts.', 'side', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Python Report Automation in a data analyst workflow.", "Use Python Report Automation to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Python Report Automation.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('public-data-api-extraction', 'ph-python-analytics', 22, 'topic', NULL, NULL, NULL, 'Public Data API Extraction', 'Fetch and structure public API data while handling parameters, pagination, rate limits, and reproducible extraction notes.', 'side', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Public Data API Extraction in a data analyst workflow.", "Use Public Data API Extraction to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Public Data API Extraction.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('interactive-python-data-apps', 'ph-python-analytics', 23, 'topic', NULL, NULL, NULL, 'Interactive Python Data Apps', 'Build a lightweight Streamlit or Plotly-based data app for portfolio exploration and stakeholder demos.', 'side', 5, 'intermediate', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Interactive Python Data Apps in a data analyst workflow.", "Use Interactive Python Data Apps to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Interactive Python Data Apps.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('ecommerce-funnel-analysis', 'ph-domain-analytics', 20, 'topic', NULL, NULL, NULL, 'E-commerce Funnel Analysis', 'Analyze product views, carts, checkout, purchase conversion, and drop-off using clear funnel definitions.', 'side', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply E-commerce Funnel Analysis in a data analyst workflow.", "Use E-commerce Funnel Analysis to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating E-commerce Funnel Analysis.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('customer-retention-analysis', 'ph-domain-analytics', 21, 'topic', NULL, NULL, NULL, 'Customer Retention Analysis', 'Analyze repeat behavior, churn, cohort retention, and lifecycle stages with clear time windows and caveats.', 'side', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Customer Retention Analysis in a data analyst workflow.", "Use Customer Retention Analysis to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Customer Retention Analysis.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('revenue-and-pricing-analysis', 'ph-domain-analytics', 22, 'topic', NULL, NULL, NULL, 'Revenue and Pricing Analysis', 'Analyze revenue, discounting, average order value, margin awareness, billing data, and pricing changes carefully.', 'side', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Revenue and Pricing Analysis in a data analyst workflow.", "Use Revenue and Pricing Analysis to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Revenue and Pricing Analysis.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('analytics-qa-review-process', 'ph-governance-collaboration', 20, 'topic', NULL, NULL, NULL, 'Analytics QA Review Process', 'Review queries, dashboards, charts, metric definitions, and stakeholder claims before release.', 'side', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Analytics QA Review Process in a data analyst workflow.", "Use Analytics QA Review Process to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Analytics QA Review Process.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('data-contracts-awareness', 'ph-governance-collaboration', 21, 'topic', NULL, NULL, NULL, 'Data Contracts Awareness', 'Understand how schema expectations, ownership, breaking changes, and data producer agreements protect downstream analysis.', 'side', 3, 'intermediate', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Data Contracts Awareness in a data analyst workflow.", "Use Data Contracts Awareness to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Data Contracts Awareness.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('dashboard-ownership-and-maintenance', 'ph-governance-collaboration', 22, 'topic', NULL, NULL, NULL, 'Dashboard Ownership and Maintenance', 'Define dashboard owners, refresh checks, retirement rules, metric change logs, and support expectations.', 'side', 3, 'intermediate', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Dashboard Ownership and Maintenance in a data analyst workflow.", "Use Dashboard Ownership and Maintenance to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Dashboard Ownership and Maintenance.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('time-series-decomposition-awareness', 'ph-advanced-analytics-awareness', 20, 'topic', NULL, NULL, NULL, 'Time Series Decomposition Awareness', 'Explain trend, seasonality, residuals, smoothing, and why naive forecasts are useful baselines.', 'side', 4, 'advanced', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Time Series Decomposition Awareness in a data analyst workflow.", "Use Time Series Decomposition Awareness to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Time Series Decomposition Awareness.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('causal-inference-caution', 'ph-advanced-analytics-awareness', 21, 'topic', NULL, NULL, NULL, 'Causal Inference Caution', 'Recognize selection bias, confounding, reverse causality, and why observational analysis rarely proves causation by itself.', 'side', 4, 'advanced', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Causal Inference Caution in a data analyst workflow.", "Use Causal Inference Caution to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Causal Inference Caution.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('survey-analysis-basics', 'ph-advanced-analytics-awareness', 22, 'topic', NULL, NULL, NULL, 'Survey Analysis Basics', 'Clean survey data, summarize response distributions, handle open-ended fields, and communicate sampling limitations.', 'side', 4, 'intermediate', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Survey Analysis Basics in a data analyst workflow.", "Use Survey Analysis Basics to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Survey Analysis Basics.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('analyst-take-home-challenges', 'ph-portfolio-capstone', 20, 'topic', NULL, NULL, NULL, 'Data Analyst Take-Home Challenge Practice', 'Practice scoped take-home analyses with timeboxing, assumption notes, clean outputs, and concise stakeholder recommendations.', 'side', 4, 'advanced', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Data Analyst Take-Home Challenge Practice in a data analyst workflow.", "Use Data Analyst Take-Home Challenge Practice to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Data Analyst Take-Home Challenge Practice.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('portfolio-dashboard-case-study', 'ph-portfolio-capstone', 21, 'topic', NULL, NULL, NULL, 'Portfolio Dashboard Case Study', 'Convert a dashboard into a case study that explains audience, metrics, interactions, data checks, insight, and business recommendation.', 'side', 4, 'advanced', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Portfolio Dashboard Case Study in a data analyst workflow.", "Use Portfolio Dashboard Case Study to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Portfolio Dashboard Case Study.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('capstone-presentation-walkthrough', 'ph-portfolio-capstone', 22, 'topic', NULL, NULL, NULL, 'Capstone Presentation Walkthrough', 'Prepare a short presentation that explains the capstone problem, data, method, findings, limitations, and next steps.', 'side', 3, 'advanced', '{"generatedBy": "roadmap-platform", "version": "expanded-curated-expansion"}'::jsonb, true, true, '["Explain and apply Capstone Presentation Walkthrough in a data analyst workflow.", "Use Capstone Presentation Walkthrough to produce clearer, more reliable analysis evidence."]'::jsonb, '["Create notes, queries, workbook logic, dashboard checks, or analysis output demonstrating Capstone Presentation Walkthrough.", "Document assumptions, validation steps, and interpretation limits."]'::jsonb),
('proj-spreadsheet-quality-audit', 'ph-spreadsheets', 120, 'project', NULL, NULL, NULL, 'Optional Spreadsheet Quality Audit', 'Optional practice project: Audit a messy workbook for formula errors, validation gaps, inconsistent categories, and documentation issues, then produce a cleanup checklist.', 'side', 4, 'beginner', '{"project":true,"projectBrief":"Audit a messy workbook for formula errors, validation gaps, inconsistent categories, and documentation issues, then produce a cleanup checklist.","projectKind":"analytics_artifact","projectScope":"small_practice_artifact","projectPurpose":"Practice the surrounding roadmap segment through a small reviewable artifact.","phaseContext":"Spreadsheets and Excel Analysis","deliverable":"a Spreadsheet Quality Audit workbook, report, or dashboard with documented assumptions","skillsToPractice":["Excel","Data Quality","Data Validation","Excel","Data Quality","Data Validation"],"suggestedSteps":["Define the Spreadsheet Quality Audit scenario and list the specific items to demonstrate: spreadsheet model.","Define the business question, audience, grain, metric formulas, and decision the artifact should support.","Prepare or clean the dataset and document assumptions, exclusions, and data-quality checks.","Build the report, dashboard, notebook, or workbook with the minimum views needed to answer the question.","Validate the numbers against source rows, spot checks, or reconciliation notes.","Write a short interpretation with caveats and recommended next actions."],"expectedEvidence":["a Spreadsheet Quality Audit workbook, report, or dashboard with documented assumptions.","README or notes that explain setup, usage, assumptions, and expected output.","Metric definitions, charts/tables, source checks, and a concise interpretation for the intended audience.","Validation evidence such as tests, screenshots, logs, query results, metrics, traces, or review notes.","Short reflection on trade-offs, limitations, and one reasonable next improvement."],"reviewFocus":["Does the artifact directly match the project brief?","Can another person run, inspect, or review the result from the provided notes?","Are assumptions, limitations, and trade-offs stated clearly?"],"scopeGuardrails":["Keep the implementation focused on the stated project brief.","Prefer a complete small artifact over a broad unfinished system.","Treat completion as reviewable evidence, not a certification claim."],"claimLimit":"Completion creates reviewable project evidence. It does not certify mastery or guarantee job readiness.","evidenceRequired":true,"optionalPractice":true,"milestone":false,"nodeKey":"proj-spreadsheet-quality-audit"}'::jsonb, false, true, '["Apply Excel, Data Quality, Data Validation through a focused Spreadsheet Quality Audit artifact.","Explain the main design, data, tooling, workflow, or review decisions behind the project.","Validate the result with evidence that another person can inspect.","Document assumptions, limitations, and a concrete follow-up improvement."]'::jsonb, '["The a Spreadsheet Quality Audit workbook, report, or dashboard with documented assumptions is available for review.","The Spreadsheet Quality Audit scope, setup path, input data or scenario, and expected output are documented.","The main workflow runs or can be inspected through a notebook, repository, dashboard, prototype, report, configuration, or case study.","Validation evidence is included and directly relates to the project brief.","The notes identify trade-offs, limitations, and one next improvement without claiming mastery or job readiness."]'::jsonb),
('proj-sql-cohort-retention-analysis', 'ph-sql-databases', 120, 'project', NULL, NULL, NULL, 'Optional SQL Cohort Retention Analysis', 'Optional practice project: Build a cohort retention table and summarize retention patterns using SQL date logic and window functions.', 'side', 6, 'intermediate', '{"project":true,"projectBrief":"Build a cohort retention table and summarize retention patterns using SQL date logic and window functions.","projectKind":"analytics_artifact","projectScope":"small_practice_artifact","projectPurpose":"Practice the surrounding roadmap segment through a small reviewable artifact.","phaseContext":"SQL and Relational Data","deliverable":"an SQL Cohort Retention Analysis workbook, report, or dashboard with documented assumptions","skillsToPractice":["SQL","Cohort Analysis","Retention Analysis","SQL","Cohort Analysis","Retention Analysis"],"suggestedSteps":["Define the SQL Cohort Retention Analysis scenario and list the specific items to demonstrate: SQL queries, cohort logic.","Define the business question, audience, grain, metric formulas, and decision the artifact should support.","Prepare or clean the dataset and document assumptions, exclusions, and data-quality checks.","Build the report, dashboard, notebook, or workbook with the minimum views needed to answer the question.","Validate the numbers against source rows, spot checks, or reconciliation notes.","Write a short interpretation with caveats and recommended next actions."],"expectedEvidence":["an SQL Cohort Retention Analysis workbook, report, or dashboard with documented assumptions.","README or notes that explain setup, usage, assumptions, and expected output.","Metric definitions, charts/tables, source checks, and a concise interpretation for the intended audience.","Validation evidence such as tests, screenshots, logs, query results, metrics, traces, or review notes.","Short reflection on trade-offs, limitations, and one reasonable next improvement."],"reviewFocus":["Does the artifact directly match the project brief?","Can another person run, inspect, or review the result from the provided notes?","Are assumptions, limitations, and trade-offs stated clearly?"],"scopeGuardrails":["Keep the implementation focused on the stated project brief.","Prefer a complete small artifact over a broad unfinished system.","Treat completion as reviewable evidence, not a certification claim."],"claimLimit":"Completion creates reviewable project evidence. It does not certify mastery or guarantee job readiness.","evidenceRequired":true,"optionalPractice":true,"milestone":false,"nodeKey":"proj-sql-cohort-retention-analysis"}'::jsonb, false, true, '["Apply SQL, Cohort Analysis, Retention Analysis through a focused SQL Cohort Retention Analysis artifact.","Explain the main design, data, tooling, workflow, or review decisions behind the project.","Validate the result with evidence that another person can inspect.","Document assumptions, limitations, and a concrete follow-up improvement."]'::jsonb, '["The an SQL Cohort Retention Analysis workbook, report, or dashboard with documented assumptions is available for review.","The SQL Cohort Retention Analysis scope, setup path, input data or scenario, and expected output are documented.","The main workflow runs or can be inspected through a notebook, repository, dashboard, prototype, report, configuration, or case study.","Validation evidence is included and directly relates to the project brief.","The notes identify trade-offs, limitations, and one next improvement without claiming mastery or job readiness."]'::jsonb),
('proj-dashboard-qa-checklist', 'ph-bi-tools', 120, 'project', NULL, NULL, NULL, 'Optional Dashboard QA Checklist', 'Optional practice project: Create and apply a dashboard QA checklist covering metric accuracy, filters, freshness, permissions, performance, and stakeholder acceptance.', 'side', 4, 'intermediate', '{"project":true,"projectBrief":"Create and apply a dashboard QA checklist covering metric accuracy, filters, freshness, permissions, performance, and stakeholder acceptance.","projectKind":"analytics_artifact","projectScope":"small_practice_artifact","projectPurpose":"Practice the surrounding roadmap segment through a small reviewable artifact.","phaseContext":"BI Tools and Dashboard Development","deliverable":"a Dashboard QA Checklist workbook, report, or dashboard with documented assumptions","skillsToPractice":["Dashboard Quality Assurance","Dashboard Design","Dashboard Quality Assurance","Dashboard Design"],"suggestedSteps":["Define the Dashboard QA Checklist scenario and list the specific items to demonstrate: dashboard decisions.","Define the business question, audience, grain, metric formulas, and decision the artifact should support.","Prepare or clean the dataset and document assumptions, exclusions, and data-quality checks.","Build the report, dashboard, notebook, or workbook with the minimum views needed to answer the question.","Validate the numbers against source rows, spot checks, or reconciliation notes.","Write a short interpretation with caveats and recommended next actions."],"expectedEvidence":["a Dashboard QA Checklist workbook, report, or dashboard with documented assumptions.","README or notes that explain setup, usage, assumptions, and expected output.","Metric definitions, charts/tables, source checks, and a concise interpretation for the intended audience.","Validation evidence such as tests, screenshots, logs, query results, metrics, traces, or review notes.","Short reflection on trade-offs, limitations, and one reasonable next improvement."],"reviewFocus":["Does the artifact directly match the project brief?","Can another person run, inspect, or review the result from the provided notes?","Are assumptions, limitations, and trade-offs stated clearly?"],"scopeGuardrails":["Keep the implementation focused on the stated project brief.","Prefer a complete small artifact over a broad unfinished system.","Treat completion as reviewable evidence, not a certification claim."],"claimLimit":"Completion creates reviewable project evidence. It does not certify mastery or guarantee job readiness.","evidenceRequired":true,"optionalPractice":true,"milestone":false,"nodeKey":"proj-dashboard-qa-checklist"}'::jsonb, false, true, '["Apply Dashboard Quality Assurance, Dashboard Design, Dashboard Quality Assurance through a focused Dashboard QA Checklist artifact.","Explain the main design, data, tooling, workflow, or review decisions behind the project.","Validate the result with evidence that another person can inspect.","Document assumptions, limitations, and a concrete follow-up improvement."]'::jsonb, '["The a Dashboard QA Checklist workbook, report, or dashboard with documented assumptions is available for review.","The Dashboard QA Checklist scope, setup path, input data or scenario, and expected output are documented.","The main workflow runs or can be inspected through a notebook, repository, dashboard, prototype, report, configuration, or case study.","Validation evidence is included and directly relates to the project brief.","The notes identify trade-offs, limitations, and one next improvement without claiming mastery or job readiness."]'::jsonb),
('proj-python-report-automation', 'ph-python-analytics', 120, 'project', NULL, NULL, NULL, 'Optional Python Report Automation Script', 'Optional practice project: Automate a repeated analysis workflow that loads data, validates assumptions, produces charts, and exports a small report.', 'side', 6, 'intermediate', '{"project":true,"projectBrief":"Automate a repeated analysis workflow that loads data, validates assumptions, produces charts, and exports a small report.","projectKind":"analytics_artifact","projectScope":"small_practice_artifact","projectPurpose":"Practice the surrounding roadmap segment through a small reviewable artifact.","phaseContext":"Python for Data Analysis","deliverable":"a Python Report Automation Script workbook, report, or dashboard with documented assumptions","skillsToPractice":["Python","Exploratory Data Analysis","Python","Exploratory Data Analysis"],"suggestedSteps":["Define the Python Report Automation Script scenario and list the specific items to demonstrate: automation flow.","Define the business question, audience, grain, metric formulas, and decision the artifact should support.","Prepare or clean the dataset and document assumptions, exclusions, and data-quality checks.","Build the report, dashboard, notebook, or workbook with the minimum views needed to answer the question.","Validate the numbers against source rows, spot checks, or reconciliation notes.","Write a short interpretation with caveats and recommended next actions."],"expectedEvidence":["a Python Report Automation Script workbook, report, or dashboard with documented assumptions.","README or notes that explain setup, usage, assumptions, and expected output.","Metric definitions, charts/tables, source checks, and a concise interpretation for the intended audience.","Validation evidence such as tests, screenshots, logs, query results, metrics, traces, or review notes.","Short reflection on trade-offs, limitations, and one reasonable next improvement."],"reviewFocus":["Does the artifact directly match the project brief?","Can another person run, inspect, or review the result from the provided notes?","Are assumptions, limitations, and trade-offs stated clearly?"],"scopeGuardrails":["Keep the implementation focused on the stated project brief.","Prefer a complete small artifact over a broad unfinished system.","Treat completion as reviewable evidence, not a certification claim."],"claimLimit":"Completion creates reviewable project evidence. It does not certify mastery or guarantee job readiness.","evidenceRequired":true,"optionalPractice":true,"milestone":false,"nodeKey":"proj-python-report-automation"}'::jsonb, false, true, '["Apply Python, Exploratory Data Analysis, Python through a focused Python Report Automation Script artifact.","Explain the main design, data, tooling, workflow, or review decisions behind the project.","Validate the result with evidence that another person can inspect.","Document assumptions, limitations, and a concrete follow-up improvement."]'::jsonb, '["The a Python Report Automation Script workbook, report, or dashboard with documented assumptions is available for review.","The Python Report Automation Script scope, setup path, input data or scenario, and expected output are documented.","The main workflow runs or can be inspected through a notebook, repository, dashboard, prototype, report, configuration, or case study.","Validation evidence is included and directly relates to the project brief.","The notes identify trade-offs, limitations, and one next improvement without claiming mastery or job readiness."]'::jsonb),
('proj-ecommerce-funnel-case-study', 'ph-domain-analytics', 120, 'project', NULL, NULL, NULL, 'Optional E-commerce Funnel Case Study', 'Optional practice project: Analyze an e-commerce funnel, identify drop-off points, segment users, and recommend a prioritized investigation plan.', 'side', 6, 'intermediate', '{"project":true,"projectBrief":"Analyze an e-commerce funnel, identify drop-off points, segment users, and recommend a prioritized investigation plan.","projectKind":"analytics_artifact","projectScope":"small_practice_artifact","projectPurpose":"Practice the surrounding roadmap segment through a small reviewable artifact.","phaseContext":"Business Domain Analytics","deliverable":"a E-commerce Funnel Case Study workbook, report, or dashboard with documented assumptions","skillsToPractice":["Funnel Analysis","Business Metrics","Data Storytelling","Funnel Analysis","Business Metrics","Data Storytelling"],"suggestedSteps":["Define the E-commerce Funnel Case Study scenario and list the specific items to demonstrate: funnel steps.","Define the business question, audience, grain, metric formulas, and decision the artifact should support.","Prepare or clean the dataset and document assumptions, exclusions, and data-quality checks.","Build the report, dashboard, notebook, or workbook with the minimum views needed to answer the question.","Validate the numbers against source rows, spot checks, or reconciliation notes.","Write a short interpretation with caveats and recommended next actions."],"expectedEvidence":["a E-commerce Funnel Case Study workbook, report, or dashboard with documented assumptions.","README or notes that explain setup, usage, assumptions, and expected output.","Metric definitions, charts/tables, source checks, and a concise interpretation for the intended audience.","Validation evidence such as tests, screenshots, logs, query results, metrics, traces, or review notes.","Short reflection on trade-offs, limitations, and one reasonable next improvement."],"reviewFocus":["Does the artifact directly match the project brief?","Can another person run, inspect, or review the result from the provided notes?","Are assumptions, limitations, and trade-offs stated clearly?"],"scopeGuardrails":["Keep the implementation focused on the stated project brief.","Prefer a complete small artifact over a broad unfinished system.","Treat completion as reviewable evidence, not a certification claim."],"claimLimit":"Completion creates reviewable project evidence. It does not certify mastery or guarantee job readiness.","evidenceRequired":true,"optionalPractice":true,"milestone":false,"nodeKey":"proj-ecommerce-funnel-case-study"}'::jsonb, false, true, '["Apply Funnel Analysis, Business Metrics, Data Storytelling through a focused E-commerce Funnel Case Study artifact.","Explain the main design, data, tooling, workflow, or review decisions behind the project.","Validate the result with evidence that another person can inspect.","Document assumptions, limitations, and a concrete follow-up improvement."]'::jsonb, '["The a E-commerce Funnel Case Study workbook, report, or dashboard with documented assumptions is available for review.","The E-commerce Funnel Case Study scope, setup path, input data or scenario, and expected output are documented.","The main workflow runs or can be inspected through a notebook, repository, dashboard, prototype, report, configuration, or case study.","Validation evidence is included and directly relates to the project brief.","The notes identify trade-offs, limitations, and one next improvement without claiming mastery or job readiness."]'::jsonb),
('proj-survey-analysis-report', 'ph-advanced-analytics-awareness', 120, 'project', NULL, NULL, NULL, 'Optional Survey Analysis Report', 'Optional practice project: Clean and summarize survey responses, visualize key patterns, handle open-ended fields, and communicate sampling limitations.', 'side', 5, 'intermediate', '{"project":true,"projectBrief":"Clean and summarize survey responses, visualize key patterns, handle open-ended fields, and communicate sampling limitations.","projectKind":"analytics_artifact","projectScope":"small_practice_artifact","projectPurpose":"Practice the surrounding roadmap segment through a small reviewable artifact.","phaseContext":"Advanced Analytics Awareness","deliverable":"a Survey Analysis Report workbook, report, or dashboard with documented assumptions","skillsToPractice":["Survey Analysis","Statistics","Data Storytelling","Survey Analysis","Statistics","Data Storytelling"],"suggestedSteps":["Define the Survey Analysis Report scenario, target user or reviewer, and the specific Advanced Analytics Awareness concepts it will demonstrate.","Define the business question, audience, grain, metric formulas, and decision the artifact should support.","Prepare or clean the dataset and document assumptions, exclusions, and data-quality checks.","Build the report, dashboard, notebook, or workbook with the minimum views needed to answer the question.","Validate the numbers against source rows, spot checks, or reconciliation notes.","Write a short interpretation with caveats and recommended next actions."],"expectedEvidence":["a Survey Analysis Report workbook, report, or dashboard with documented assumptions.","README or notes that explain setup, usage, assumptions, and expected output.","Metric definitions, charts/tables, source checks, and a concise interpretation for the intended audience.","Validation evidence such as tests, screenshots, logs, query results, metrics, traces, or review notes.","Short reflection on trade-offs, limitations, and one reasonable next improvement."],"reviewFocus":["Does the artifact directly match the project brief?","Can another person run, inspect, or review the result from the provided notes?","Are assumptions, limitations, and trade-offs stated clearly?"],"scopeGuardrails":["Keep the implementation focused on the stated project brief.","Prefer a complete small artifact over a broad unfinished system.","Treat completion as reviewable evidence, not a certification claim."],"claimLimit":"Completion creates reviewable project evidence. It does not certify mastery or guarantee job readiness.","evidenceRequired":true,"optionalPractice":true,"milestone":false,"nodeKey":"proj-survey-analysis-report"}'::jsonb, false, true, '["Apply Survey Analysis, Statistics, Data Storytelling through a focused Survey Analysis Report artifact.","Explain the main design, data, tooling, workflow, or review decisions behind the project.","Validate the result with evidence that another person can inspect.","Document assumptions, limitations, and a concrete follow-up improvement."]'::jsonb, '["The a Survey Analysis Report workbook, report, or dashboard with documented assumptions is available for review.","The Survey Analysis Report scope, setup path, input data or scenario, and expected output are documented.","The main workflow runs or can be inspected through a notebook, repository, dashboard, prototype, report, configuration, or case study.","Validation evidence is included and directly relates to the project brief.","The notes identify trade-offs, limitations, and one next improvement without claiming mastery or job readiness."]'::jsonb),
('proj-public-data-api-mini-project', 'ph-python-analytics', 121, 'project', NULL, NULL, NULL, 'Optional Public Data API Mini Project', 'Optional practice project: Collect public API data, transform it into an analysis-ready table, and document reproducible extraction steps.', 'side', 5, 'intermediate', '{"project":true,"projectBrief":"Collect public API data, transform it into an analysis-ready table, and document reproducible extraction steps.","projectKind":"analytics_artifact","projectScope":"small_practice_artifact","projectPurpose":"Practice the surrounding roadmap segment through a small reviewable artifact.","phaseContext":"Python for Data Analysis","deliverable":"a Public Data API Mini Project workbook, report, or dashboard with documented assumptions","skillsToPractice":["Python","API","Python","API"],"suggestedSteps":["Define the Public Data API Mini Project scenario and list the specific items to demonstrate: API behavior.","Define the business question, audience, grain, metric formulas, and decision the artifact should support.","Prepare or clean the dataset and document assumptions, exclusions, and data-quality checks.","Build the report, dashboard, notebook, or workbook with the minimum views needed to answer the question.","Validate the numbers against source rows, spot checks, or reconciliation notes.","Write a short interpretation with caveats and recommended next actions."],"expectedEvidence":["a Public Data API Mini Project workbook, report, or dashboard with documented assumptions.","README or notes that explain setup, usage, assumptions, and expected output.","Metric definitions, charts/tables, source checks, and a concise interpretation for the intended audience.","Validation evidence such as tests, screenshots, logs, query results, metrics, traces, or review notes.","Short reflection on trade-offs, limitations, and one reasonable next improvement."],"reviewFocus":["Does the artifact directly match the project brief?","Can another person run, inspect, or review the result from the provided notes?","Are assumptions, limitations, and trade-offs stated clearly?"],"scopeGuardrails":["Keep the implementation focused on the stated project brief.","Prefer a complete small artifact over a broad unfinished system.","Treat completion as reviewable evidence, not a certification claim."],"claimLimit":"Completion creates reviewable project evidence. It does not certify mastery or guarantee job readiness.","evidenceRequired":true,"optionalPractice":true,"milestone":false,"nodeKey":"proj-public-data-api-mini-project"}'::jsonb, false, true, '["Apply Python, API, Python through a focused Public Data API Mini Project artifact.","Explain the main design, data, tooling, workflow, or review decisions behind the project.","Validate the result with evidence that another person can inspect.","Document assumptions, limitations, and a concrete follow-up improvement."]'::jsonb, '["The a Public Data API Mini Project workbook, report, or dashboard with documented assumptions is available for review.","The Public Data API Mini Project scope, setup path, input data or scenario, and expected output are documented.","The main workflow runs or can be inspected through a notebook, repository, dashboard, prototype, report, configuration, or case study.","Validation evidence is included and directly relates to the project brief.","The notes identify trade-offs, limitations, and one next improvement without claiming mastery or job readiness."]'::jsonb),
('proj-portfolio-presentation-video', 'ph-portfolio-capstone', 120, 'project', NULL, NULL, NULL, 'Optional Portfolio Presentation Video', 'Capstone project: Record a concise walkthrough of one analyst project, explaining the question, data, method, dashboard, recommendation, and limitations.', 'side', 3, 'advanced', '{"project":true,"projectBrief":"Record a concise walkthrough of one analyst project, explaining the question, data, method, dashboard, recommendation, and limitations.","projectKind":"portfolio_capstone","projectScope":"portfolio_capstone","projectPurpose":"Integrate several roadmap areas into a portfolio-ready case study.","phaseContext":"Portfolio, Interview Readiness, and Capstone","deliverable":"a portfolio-ready Portfolio Presentation Video case study and integrated artifact","skillsToPractice":["Stakeholder Communication","Stakeholder Communication"],"suggestedSteps":["Define the Portfolio Presentation Video scenario and list the specific items to demonstrate: dashboard decisions.","Choose a coherent product or case-study scenario that can integrate the strongest projects from the roadmap.","Define the architecture, data or workflow model, user path, and review evidence before implementation.","Build the core artifact end-to-end with scoped features rather than disconnected demos.","Add validation evidence appropriate to the role, such as tests, metrics, query checks, playtest notes, traces, screenshots, or incident notes.","Prepare a case study explaining decisions, trade-offs, limitations, and what would need further review for real deployment.","Package the repository or artifact so another person can reproduce or inspect the result.","Create a review checklist that connects the artifact to the relevant skills and roadmap segment.","Write a limitation and risk section so the project is presented as reviewable evidence, not a certification claim."],"expectedEvidence":["a portfolio-ready Portfolio Presentation Video case study and integrated artifact.","README or notes that explain setup, usage, assumptions, and expected output.","Case study page or write-up that connects the artifact to role-relevant decisions and evidence.","Validation evidence such as tests, screenshots, logs, query results, metrics, traces, or review notes.","Short reflection on trade-offs, limitations, and one reasonable next improvement.","Architecture, review checklist, and limitation section suitable for portfolio discussion."],"reviewFocus":["Does the artifact directly match the project brief?","Can another person run, inspect, or review the result from the provided notes?","Are assumptions, limitations, and trade-offs stated clearly?"],"scopeGuardrails":["Keep the implementation focused on the stated project brief.","Prefer a complete small artifact over a broad unfinished system.","Treat completion as reviewable evidence, not a certification claim."],"claimLimit":"Completion creates reviewable project evidence. It does not certify mastery or guarantee job readiness.","evidenceRequired":true,"optionalPractice":true,"milestone":false,"nodeKey":"proj-portfolio-presentation-video"}'::jsonb, false, true, '["Apply Stakeholder Communication, Stakeholder Communication through a focused Portfolio Presentation Video artifact.","Integrate multiple roadmap areas into one coherent case study rather than isolated exercises.","Explain the main design, data, tooling, workflow, or review decisions behind the project.","Validate the result with evidence that another person can inspect.","Document assumptions, limitations, and a concrete follow-up improvement."]'::jsonb, '["The a portfolio-ready Portfolio Presentation Video case study and integrated artifact is available for review.","The Portfolio Presentation Video scope, setup path, input data or scenario, and expected output are documented.","The main workflow runs or can be inspected through a notebook, repository, dashboard, prototype, report, configuration, or case study.","The case study connects implementation decisions to relevant roadmap skills and explains why alternatives were not chosen.","Validation evidence is included and directly relates to the project brief.","The notes identify trade-offs, limitations, and one next improvement without claiming mastery or job readiness.","The project is packaged so a reviewer can understand the artifact without live mentoring."]'::jsonb);

INSERT INTO public.roadmap_node (roadmap_node_id, roadmap_version_id, parent_node_id, slug, node_type, checkpoint_type, selection_type, required_count, title, description, order_index, layout_role, estimated_hours, difficulty_level, metadata, is_required, is_trackable, learning_outcomes, completion_criteria)
SELECT svn.node_id, m.roadmap_version_id, parent.roadmap_node_id, svn.node_key, svn.node_type, svn.checkpoint_type, svn.selection_type, svn.required_count, svn.title, svn.description, svn.order_index, svn.layout_role, svn.estimated_hours, svn.difficulty_level, svn.metadata, svn.is_required, svn.is_trackable, svn.learning_outcomes, svn.completion_criteria
FROM seed_node svn
JOIN seed_roadmap_map m ON true
JOIN public.roadmap_node parent ON parent.roadmap_version_id = m.roadmap_version_id AND parent.slug = svn.parent_key
ON CONFLICT (roadmap_version_id, slug) DO UPDATE SET
    parent_node_id = EXCLUDED.parent_node_id,
    node_type = EXCLUDED.node_type,
    checkpoint_type = EXCLUDED.checkpoint_type,
    selection_type = EXCLUDED.selection_type,
    required_count = EXCLUDED.required_count,
    title = EXCLUDED.title,
    description = EXCLUDED.description,
    order_index = EXCLUDED.order_index,
    layout_role = EXCLUDED.layout_role,
    estimated_hours = EXCLUDED.estimated_hours,
    difficulty_level = EXCLUDED.difficulty_level,
    metadata = EXCLUDED.metadata,
    is_required = EXCLUDED.is_required,
    is_trackable = EXCLUDED.is_trackable,
    learning_outcomes = EXCLUDED.learning_outcomes,
    completion_criteria = EXCLUDED.completion_criteria;

DROP TABLE IF EXISTS seed_node_skill;
CREATE TEMP TABLE seed_node_skill (
    node_key text NOT NULL,
    skill_slug text NOT NULL,
    PRIMARY KEY (node_key, skill_slug)
) ON COMMIT DROP;
-- Additional seed_node_skill rows were consolidated into the first standardized mapping block.



-- Supplemental Data Analyst mappings for nodes inserted in the second seed_node section.
DROP TABLE IF EXISTS seed_node_skill;
CREATE TEMP TABLE seed_node_skill (
    node_key text NOT NULL,
    skill_slug text NOT NULL,
    PRIMARY KEY (node_key, skill_slug)
) ON COMMIT DROP;

INSERT INTO seed_node_skill VALUES

('data-source-types', 'data-source-types'),
('data-source-types', 'data-governance'),
('data-source-types', 'business-metrics'),
('analytical-deliverable-types', 'analytical-deliverable-types'),
('analytical-deliverable-types', 'stakeholder-communication'),
('analytical-deliverable-types', 'data-storytelling'),
('stakeholder-interview-questions', 'stakeholder-interview-questions'),
('stakeholder-interview-questions', 'stakeholder-communication'),
('stakeholder-interview-questions', 'business-metrics'),
('spreadsheet-data-validation', 'spreadsheet-data-validation-rules'),
('spreadsheet-data-validation', 'excel'),
('spreadsheet-data-validation', 'data-validation'),
('conditional-formatting-analysis', 'conditional-formatting-for-analysis'),
('conditional-formatting-analysis', 'excel'),
('conditional-formatting-analysis', 'data-visualization'),
('spreadsheet-lookup-patterns', 'spreadsheet-lookup-patterns'),
('spreadsheet-lookup-patterns', 'excel'),
('spreadsheet-lookup-patterns', 'data-cleaning'),
('sql-date-time-analysis', 'sql-date-and-time-analysis'),
('sql-date-time-analysis', 'sql'),
('sql-date-time-analysis', 'time-series-analysis'),
('cohort-retention-sql', 'cohort-and-retention-sql'),
('cohort-retention-sql', 'sql'),
('cohort-retention-sql', 'cohort-analysis'),
('cohort-retention-sql', 'retention-analysis'),
('analyst-dimensional-modeling', 'dimensional-modeling-for-analysts'),
('analyst-dimensional-modeling', 'data-modeling'),
('analyst-dimensional-modeling', 'relational-databases'),
('analyst-dimensional-modeling', 'business-metrics'),
('outliers-and-robust-summaries', 'outliers-and-robust-summaries'),
('outliers-and-robust-summaries', 'statistics'),
('outliers-and-robust-summaries', 'data-quality'),
('sample-size-and-power-basics', 'sample-size-and-power'),
('sample-size-and-power-basics', 'statistics'),
('sample-size-and-power-basics', 'a-b-testing'),
('practical-vs-statistical-significance', 'practical-vs-statistical-significance'),
('practical-vs-statistical-significance', 'statistics'),
('practical-vs-statistical-significance', 'business-metrics'),
('practical-vs-statistical-significance', 'data-storytelling'),
('text-cleaning-and-standardization', 'text-cleaning-and-standardization'),
('text-cleaning-and-standardization', 'data-cleaning'),
('text-cleaning-and-standardization', 'python'),
('schema-drift-and-source-changes', 'schema-drift-and-source-changes'),
('schema-drift-and-source-changes', 'data-validation'),
('schema-drift-and-source-changes', 'data-contracts'),
('data-dictionary-authoring', 'data-dictionary-authoring'),
('data-dictionary-authoring', 'data-governance'),
('data-dictionary-authoring', 'stakeholder-communication'),
('chart-accessibility', 'chart-accessibility'),
('chart-accessibility', 'accessibility'),
('chart-accessibility', 'data-visualization'),
('small-multiples-and-faceting', 'small-multiples-and-faceting'),
('small-multiples-and-faceting', 'data-visualization'),
('small-multiples-and-faceting', 'data-storytelling'),
('executive-summary-writing', 'executive-summary-writing'),
('executive-summary-writing', 'data-storytelling'),
('executive-summary-writing', 'stakeholder-communication'),
('semantic-models-and-metrics', 'semantic-models-and-reusable-metrics'),
('semantic-models-and-metrics', 'semantic-layer'),
('semantic-models-and-metrics', 'business-metrics'),
('semantic-models-and-metrics', 'data-modeling'),
('dax-and-calculated-measures', 'dax-and-calculated-measures'),
('dax-and-calculated-measures', 'power-bi'),
('dax-and-calculated-measures', 'business-metrics'),
('dashboard-performance-and-refresh', 'dashboard-performance-and-refresh'),
('dashboard-performance-and-refresh', 'dashboard-design'),
('dashboard-performance-and-refresh', 'performance-analysis'),
('dashboard-quality-assurance-topic', 'dashboard-quality-assurance'),
('dashboard-quality-assurance-topic', 'data-validation'),
('pandas-time-series-analysis', 'pandas-time-series-analysis'),
('pandas-time-series-analysis', 'python'),
('pandas-time-series-analysis', 'time-series-analysis'),
('python-report-automation', 'python-report-automation'),
('python-report-automation', 'python'),
('python-report-automation', 'exploratory-data-analysis'),
('public-data-api-extraction', 'public-data-api-extraction'),
('public-data-api-extraction', 'python'),
('public-data-api-extraction', 'api'),
('interactive-python-data-apps', 'interactive-python-data-apps'),
('interactive-python-data-apps', 'python'),
('interactive-python-data-apps', 'dashboard-design'),
('interactive-python-data-apps', 'data-visualization'),
('ecommerce-funnel-analysis', 'e-commerce-funnel-analysis'),
('ecommerce-funnel-analysis', 'funnel-analysis'),
('ecommerce-funnel-analysis', 'business-metrics'),
('customer-retention-analysis', 'customer-retention-analysis'),
('customer-retention-analysis', 'retention-analysis'),
('customer-retention-analysis', 'cohort-analysis'),
('revenue-and-pricing-analysis', 'revenue-and-pricing-analysis'),
('revenue-and-pricing-analysis', 'business-metrics'),
('revenue-and-pricing-analysis', 'statistics'),
('analytics-qa-review-process', 'analytics-qa-review-process'),
('analytics-qa-review-process', 'dashboard-quality-assurance'),
('analytics-qa-review-process', 'data-validation'),
('data-contracts-awareness', 'data-contracts'),
('data-contracts-awareness', 'data-governance'),
('dashboard-ownership-and-maintenance', 'dashboard-ownership-and-maintenance'),
('dashboard-ownership-and-maintenance', 'dashboard-design'),
('dashboard-ownership-and-maintenance', 'data-governance'),
('time-series-decomposition-awareness', 'time-series-decomposition'),
('time-series-decomposition-awareness', 'time-series-analysis'),
('time-series-decomposition-awareness', 'statistics'),
('causal-inference-caution', 'causal-inference-caution'),
('causal-inference-caution', 'statistics'),
('causal-inference-caution', 'data-storytelling'),
('survey-analysis-basics', 'survey-analysis'),
('survey-analysis-basics', 'statistics'),
('analyst-take-home-challenges', 'data-analyst-take-home-challenge-practice'),
('analyst-take-home-challenges', 'stakeholder-communication'),
('portfolio-dashboard-case-study', 'portfolio-communication'),
('portfolio-dashboard-case-study', 'dashboard-design'),
('portfolio-dashboard-case-study', 'technical-case-studies'),
('portfolio-dashboard-case-study', 'data-storytelling'),
('capstone-presentation-walkthrough', 'stakeholder-communication'),
('capstone-presentation-walkthrough', 'data-storytelling'),
('proj-spreadsheet-quality-audit', 'excel'),
('proj-spreadsheet-quality-audit', 'data-quality'),
('proj-spreadsheet-quality-audit', 'data-validation'),
('proj-sql-cohort-retention-analysis', 'sql'),
('proj-sql-cohort-retention-analysis', 'cohort-analysis'),
('proj-sql-cohort-retention-analysis', 'retention-analysis'),
('proj-dashboard-qa-checklist', 'dashboard-quality-assurance'),
('proj-dashboard-qa-checklist', 'dashboard-design'),
('proj-python-report-automation', 'python'),
('proj-python-report-automation', 'exploratory-data-analysis'),
('proj-ecommerce-funnel-case-study', 'funnel-analysis'),
('proj-ecommerce-funnel-case-study', 'business-metrics'),
('proj-ecommerce-funnel-case-study', 'data-storytelling'),
('proj-survey-analysis-report', 'survey-analysis'),
('proj-survey-analysis-report', 'statistics'),
('proj-survey-analysis-report', 'data-storytelling'),
('proj-public-data-api-mini-project', 'python'),
('proj-public-data-api-mini-project', 'api'),
('proj-portfolio-presentation-video', 'stakeholder-communication')
ON CONFLICT (node_key, skill_slug) DO NOTHING;

INSERT INTO public.roadmap_node_skill (roadmap_node_id, skill_id)
SELECT DISTINCT
    rn.roadmap_node_id, resolved_skill.skill_id
FROM seed_node_skill sns
JOIN seed_roadmap_map m
    ON true
JOIN public.roadmap_node rn
    ON rn.roadmap_version_id = m.roadmap_version_id
   AND rn.slug = sns.node_key
LEFT JOIN seed_skill ss
    ON ss.slug = sns.skill_slug
JOIN LATERAL (
    SELECT s.skill_id
    FROM public.skill s
    WHERE s.slug = sns.skill_slug
       OR (ss.name IS NOT NULL AND s.name = ss.name)
    ORDER BY
        CASE WHEN s.slug = sns.skill_slug THEN 0 ELSE 1 END
    LIMIT 1
) resolved_skill ON true
ON CONFLICT (roadmap_node_id, skill_id) DO NOTHING;

WITH duplicate_node_skill AS (
    SELECT
        rns.roadmap_node_skill_id,
        row_number() OVER (
            PARTITION BY rns.roadmap_node_id, rns.skill_id
            ORDER BY rns.roadmap_node_skill_id
        ) AS duplicate_rank
    FROM public.roadmap_node_skill rns
    JOIN public.roadmap_node rn ON rn.roadmap_node_id = rns.roadmap_node_id
    JOIN seed_roadmap_map m ON m.roadmap_version_id = rn.roadmap_version_id
)
DELETE FROM public.roadmap_node_skill rns
USING duplicate_node_skill dns
WHERE rns.roadmap_node_skill_id = dns.roadmap_node_skill_id
  AND dns.duplicate_rank > 1;

DROP TABLE IF EXISTS seed_node_resource;
CREATE TEMP TABLE seed_node_resource (
    node_key text NOT NULL,
    resource_key text NOT NULL,
    order_index int NOT NULL,
    PRIMARY KEY (node_key, resource_key)
) ON COMMIT DROP;
INSERT INTO seed_node_resource (node_key, resource_key, order_index) VALUES
('ab-test-interpretation', 'google-ab-testing-course', 1),
('ab-test-interpretation', 'cur-dbt-tests', 2),
('ab-test-interpretation', 'ibm-data-quality', 3),
('accessibility-in-data-visualization', 'w3c-accessibility-intro', 1),
('accessibility-in-data-visualization', 'wcag-understanding', 2),
('accessibility-in-data-visualization', 'cur-mdn-accessibility', 3),
('aggregation-and-grouping', 'mode-sql-tutorial', 1),
('aggregation-and-grouping', 'sup-postgres-tutorial', 2),
('ai-assisted-analysis-awareness', 'google-data-analytics-certificate', 1),
('ai-assisted-analysis-awareness', 'sqlbolt', 2),
('ai-assisted-analysis-awareness', 'mode-sql-tutorial', 3),
('ai-assisted-analysis-awareness', 'ibm-data-analyst-certificate', 4),
('analyst-dimensional-modeling', 'ibm-data-analyst-certificate', 2),
('analyst-dimensional-modeling', 'google-data-analytics-certificate', 3),
('analyst-take-home-challenges', 'great-expectations-docs', 1),
('analyst-take-home-challenges', 'tableau-performance-recording', 2),
('analyst-take-home-challenges', 'ibm-data-analyst-certificate', 3),
('analyst-take-home-challenges', 'google-data-analytics-certificate', 4),
('analytical-deliverable-types', 'google-data-analytics-certificate', 1),
('analytical-deliverable-types', 'ibm-data-analyst-certificate', 2),
('analytical-deliverable-types', 'data-kaggle-visualization', 3),
('analytical-thinking-basics', 'coursera-wharton-business-analytics', 1),
('analytical-thinking-basics', 'scikit-learn-model-evaluation', 2),
('analytics-backlog-and-prioritization', 'ibm-data-governance', 1),
('analytics-backlog-and-prioritization', 'coursera-wharton-business-analytics', 2),
('analytics-case-interviews', 'google-data-analytics-certificate', 1),
('analytics-case-interviews', 'qe-opentelemetry-observability', 2),
('analytics-case-interviews', 'data-dbt-intro', 3),
('analytics-case-interviews', 'coursera-wharton-business-analytics', 4),
('analytics-documentation', 'stripe-analytics-docs', 1),
('analytics-documentation', 'tableau-help', 2),
('analytics-lifecycle', 'google-data-analytics-certificate', 1),
('analytics-lifecycle', 'microsoft-excel-help', 2),
('analytics-qa-review-process', 'dbt-docs', 1),
('analytics-qa-review-process', 'google-ab-testing-course', 2),
('analytics-qa-review-process', 'sup-google-code-review', 3),
('anomaly-detection-awareness', 'ibm-data-quality', 1),
('anomaly-detection-awareness', 'cur-dbt-tests', 2),
('api-data-extraction', 'census-data-api', 1),
('api-data-extraction', 'world-bank-api', 2),
('api-data-extraction', 'python-docs', 3),
('automation-scripts', 'pandas-missing-data', 1),
('automation-scripts', 'python-docs', 2),
('business-questions-and-kpis', 'coursera-wharton-business-analytics', 1),
('business-questions-and-kpis', 'ibm-data-governance', 2),
('capstone-presentation-walkthrough', 'ibm-data-analyst-certificate', 1),
('capstone-presentation-walkthrough', 'tableau-performance-recording', 2),
('capstone-presentation-walkthrough', 'data-kaggle-visualization', 3),
('capstone-presentation-walkthrough', 'google-data-analytics-certificate', 4),
('case-study-writing', 'coursera-wharton-business-analytics', 1),
('case-study-writing', 'github-writing-readmes', 2),
('case-study-writing', 'sup-google-tech-writing-one', 3),
('causal-inference-caution', 'google-data-analytics-certificate', 1),
('causal-inference-caution', 'ibm-data-analyst-certificate', 2),
('chart-accessibility', 'cur-mdn-accessibility', 1),
('chart-accessibility', 'w3c-accessibility-intro', 2),
('chart-accessibility', 'wcag-understanding', 3),
('chart-selection', 'storytelling-with-data-blog', 1),
('chart-selection', 'qe-kaggle-data-viz', 2),
('chart-selection', 'ibm-data-analyst-certificate', 3),
('chk-advanced-awareness-review', 'google-data-analytics-certificate', 1),
('chk-advanced-awareness-review', 'sup-google-code-review', 2),
('chk-da-foundations-review', 'google-data-analytics-certificate', 1),
('chk-da-foundations-review', 'ibm-data-analyst-certificate', 2),
('chk-da-foundations-review', 'coursera-wharton-business-analytics', 3),
('chk-dashboard-review', 'tableau-performance-recording', 1),
('chk-dashboard-review', 'power-bi-guidance', 2),
('chk-dashboard-review', 'power-bi-docs', 3),
('chk-data-cleaning-review', 'power-query-docs', 1),
('chk-data-cleaning-review', 'sup-google-code-review', 2),
('chk-domain-analytics-review', 'postgres-datetime-functions', 1),
('chk-domain-analytics-review', 'postgres-window-functions', 2),
('chk-final-readiness-review', 'google-tech-writing-one-extra', 1),
('chk-final-readiness-review', 'ibm-data-analyst-certificate', 2),
('chk-governance-review', 'ibm-data-governance', 1),
('chk-governance-review', 'data-dbt-intro', 2),
('chk-governance-review', 'sup-google-code-review', 3),
('chk-python-analytics-review', 'python-tutorial', 1),
('chk-python-analytics-review', 'python-docs', 2),
('chk-spreadsheet-review', 'excel-pivottables-guide', 1),
('chk-spreadsheet-review', 'data-excel-pivottable', 2),
('chk-sql-review', 'sqlbolt', 1),
('chk-sql-review', 'mode-sql-tutorial', 2),
('chk-sql-review', 'kaggle-advanced-sql', 3),
('chk-statistics-review', 'data-khan-statistics', 1),
('chk-statistics-review', 'sup-google-code-review', 2),
('chk-statistics-review', 'sup-openintro-statistics', 3),
('chk-visualization-review', 'w3c-accessibility-intro', 1),
('chk-visualization-review', 'wcag-understanding', 2),
('chk-visualization-review', 'cur-mdn-accessibility', 3),
('cohort-analysis', 'google-ab-testing-course', 1),
('cohort-analysis', 'google-analytics-events', 2),
('cohort-retention-sql', 'postgres-datetime-functions', 1),
('cohort-retention-sql', 'postgres-window-functions', 2),
('cohort-retention-sql', 'kaggle-advanced-sql', 3),
('conditional-formatting-analysis', 'excel-conditional-formatting', 1),
('conditional-formatting-analysis', 'microsoft-excel-help', 2),
('conditional-formatting-analysis', 'google-data-analytics-certificate', 3),
('confidence-intervals', 'khan-statistics', 1),
('confidence-intervals', 'openintro-statistics', 2),
('correlation-causation', 'microsoft-excel-help', 1),
('correlation-causation', 'openintro-statistics', 2),
('ctes-and-subqueries', 'sqlbolt', 1),
('ctes-and-subqueries', 'mode-sql-tutorial', 2),
('ctes-and-subqueries', 'kaggle-advanced-sql', 3),
('customer-retention-analysis', 'cur-matplotlib-quickstart', 1),
('customer-retention-analysis', 'google-data-analytics-certificate', 2),
('dashboard-data-modeling', 'google-data-analytics-certificate', 1),
('dashboard-data-modeling', 'tableau-help', 2),
('dashboard-documentation', 'tableau-performance-recording', 1),
('dashboard-documentation', 'data-kaggle-visualization', 2),
('dashboard-documentation', 'ibm-data-analyst-certificate', 3),
('dashboard-interactivity', 'tableau-help', 1),
('dashboard-interactivity', 'nngroup-dashboards', 2),
('dashboard-ownership-and-maintenance', 'tableau-performance-recording', 1),
('dashboard-ownership-and-maintenance', 'data-kaggle-visualization', 2),
('dashboard-performance-and-refresh', 'tableau-performance-recording', 1),
('dashboard-performance-and-refresh', 'power-bi-guidance', 2),
('dashboard-performance-and-refresh', 'power-bi-docs', 3),
('dashboard-performance-and-refresh', 'data-kaggle-visualization', 4),
('dashboard-performance-basics', 'tableau-performance-recording', 1),
('dashboard-performance-basics', 'power-bi-guidance', 2),
('dashboard-performance-basics', 'power-bi-docs', 3),
('dashboard-performance-basics', 'data-kaggle-visualization', 4),
('dashboard-portfolio-polish', 'tableau-performance-recording', 1),
('dashboard-portfolio-polish', 'power-bi-guidance', 2),
('dashboard-portfolio-polish', 'power-bi-docs', 3),
('dashboard-portfolio-polish', 'nngroup-dashboards', 4),
('dashboard-purpose-and-layout', 'google-data-analytics-certificate', 1),
('dashboard-purpose-and-layout', 'tableau-help', 2),
('dashboard-quality-assurance-topic', 'great-expectations-docs', 1),
('dashboard-quality-assurance-topic', 'cur-dbt-tests', 2),
('dashboard-quality-assurance-topic', 'qe-owasp-web-security-testing', 3),
('data-analyst-role', 'mode-sql-tutorial', 1),
('data-analyst-role', 'power-bi-guidance', 2),
('data-catalogs-lineage', 'ibm-data-governance', 1),
('data-catalogs-lineage', 'data-dbt-intro', 2),
('data-contracts-awareness', 'ibm-data-governance', 1),
('data-contracts-awareness', 'google-data-analytics-certificate', 2),
('data-dictionary-authoring', 'ibm-data-governance', 1),
('data-dictionary-authoring', 'google-data-analytics-certificate', 2),
('data-dictionary-authoring', 'tableau-help', 3),
('data-literacy-and-ethics', 'storytelling-with-data-blog', 1),
('data-literacy-and-ethics', 'ibm-data-governance', 2),
('data-profiling', 'ibm-data-quality', 1),
('data-profiling', 'great-expectations-docs', 2),
('data-source-types', 'google-data-analytics-certificate', 1),
('data-source-types', 'ibm-data-analyst-certificate', 2),
('data-source-types', 'tableau-performance-recording', 3),
('data-type-and-format-standardization', 'pandas-missing-data', 1),
('data-type-and-format-standardization', 'pandas-text-data', 2),
('data-validation-rules', 'excel-data-validation', 1),
('data-validation-rules', 'great-expectations-docs', 2),
('dax-and-calculated-measures', 'power-bi-docs', 1),
('dax-and-calculated-measures', 'power-bi-training', 2),
('dax-and-calculated-measures', 'power-bi-dax', 3),
('descriptive-statistics-analysis', 'data-khan-statistics', 1),
('descriptive-statistics-analysis', 'google-data-analytics-certificate', 2),
('distributions-and-sampling', 'data-khan-statistics', 1),
('distributions-and-sampling', 'statistics-how-to-power', 2),
('duplicates-and-entity-resolution', 'great-expectations-docs', 1),
('duplicates-and-entity-resolution', 'google-ab-testing-course', 2),
('ecommerce-funnel-analysis', 'google-data-analytics-certificate', 1),
('ecommerce-funnel-analysis', 'shopify-analytics', 2),
('excel-formulas-functions', 'microsoft-excel-help', 1),
('excel-formulas-functions', 'excel-formulas-overview', 2),
('excel-formulas-functions', 'excel-xlookup', 3),
('executive-summary-writing', 'google-data-analytics-certificate', 1),
('executive-summary-writing', 'storytelling-with-data-blog', 2),
('finance-analytics-track', 'shopify-analytics', 1),
('finance-analytics-track', 'stripe-analytics-docs', 2),
('forecasting-awareness', 'sup-numpy-beginners', 1),
('forecasting-awareness', 'prophet-docs', 2),
('funnel-analysis', 'google-analytics-events', 1),
('funnel-analysis', 'shopify-analytics', 2),
('funnel-analysis', 'stripe-analytics-docs', 3),
('geospatial-analysis-awareness', 'google-data-analytics-certificate', 1),
('geospatial-analysis-awareness', 'tableau-visual-best-practices', 2),
('hypothesis-testing-basics', 'google-ab-testing-course', 1),
('hypothesis-testing-basics', 'openintro-statistics', 2),
('hypothesis-testing-basics', 'cur-dbt-tests', 3),
('insight-narrative', 'tableau-help', 1),
('insight-narrative', 'google-data-analytics-certificate', 2),
('interactive-python-data-apps', 'streamlit-docs', 1),
('interactive-python-data-apps', 'plotly-python', 2),
('interactive-python-data-apps', 'python-docs', 3),
('joins-and-relationships', 'sqlbolt', 1),
('joins-and-relationships', 'mode-sql-tutorial', 2),
('joins-and-relationships', 'kaggle-advanced-sql', 3),
('looker-awareness-path', 'looker-studio-help', 1),
('looker-awareness-path', 'looker-docs', 2),
('looker-studio-path', 'looker-studio-help', 1),
('looker-studio-path', 'looker-docs', 2),
('lookup-and-reference-functions', 'excel-xlookup', 1),
('lookup-and-reference-functions', 'sup-scipy-stats', 2),
('lookup-and-reference-functions', 'cur-excel-formulas', 3),
('marketing-analytics-track', 'google-analytics-help', 1),
('marketing-analytics-track', 'google-ab-testing-course', 2),
('metric-definitions', 'google-data-analytics-certificate', 1),
('metric-definitions', 'tableau-help', 2),
('metric-definitions', 'tableau-visual-best-practices', 3),
('misleading-visuals', 'khan-statistics', 1),
('misleading-visuals', 'openintro-statistics', 2),
('misleading-visuals', 'tableau-visual-best-practices', 3),
('missing-data-handling', 'great-expectations-docs', 1),
('missing-data-handling', 'pandas-missing-data', 2),
('notebook-workflows', 'python-tutorial', 1),
('notebook-workflows', 'python-docs', 2),
('notebook-workflows', 'statsmodels-docs', 3),
('numpy-for-analysis', 'sup-numpy-beginners', 1),
('numpy-for-analysis', 'python-docs', 2),
('numpy-for-analysis', 'sup-pandas-user-guide', 3),
('operations-analytics-track', 'google-analytics-help', 1),
('operations-analytics-track', 'scikit-learn-model-evaluation', 2),
('outliers-and-anomalies', 'ibm-data-quality', 1),
('outliers-and-anomalies', 'great-expectations-docs', 2),
('outliers-and-robust-summaries', 'openintro-statistics', 1),
('outliers-and-robust-summaries', 'khan-statistics', 2),
('outliers-and-robust-summaries', 'great-expectations-docs', 3),
('pandas-data-analysis', 'mode-sql-tutorial', 1),
('pandas-data-analysis', 'python-docs', 2),
('pandas-time-series-analysis', 'pandas-timeseries', 1),
('pandas-time-series-analysis', 'statsmodels-time-series', 2),
('pandas-time-series-analysis', 'prophet-docs', 3),
('pivot-tables', 'excel-pivottables-guide', 1),
('pivot-tables', 'data-excel-pivottable', 2),
('pivot-tables', 'microsoft-excel-help', 3),
('portfolio-dashboard-case-study', 'ibm-data-analyst-certificate', 1),
('portfolio-dashboard-case-study', 'tableau-help', 2),
('portfolio-dashboard-case-study', 'tableau-visual-best-practices', 3),
('portfolio-dashboard-case-study', 'data-kaggle-visualization', 4),
('portfolio-project-selection', 'google-data-analytics-certificate', 1),
('portfolio-project-selection', 'sqlbolt', 2),
('portfolio-project-selection', 'mode-sql-tutorial', 3),
('portfolio-project-selection', 'ibm-data-analyst-certificate', 4),
('power-bi-path', 'power-query-docs', 1),
('power-bi-path', 'power-bi-docs', 2),
('power-query-basics', 'power-query-docs', 1),
('power-query-basics', 'microsoft-excel-help', 2),
('power-query-basics', 'power-bi-docs', 3),
('practical-vs-statistical-significance', 'openintro-statistics', 1),
('practical-vs-statistical-significance', 'statistics-how-to-power', 2),
('predictive-modeling-awareness', 'scikit-learn-user-guide', 1),
('predictive-modeling-awareness', 'scikit-learn-model-evaluation', 2),
('presentation-delivery', 'google-data-analytics-certificate', 1),
('presentation-delivery', 'tableau-help', 2),
('privacy-sensitive-data', 'ibm-data-governance', 1),
('privacy-sensitive-data', 'nist-privacy-framework', 2),
('product-analytics-track', 'coursera-wharton-business-analytics', 1),
('product-analytics-track', 'google-analytics-help', 2),
('product-analytics-track', 'google-analytics-events', 3),
('proj-advanced-analytics-brief', 'google-data-analytics-certificate', 1),
('proj-advanced-analytics-brief', 'ibm-data-analyst-certificate', 2),
('proj-advanced-analytics-brief', 'coursera-wharton-business-analytics', 3),
('proj-advanced-analytics-brief', 'atlassian-stakeholder-management', 4),
('proj-advanced-analytics-brief', 'openintro-statistics', 5),
('proj-analytics-question-brief', 'google-data-analytics-certificate', 1),
('proj-analytics-question-brief', 'ibm-data-analyst-certificate', 2),
('proj-analytics-question-brief', 'coursera-wharton-business-analytics', 3),
('proj-analytics-question-brief', 'atlassian-stakeholder-management', 4),
('proj-analytics-question-brief', 'dbt-docs', 5),
('proj-bi-dashboard', 'tableau-help', 1),
('proj-bi-dashboard', 'power-bi-training', 2),
('proj-bi-dashboard', 'tableau-visual-best-practices', 3),
('proj-bi-dashboard', 'power-bi-guidance', 4),
('proj-bi-dashboard', 'data-kaggle-visualization', 5),
('proj-cleaning-pipeline-report', 'google-data-analytics-certificate', 1),
('proj-cleaning-pipeline-report', 'sqlbolt', 2),
('proj-cleaning-pipeline-report', 'mode-sql-tutorial', 3),
('proj-cleaning-pipeline-report', 'kaggle-intro-sql', 4),
('proj-cleaning-pipeline-report', 'ibm-data-analyst-certificate', 5),
('proj-dashboard-qa-checklist', 'tableau-performance-recording', 1),
('proj-dashboard-qa-checklist', 'power-bi-guidance', 2),
('proj-dashboard-qa-checklist', 'power-bi-docs', 3),
('proj-dashboard-qa-checklist', 'qe-owasp-web-security-testing', 4),
('proj-dashboard-qa-checklist', 'google-ab-testing-course', 5),
('proj-data-analyst-capstone', 'google-data-analytics-certificate', 1),
('proj-data-analyst-capstone', 'dbt-docs', 2),
('proj-data-analyst-capstone', 'power-bi-guidance', 3),
('proj-data-analyst-capstone', 'postgres-datetime-functions', 4),
('proj-data-analyst-capstone', 'ibm-data-analyst-certificate', 5),
('proj-data-analyst-capstone', 'mode-sql-tutorial', 6),
('proj-domain-analytics-case-study', 'google-data-analytics-certificate', 1),
('proj-domain-analytics-case-study', 'dbt-docs', 2),
('proj-domain-analytics-case-study', 'looker-docs', 3),
('proj-domain-analytics-case-study', 'ibm-data-analyst-certificate', 4),
('proj-ecommerce-funnel-case-study', 'google-analytics-events', 1),
('proj-ecommerce-funnel-case-study', 'shopify-analytics', 2),
('proj-ecommerce-funnel-case-study', 'stripe-analytics-docs', 3),
('proj-ecommerce-funnel-case-study', 'nngroup-analytics-reports', 4),
('proj-ecommerce-funnel-case-study', 'google-data-analytics-certificate', 5),
('proj-excel-sales-analysis', 'excel-pivottables-guide', 1),
('proj-excel-sales-analysis', 'microsoft-excel-help', 2),
('proj-excel-sales-analysis', 'data-excel-pivottable', 3),
('proj-excel-sales-analysis', 'excel-formulas-overview', 4),
('proj-excel-sales-analysis', 'google-data-analytics-certificate', 5),
('proj-executive-insight-deck', 'storytelling-with-data-blog', 1),
('proj-executive-insight-deck', 'tableau-performance-recording', 2),
('proj-executive-insight-deck', 'google-data-analytics-certificate', 3),
('proj-executive-insight-deck', 'data-kaggle-visualization', 4),
('proj-executive-insight-deck', 'ibm-data-analyst-certificate', 5),
('proj-governed-metric-dictionary', 'dbt-docs', 1),
('proj-governed-metric-dictionary', 'looker-docs', 2),
('proj-governed-metric-dictionary', 'ibm-data-analyst-certificate', 3),
('proj-governed-metric-dictionary', 'google-data-analytics-certificate', 4),
('proj-portfolio-presentation-video', 'nngroup-dashboards', 1),
('proj-portfolio-presentation-video', 'tableau-help', 2),
('proj-portfolio-presentation-video', 'tableau-visual-best-practices', 3),
('proj-portfolio-presentation-video', 'power-bi-guidance', 4),
('proj-portfolio-presentation-video', 'ibm-data-analyst-certificate', 5),
('proj-portfolio-presentation-video', 'google-data-analytics-certificate', 6),
('proj-public-data-api-mini-project', 'census-data-api', 1),
('proj-public-data-api-mini-project', 'world-bank-api', 2),
('proj-public-data-api-mini-project', 'python-docs', 3),
('proj-public-data-api-mini-project', 'qe-aspnet-minimal-api', 4),
('proj-public-data-api-mini-project', 'sup-owasp-api-top10', 5),
('proj-public-portfolio-package', 'great-expectations-docs', 1),
('proj-public-portfolio-package', 'ibm-data-analyst-certificate', 2),
('proj-public-portfolio-package', 'cur-github-readme', 3),
('proj-python-eda-notebook', 'python-tutorial', 1),
('proj-python-eda-notebook', 'great-expectations-docs', 2),
('proj-python-eda-notebook', 'cur-dbt-tests', 3),
('proj-python-eda-notebook', 'ibm-data-quality', 4),
('proj-python-eda-notebook', 'python-docs', 5),
('proj-python-report-automation', 'python-docs', 1),
('proj-python-report-automation', 'python-tutorial', 2),
('proj-python-report-automation', 'plotly-python', 3),
('proj-python-report-automation', 'sup-python-unittest', 4),
('proj-python-report-automation', 'sup-pytest-docs', 5),
('proj-spreadsheet-quality-audit', 'excel-data-validation', 1),
('proj-spreadsheet-quality-audit', 'great-expectations-docs', 2),
('proj-spreadsheet-quality-audit', 'cur-dbt-tests', 3),
('proj-spreadsheet-quality-audit', 'microsoft-excel-help', 4),
('proj-spreadsheet-quality-audit', 'excel-formulas-overview', 5),
('proj-sql-business-analysis', 'mode-sql-tutorial', 1),
('proj-sql-business-analysis', 'postgres-datetime-functions', 2),
('proj-sql-business-analysis', 'postgres-window-functions', 3),
('proj-sql-business-analysis', 'kaggle-advanced-sql', 4),
('proj-sql-business-analysis', 'kaggle-intro-sql', 5),
('proj-sql-cohort-retention-analysis', 'postgres-datetime-functions', 1),
('proj-sql-cohort-retention-analysis', 'postgres-window-functions', 2),
('proj-sql-cohort-retention-analysis', 'kaggle-advanced-sql', 3),
('proj-sql-cohort-retention-analysis', 'mode-sql-tutorial', 4),
('proj-sql-cohort-retention-analysis', 'sup-postgres-explain', 5),
('proj-statistical-insight-report', 'khan-statistics', 1),
('proj-statistical-insight-report', 'openintro-statistics', 2),
('proj-statistical-insight-report', 'statistics-how-to-power', 3),
('proj-statistical-insight-report', 'sup-scipy-stats', 4),
('proj-statistical-insight-report', 'google-data-analytics-certificate', 5),
('proj-survey-analysis-report', 'data-khan-statistics', 1),
('proj-survey-analysis-report', 'openintro-statistics', 2),
('proj-survey-analysis-report', 'statistics-how-to-power', 3),
('proj-survey-analysis-report', 'google-data-analytics-certificate', 4),
('public-data-api-extraction', 'census-data-api', 1),
('public-data-api-extraction', 'world-bank-api', 2),
('public-data-api-extraction', 'python-docs', 3),
('python-basics-for-analysts', 'python-tutorial', 1),
('python-basics-for-analysts', 'python-docs', 2),
('python-basics-for-analysts', 'scipy-stats', 3),
('python-report-automation', 'great-expectations-docs', 1),
('python-report-automation', 'ibm-data-analyst-certificate', 2),
('python-report-automation', 'python-docs', 3),
('python-visualization', 'plotly-python', 1),
('python-visualization', 'ibm-data-analyst-certificate', 2),
('query-debugging-performance', 'mode-sql-tutorial', 1),
('query-debugging-performance', 'sqlbolt', 2),
('query-debugging-performance', 'kaggle-advanced-sql', 3),
('query-debugging-performance', 'postgres-window-functions', 4),
('regression-awareness', 'openintro-statistics', 1),
('regression-awareness', 'scipy-stats', 2),
('regression-awareness', 'statsmodels-docs', 3),
('relational-data-models', 'sqlbolt', 1),
('relational-data-models', 'mode-sql-tutorial', 2),
('relational-data-models', 'kaggle-advanced-sql', 3),
('reproducible-cleaning-workflows', 'sqlbolt', 1),
('reproducible-cleaning-workflows', 'mode-sql-tutorial', 2),
('requirements-intake', 'atlassian-stakeholder-management', 1),
('requirements-intake', 'coursera-wharton-business-analytics', 2),
('resume-and-project-positioning', 'cur-github-readme', 1),
('resume-and-project-positioning', 'ibm-data-analyst-certificate', 2),
('revenue-and-pricing-analysis', 'google-data-analytics-certificate', 1),
('revenue-and-pricing-analysis', 'stripe-analytics-docs', 2),
('sales-analytics-track', 'tableau-training', 1),
('sales-analytics-track', 'dbt-tests', 2),
('sample-size-and-power-basics', 'power-bi-training', 1),
('sample-size-and-power-basics', 'openintro-statistics', 2),
('sample-size-and-power-basics', 'statistics-how-to-power', 3),
('schema-drift-and-source-changes', 'google-data-analytics-certificate', 1),
('schema-drift-and-source-changes', 'great-expectations-docs', 2),
('segmentation-and-clustering-awareness', 'nist-privacy-framework', 1),
('segmentation-and-clustering-awareness', 'ml-sklearn-metrics', 2),
('select-filter-sort', 'sqlbolt', 1),
('select-filter-sort', 'mode-sql-tutorial', 2),
('select-filter-sort', 'kaggle-advanced-sql', 3),
('semantic-models-and-metrics', 'qe-opentelemetry-observability', 1),
('semantic-models-and-metrics', 'power-bi-docs', 2),
('small-multiples-and-faceting', 'storytelling-with-data-blog', 1),
('small-multiples-and-faceting', 'data-kaggle-visualization', 2),
('spreadsheet-charts', 'microsoft-excel-help', 1),
('spreadsheet-charts', 'excel-formulas-overview', 2),
('spreadsheet-charts', 'data-kaggle-visualization', 3),
('spreadsheet-cleaning', 'qe-kaggle-data-cleaning', 1),
('spreadsheet-cleaning', 'excel-formulas-overview', 2),
('spreadsheet-cleaning', 'excel-pivottables-guide', 3),
('spreadsheet-data-validation', 'excel-data-validation', 1),
('spreadsheet-data-validation', 'great-expectations-docs', 2),
('spreadsheet-lookup-patterns', 'google-data-analytics-certificate', 1),
('spreadsheet-lookup-patterns', 'excel-xlookup', 2),
('spreadsheet-lookup-patterns', 'microsoft-excel-help', 3),
('spreadsheet-structure', 'microsoft-excel-help', 1),
('spreadsheet-structure', 'excel-xlookup', 2),
('spreadsheet-structure', 'cur-pandas-merge', 3),
('sql-data-quality-checks', 'sqlbolt', 1),
('sql-data-quality-checks', 'mode-sql-tutorial', 2),
('sql-data-quality-checks', 'kaggle-advanced-sql', 3),
('sql-date-time-analysis', 'postgres-datetime-functions', 1),
('sql-date-time-analysis', 'postgres-window-functions', 2),
('sql-date-time-analysis', 'kaggle-advanced-sql', 3),
('sql-interview-practice', 'sqlbolt', 1),
('sql-interview-practice', 'mode-sql-tutorial', 2),
('sql-interview-practice', 'kaggle-advanced-sql', 3),
('sql-interview-practice', 'kaggle-intro-sql', 4),
('stakeholder-interview-questions', 'google-data-analytics-certificate', 1),
('stakeholder-interview-questions', 'ibm-data-analyst-certificate', 2),
('stakeholder-interview-questions', 'coursera-wharton-business-analytics', 3),
('survey-analysis-basics', 'data-khan-statistics', 1),
('survey-analysis-basics', 'openintro-statistics', 2),
('survey-analysis-basics', 'sup-scipy-stats', 3),
('tableau-path', 'tableau-training', 1),
('tableau-path', 'tableau-help', 2),
('tableau-path', 'tableau-visual-best-practices', 3),
('text-cleaning-and-standardization', 'pandas-text-data', 1),
('text-cleaning-and-standardization', 'google-data-analytics-certificate', 2),
('time-series-decomposition-awareness', 'pandas-timeseries', 1),
('time-series-decomposition-awareness', 'statsmodels-time-series', 2),
('time-series-decomposition-awareness', 'prophet-docs', 3),
('time-series-decomposition-awareness', 'google-data-analytics-certificate', 4),
('version-control-for-analysis', 'sqlbolt', 1),
('version-control-for-analysis', 'mode-sql-tutorial', 2),
('version-control-for-analysis', 'sup-pro-git-branching', 3),
('visual-design-principles', 'storytelling-with-data-blog', 1),
('visual-design-principles', 'tableau-training', 2),
('visual-design-principles', 'data-kaggle-visualization', 3),
('window-functions', 'postgres-window-functions', 1),
('window-functions', 'mode-sql-tutorial', 2),
('window-functions', 'postgres-datetime-functions', 3),
('window-functions', 'kaggle-advanced-sql', 4),
('aggregation-and-grouping', 'sup-postgres-explain', 3),
('descriptive-statistics-analysis', 'sup-scipy-stats', 3),
('distributions-and-sampling', 'sup-scipy-stats', 3),
('confidence-intervals', 'statistics-how-to-power', 3),
('data-profiling', 'google-ab-testing-course', 3),
('missing-data-handling', 'google-ab-testing-course', 3),
('duplicates-and-entity-resolution', 'cur-dbt-tests', 3),
('outliers-and-anomalies', 'google-ab-testing-course', 3),
('reproducible-cleaning-workflows', 'ibm-data-analyst-certificate', 3),
('dashboard-purpose-and-layout', 'data-kaggle-visualization', 3),
('insight-narrative', 'ibm-data-analyst-certificate', 3),
('presentation-delivery', 'ibm-data-analyst-certificate', 3),
('dashboard-data-modeling', 'power-bi-training', 3),
('dashboard-interactivity', 'data-kaggle-visualization', 3),
('python-visualization', 'data-kaggle-visualization', 3),
('analytics-documentation', 'google-data-analytics-certificate', 3),
('predictive-modeling-awareness', 'qe-deepeval-docs', 3),
('anomaly-detection-awareness', 'google-ab-testing-course', 3),
('proj-public-portfolio-package', 'qe-github-pages', 4),
('spreadsheet-data-validation', 'google-data-analytics-certificate', 3),
('practical-vs-statistical-significance', 'sup-scipy-stats', 3),
('text-cleaning-and-standardization', 'ibm-data-analyst-certificate', 3),
('schema-drift-and-source-changes', 'ibm-data-analyst-certificate', 3),
('small-multiples-and-faceting', 'google-data-analytics-certificate', 3),
('executive-summary-writing', 'ibm-data-analyst-certificate', 3),
('semantic-models-and-metrics', 'qe-google-sre-monitoring', 3),
('ecommerce-funnel-analysis', 'ibm-data-analyst-certificate', 3),
('customer-retention-analysis', 'ibm-data-analyst-certificate', 3),
('revenue-and-pricing-analysis', 'ibm-data-analyst-certificate', 3),
('data-contracts-awareness', 'ibm-data-analyst-certificate', 3),
('dashboard-ownership-and-maintenance', 'google-data-analytics-certificate', 3),
('causal-inference-caution', 'tableau-training', 3)
ON CONFLICT (node_key, resource_key) DO NOTHING;

WITH resolved_node_resource AS (
    SELECT DISTINCT ON (rn.roadmap_node_id, lower(lr.url))
        rn.roadmap_node_id,
        lr.learning_resource_id
    FROM seed_node_resource snr
    JOIN seed_node sn ON sn.node_key = snr.node_key
    JOIN seed_roadmap_map m ON true
    JOIN public.roadmap_node rn
        ON rn.roadmap_version_id = m.roadmap_version_id
       AND rn.slug = sn.node_key
    JOIN seed_resource sr ON sr.resource_key = snr.resource_key
    JOIN LATERAL (
        SELECT lr.learning_resource_id, lr.url
        FROM public.learning_resource lr
        WHERE lr.url = sr.url
        ORDER BY lr.learning_resource_id
        LIMIT 1
    ) lr ON true
    ORDER BY rn.roadmap_node_id, lower(lr.url), snr.order_index
)
INSERT INTO public.roadmap_node_resource (roadmap_node_id, learning_resource_id)
SELECT roadmap_node_id, learning_resource_id
FROM resolved_node_resource
ON CONFLICT (roadmap_node_id, learning_resource_id) DO NOTHING;

WITH resolved_resource_skill AS (
    SELECT DISTINCT
        lr.learning_resource_id,
        s.skill_id
    FROM seed_node_resource snr
    JOIN seed_node_skill sns ON sns.node_key = snr.node_key
    JOIN seed_resource sr ON sr.resource_key = snr.resource_key
    JOIN LATERAL (
        SELECT lr.learning_resource_id
        FROM public.learning_resource lr
        WHERE lr.url = sr.url
        ORDER BY lr.learning_resource_id
        LIMIT 1
    ) lr ON true
    JOIN public.skill s ON s.slug = sns.skill_slug
)
INSERT INTO public.learning_resource_skill (learning_resource_id, skill_id)
SELECT learning_resource_id, skill_id
FROM resolved_resource_skill
ON CONFLICT (learning_resource_id, skill_id) DO NOTHING;

WITH duplicate_node_resource AS (
    SELECT
        rnr.roadmap_node_resource_id,
        row_number() OVER (
            PARTITION BY rnr.roadmap_node_id, lower(lr.url)
            ORDER BY rnr.roadmap_node_resource_id
        ) AS duplicate_rank
    FROM public.roadmap_node_resource rnr
    JOIN public.roadmap_node rn ON rn.roadmap_node_id = rnr.roadmap_node_id
    JOIN public.learning_resource lr ON lr.learning_resource_id = rnr.learning_resource_id
    JOIN seed_roadmap_map m ON m.roadmap_version_id = rn.roadmap_version_id
)
DELETE FROM public.roadmap_node_resource rnr
USING duplicate_node_resource dnr
WHERE rnr.roadmap_node_resource_id = dnr.roadmap_node_resource_id
  AND dnr.duplicate_rank > 1;

WITH duplicate_resource_skill AS (
    SELECT
        lrs.learning_resource_skill_id,
        row_number() OVER (
            PARTITION BY lower(lr.url), lrs.skill_id
            ORDER BY lrs.learning_resource_skill_id
        ) AS duplicate_rank
    FROM public.learning_resource_skill lrs
    JOIN public.learning_resource lr ON lr.learning_resource_id = lrs.learning_resource_id
    JOIN seed_resource sr ON sr.url = lr.url
)
DELETE FROM public.learning_resource_skill lrs
USING duplicate_resource_skill drs
WHERE lrs.learning_resource_skill_id = drs.learning_resource_skill_id
  AND drs.duplicate_rank > 1;

DROP TABLE IF EXISTS seed_edge;
CREATE TEMP TABLE seed_edge (from_key text NOT NULL, to_key text NOT NULL, edge_type text NOT NULL, dependency_type text NOT NULL, condition jsonb NOT NULL) ON COMMIT DROP;
INSERT INTO seed_edge VALUES
('ph-da-foundations', 'data-source-types', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-da-foundations', 'analytical-deliverable-types', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-da-foundations', 'stakeholder-interview-questions', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-spreadsheets', 'spreadsheet-data-validation', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-spreadsheets', 'conditional-formatting-analysis', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-spreadsheets', 'spreadsheet-lookup-patterns', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-sql-databases', 'sql-date-time-analysis', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-sql-databases', 'cohort-retention-sql', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-sql-databases', 'analyst-dimensional-modeling', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-statistics', 'outliers-and-robust-summaries', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-statistics', 'sample-size-and-power-basics', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-statistics', 'practical-vs-statistical-significance', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-data-cleaning-preparation', 'text-cleaning-and-standardization', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-data-cleaning-preparation', 'schema-drift-and-source-changes', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-data-cleaning-preparation', 'data-dictionary-authoring', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-visualization-storytelling', 'chart-accessibility', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-visualization-storytelling', 'small-multiples-and-faceting', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-visualization-storytelling', 'executive-summary-writing', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-bi-tools', 'semantic-models-and-metrics', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-bi-tools', 'dax-and-calculated-measures', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-bi-tools', 'dashboard-performance-and-refresh', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-bi-tools', 'dashboard-quality-assurance-topic', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-python-analytics', 'pandas-time-series-analysis', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-python-analytics', 'python-report-automation', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-python-analytics', 'public-data-api-extraction', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-python-analytics', 'interactive-python-data-apps', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-domain-analytics', 'ecommerce-funnel-analysis', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-domain-analytics', 'customer-retention-analysis', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-domain-analytics', 'revenue-and-pricing-analysis', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-governance-collaboration', 'analytics-qa-review-process', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-governance-collaboration', 'data-contracts-awareness', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-governance-collaboration', 'dashboard-ownership-and-maintenance', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-advanced-analytics-awareness', 'time-series-decomposition-awareness', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-advanced-analytics-awareness', 'causal-inference-caution', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-advanced-analytics-awareness', 'survey-analysis-basics', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-portfolio-capstone', 'analyst-take-home-challenges', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-portfolio-capstone', 'portfolio-dashboard-case-study', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-portfolio-capstone', 'capstone-presentation-walkthrough', 'contains', 'required', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-spreadsheets', 'proj-spreadsheet-quality-audit', 'contains', 'optional', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-sql-databases', 'proj-sql-cohort-retention-analysis', 'contains', 'optional', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-bi-tools', 'proj-dashboard-qa-checklist', 'contains', 'optional', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-python-analytics', 'proj-python-report-automation', 'contains', 'optional', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-domain-analytics', 'proj-ecommerce-funnel-case-study', 'contains', 'optional', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-advanced-analytics-awareness', 'proj-survey-analysis-report', 'contains', 'optional', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-python-analytics', 'proj-public-data-api-mini-project', 'contains', 'optional', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('ph-portfolio-capstone', 'proj-portfolio-presentation-video', 'contains', 'optional', '{"rule": "parent_contains_child", "source": "curated-expansion"}'::jsonb),
('sql-date-time-analysis', 'cohort-retention-sql', 'dependency', 'required', '{"rule": "source_completed", "source": "curated-expansion"}'::jsonb),
('cohort-retention-sql', 'proj-sql-cohort-retention-analysis', 'dependency', 'required', '{"rule": "source_completed", "source": "curated-expansion"}'::jsonb),
('semantic-models-and-metrics', 'dax-and-calculated-measures', 'dependency', 'required', '{"rule": "source_completed", "source": "curated-expansion"}'::jsonb),
('dashboard-quality-assurance-topic', 'proj-dashboard-qa-checklist', 'dependency', 'required', '{"rule": "source_completed", "source": "curated-expansion"}'::jsonb),
('pandas-time-series-analysis', 'python-report-automation', 'dependency', 'required', '{"rule": "source_completed", "source": "curated-expansion"}'::jsonb),
('public-data-api-extraction', 'proj-public-data-api-mini-project', 'dependency', 'required', '{"rule": "source_completed", "source": "curated-expansion"}'::jsonb),
('ecommerce-funnel-analysis', 'proj-ecommerce-funnel-case-study', 'dependency', 'required', '{"rule": "source_completed", "source": "curated-expansion"}'::jsonb),
('survey-analysis-basics', 'proj-survey-analysis-report', 'dependency', 'required', '{"rule": "source_completed", "source": "curated-expansion"}'::jsonb),
('portfolio-dashboard-case-study', 'proj-data-analyst-capstone', 'dependency', 'required', '{"rule": "source_completed", "source": "curated-expansion"}'::jsonb),
('capstone-presentation-walkthrough', 'proj-data-analyst-capstone', 'dependency', 'required', '{"rule": "source_completed", "source": "curated-expansion"}'::jsonb);

INSERT INTO public.roadmap_edge (roadmap_version_id, from_node_id, to_node_id, edge_type, dependency_type, condition)
SELECT DISTINCT ON (m.roadmap_version_id, source.roadmap_node_id, target.roadmap_node_id, sve.edge_type)
       m.roadmap_version_id, source.roadmap_node_id, target.roadmap_node_id, sve.edge_type, sve.dependency_type, sve.condition
FROM seed_edge sve
JOIN seed_roadmap_map m ON true
JOIN public.roadmap_node source ON source.roadmap_version_id = m.roadmap_version_id AND source.slug = sve.from_key
JOIN public.roadmap_node target ON target.roadmap_version_id = m.roadmap_version_id AND target.slug = sve.to_key
ORDER BY m.roadmap_version_id, source.roadmap_node_id, target.roadmap_node_id, sve.edge_type, sve.dependency_type
ON CONFLICT (roadmap_version_id, from_node_id, to_node_id, edge_type) DO UPDATE SET dependency_type = EXCLUDED.dependency_type,
    condition = EXCLUDED.condition;

COMMIT;

-- Expansion summary: added 38 topics, 8 optional projects, 48 learning resources, 100 node-resource mappings, and 56 edges.
