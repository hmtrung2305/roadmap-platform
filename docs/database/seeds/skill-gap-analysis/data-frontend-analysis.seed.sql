-- =============================================
-- FRONTEND DEVELOPER SKILL GAP ANALYSIS
-- =============================================

-- =============================================
-- 1. Skill Group
-- =============================================
INSERT INTO skill_group
(
    name,
    slug,
    completion_rule,
    required_skill_count,
    description
)
VALUES

(
    'Internet Fundamentals',
    'internet-fundamentals',
    'ALL',
    NULL,
    'Browser, HTTP, DNS and hosting fundamentals.'
),

(
    'HTML & Accessibility',
    'html-accessibility',
    'COUNT',
    3,
    'HTML structure, forms and accessibility.'
),

(
    'CSS & Responsive Design',
    'css-responsive-design',
    'COUNT',
    3,
    'CSS layouts, responsive design and styling.'
),

(
    'JavaScript Fundamentals',
    'javascript-fundamentals',
    'COUNT',
    4,
    'Core JavaScript and browser APIs.'
),

(
    'Frameworks & SPA Development',
    'frameworks-spa-development',
    'COUNT',
    3,
    'Modern frontend frameworks and SPA development.'
),

(
    'Package Management & Tooling',
    'package-management-tooling',
    'COUNT',
    3,
    'Package managers and development tooling.'
),

(
    'API Integration',
    'api-integration',
    'COUNT',
    2,
    'Frontend communication with backend services.'
),

(
    'Security',
    'security',
    'COUNT',
    3,
    'Frontend security and authentication.'
),

(
    'Testing & Quality',
    'testing-quality',
    'COUNT',
    3,
    'Testing strategies and code quality.'
),

(
    'Performance & Production',
    'performance-production',
    'COUNT',
    4,
    'Performance optimization and deployment.'
);



-- =============================================
-- 2. Career Role Skill Group
-- =============================================
INSERT INTO career_role_skill_group
(
    career_role_id,
    skill_group_id,
    priority
)
SELECT
    cr.career_role_id,
    sg.skill_group_id,
    p.priority
FROM
(
    VALUES
        ('internet-fundamentals', 1),
        ('html-accessibility', 1),
        ('css-responsive-design', 1),
        ('javascript-fundamentals', 1),

        ('frameworks-spa-development', 2),
        ('package-management-tooling', 2),
        ('api-integration', 2),
        ('security', 2),

        ('testing-quality', 3),
        ('performance-production', 3)

) AS p(slug, priority)

JOIN skill_group sg
    ON sg.slug = p.slug

JOIN career_role cr
    ON cr.slug = 'frontend-developer';

	



-- =============================================
-- 3. Skill Group Item
-- =============================================

-- =============================================
-- 3.1 Internet Fundamentals
-- =============================================
INSERT INTO skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM skill_group sg
JOIN skill s ON s.slug IN
(
    'how-browsers-work',
    'http-fundamentals',
    'domain-names-and-dns',
    'hosting-basics',
    'browser-compatibility'
)
WHERE sg.slug = 'internet-fundamentals';
-- =============================================
-- 3.2 HTML & Accessibility
-- =============================================
INSERT INTO skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM skill_group sg
JOIN skill s ON s.slug IN
(
    'html-basics',
    'forms-and-validation',
    'html-templates',
    'progressive-enhancement',
    'writing-semantic-html'
)
WHERE sg.slug = 'html-accessibility';
-- =============================================
-- 3.3 CSS & Responsive Design
-- =============================================
INSERT INTO skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM skill_group sg
JOIN skill s ON s.slug IN
(
    'css-basics',
    'making-layouts',
    'box-model-cascade-and-specificity',
    'bem-css-architecture',
    'design-tokens-and-theming',
    'component-libraries-and-design-systems'
)
WHERE sg.slug = 'css-responsive-design';
-- =============================================
-- 3.4 JavaScript Fundamentals
-- =============================================
INSERT INTO skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM skill_group sg
JOIN skill s ON s.slug IN
(
    'javascript-basics',
    'javascript-modules',
    'dom-manipulation',
    'asynchronous-javascript',
    'fetch-api-ajax',
    'browser-storage-basics'
)
WHERE sg.slug = 'javascript-fundamentals';
-- =============================================
-- 3.5 Frameworks & SPA Development
-- =============================================
INSERT INTO skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM skill_group sg
JOIN skill s ON s.slug IN
(
    'frameworks-fundamentals',
    'react',
    'angular',
    'client-side-routing',
    'component-composition',
    'forms-in-frameworks'
)
WHERE sg.slug = 'frameworks-spa-development';
-- =============================================
-- 3.6 Package Management & Tooling
-- =============================================
INSERT INTO skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM skill_group sg
JOIN skill s ON s.slug IN
(
    'npm',
    'pnpm',
    'eslint-and-prettier',
    'jest-and-vitest',
	'vite'
)
WHERE sg.slug = 'package-management-tooling';
-- =============================================
-- 3.7 API Integration
-- =============================================
INSERT INTO skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM skill_group sg
JOIN skill s ON s.slug IN
(
    'fetch-api-ajax',
    'apollo-client',
	'graphql'
)
WHERE sg.slug = 'api-integration';
-- =============================================
-- 3.8 Security
-- =============================================
INSERT INTO skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM skill_group sg
JOIN skill s ON s.slug IN
(
    'owasp-security-risks',
    'content-security-policy',
    'protected-routes-and-auth-ui',
    'authentication-strategies',
    'jwt-oauth-and-sso'
)
WHERE sg.slug = 'security';
-- =============================================
-- 3.9 Testing & Quality
-- =============================================
INSERT INTO skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM skill_group sg
JOIN skill s ON s.slug IN
(
    'playwright',
    'cypress',
    'accessibility-testing',
    'frontend-code-review',
    'component-documentation-and-storybook',
	'testing-library',
	'testing-fundamentals'
)
WHERE sg.slug = 'testing-quality';
-- =============================================
-- 3.10 Performance & Production
-- =============================================

INSERT INTO skill_group_item(skill_group_id, skill_id)
SELECT sg.skill_group_id, s.skill_id
FROM skill_group sg
JOIN skill s ON s.slug IN
(
    'performance-fundamentals',
    'performance-metrics-and-core-web-vitals',
    'asset-and-bundle-optimization',
    'prpl-pattern',
    'frontend-error-monitoring',
    'frontend-deployment',
	'using-devtools',
	'using-lighthouse'
)
WHERE sg.slug = 'performance-production';











