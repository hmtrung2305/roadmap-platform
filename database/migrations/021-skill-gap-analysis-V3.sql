CREATE TABLE IF NOT EXISTS assessment_level
(
    assessment_level_id UUID PRIMARY KEY,

    career_role_id UUID NOT NULL,

    name VARCHAR(50) NOT NULL,

	slug VARCHAR(50) NOT NULL, 

    sort_order INT NOT NULL,

    created_at TIMESTAMPTZ DEFAULT NOW(),

	CONSTRAINT uq_assessment_level_slug
    	UNIQUE(career_role_id, slug),	
	
    CONSTRAINT fk_assessment_level_career_role
        FOREIGN KEY (career_role_id)
        REFERENCES career_role(career_role_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS assessment_level_group
(
    assessment_level_group_id UUID PRIMARY KEY,

    assessment_level_id UUID NOT NULL,

    roadmap_node_id UUID NOT NULL,

	CONSTRAINT uq_assessment_level_group
	UNIQUE
	(
    assessment_level_id,
    roadmap_node_id
	),

    CONSTRAINT fk_assessment_level_group_level
        FOREIGN KEY (assessment_level_id)
        REFERENCES assessment_level(assessment_level_id) ON DELETE CASCADE,

    CONSTRAINT fk_assessment_level_group_node
        FOREIGN KEY (roadmap_node_id)
        REFERENCES roadmap_node(roadmap_node_id) ON DELETE CASCADE
);











