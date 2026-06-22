ALTER TABLE skill_gap_analysis_history
DROP COLUMN readiness_percent;

ALTER TABLE skill_gap_analysis_history
DROP COLUMN skill_coverage_percent;

ALTER TABLE skill_gap_analysis_history
ADD COLUMN level_name VARCHAR(50) NOT NULL DEFAULT '';

ALTER TABLE skill_gap_analysis_history
ADD COLUMN level_slug VARCHAR(50) NOT NULL DEFAULT '';

ALTER TABLE skill_gap_analysis_history
ADD COLUMN matched_skills INT NOT NULL DEFAULT 0;

ALTER TABLE skill_gap_analysis_history
ADD COLUMN total_skills INT NOT NULL DEFAULT 0;

ALTER TABLE skill_gap_analysis_history
ADD COLUMN missing_skills INT NOT NULL DEFAULT 0;