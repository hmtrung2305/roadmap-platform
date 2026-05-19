-- Table: public.User

DROP SCHEMA public CASCADE;
CREATE SCHEMA public;
GRANT ALL ON SCHEMA public TO postgres;
GRANT ALL ON SCHEMA public TO public;

-- Table: public.User, public.Role, public.Permission

DROP TABLE IF EXISTS public."Permission";
CREATE TABLE IF NOT EXISTS public."Permission"
(
	permission_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	permission_name varchar(50)
);

DROP TABLE IF EXISTS public."Role";
CREATE TABLE IF NOT EXISTS public."Role"
(
    role_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    role_name varchar(15)
);

DROP TABLE IF EXISTS public."User";
CREATE TABLE IF NOT EXISTS public."User"
(
    user_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    name varchar(100),
	gender bool, -- true-M, false-FM
	phone varchar(10),
	status varchar(20) DEFAULT 'active',
	birthdate date,
	created_at timestamptz DEFAULT now(),
	updated_at timestamptz DEFAULT now()
);

DROP TABLE IF EXISTS public."Permission_Role";
CREATE TABLE IF NOT EXISTS public."Permission_Role"
(
	id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	permission_id uuid NOT NULL,
	role_id uuid NOT NULL,

	CONSTRAINT fk_permission_id
		FOREIGN KEY (permission_id)
		REFERENCES "Permission"(permission_id),
	CONSTRAINT fk_role_id
		FOREIGN KEY (role_id)
		REFERENCES "Role"(role_id)
);

DROP TABLE IF EXISTS public."User_Role";
CREATE TABLE IF NOT EXISTS public."User_Role"
(
	id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	role_id uuid NOT NULL,
	user_id uuid NOT NULL,

	CONSTRAINT fk_user_id
		FOREIGN KEY (user_id)
		REFERENCES "User"(user_id),
	CONSTRAINT fk_role_id
		FOREIGN KEY (role_id)
		REFERENCES "Role"(role_id)
);

DROP TABLE IF EXISTS public."User_Auth_Provider";
CREATE TABLE IF NOT EXISTS public."User_Auth_Provider"
(
	id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	user_id uuid NOT NULL,
	email varchar(100),
	password_hash text,
	provider varchar(100) UNIQUE NOT NULL,
	provider_user_id varchar(255) NOT NULL,
	created_at timestamptz DEFAULT now()

	CONSTRAINT fk_user_id
		FOREIGN KEY (user_id)
		REFERENCES "User"(user_id)
);

-- Table: Roadmap

DROP TABLE IF EXISTS public."Specialty";
CREATE TABLE IF NOT EXISTS public."Specialty"
(
	specialty_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	specialty_name varchar(100),
	description text,
	created_at timestamptz DEFAULT now()
); 

DROP TABLE IF EXISTS public."Roadmap";
CREATE TABLE IF NOT EXISTS public."Roadmap"
(
	roadmap_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	specialty_id uuid NOT NULL,
	roadmap_name varchar(100),
	version int DEFAULT 1,
	is_active bool DEFAULT 'true',
	created_at timestamptz DEFAULT now(),

	CONSTRAINT fk_specialty_id
		FOREIGN KEY (specialty_id)
		REFERENCES "Specialty"(specialty_id)
);

DROP TABLE IF EXISTS public."Roadmap_Node";
CREATE TABLE IF NOT EXISTS public."Roadmap_Node"
(
	node_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	roadmap_id uuid NOT NULL,
	title varchar(50),
	position_x float,
	position_y float,
	description text,
	is_mandatory bool DEFAULT true,

	CONSTRAINT fk_roadmap_id
		FOREIGN KEY (roadmap_id)
		REFERENCES "Roadmap"(roadmap_id)
);

DROP TABLE IF EXISTS public."Roadmap_Edge";
CREATE TABLE IF NOT EXISTS public."Roadmap_Edge"
(
	edge_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	roadmap_id uuid NOT NULL,
	ancestor_node_id uuid NOT NULL,
	descendant_node_id uuid NOT NULL,

	CONSTRAINT fk_roadmap_id
		FOREIGN KEY (roadmap_id)
		REFERENCES "Roadmap"(roadmap_id),
	CONSTRAINT fk_ancestor_node_id
		FOREIGN KEY (ancestor_node_id)
		REFERENCES "Roadmap_Node"(node_id),
	CONSTRAINT fk_descendant_node_id
		FOREIGN KEY (descendant_node_id)
		REFERENCES "Roadmap_Node"(node_id)
);

DROP TABLE IF EXISTS public."Skill";
CREATE TABLE IF NOT EXISTS public."Skill"
(
	skill_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	skill_name varchar(100) NOT NULL,
	description text
);

DROP TABLE IF EXISTS public."Node_Skill";
CREATE TABLE IF NOT EXISTS public."Node_Skill"
(
	node_skill_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	roadmap_node_id uuid NOT NULL,
	skill_id uuid NOT NULL,

	CONSTRAINT fk_roadmap_node_id
		FOREIGN KEY (roadmap_node_id)
		REFERENCES "Roadmap_Node"(node_id),
	CONSTRAINT fk_skill_id
		FOREIGN KEY (skill_id)
		REFERENCES "Skill"(skill_id)
);

DROP TABLE IF EXISTS public."My_Resource";
CREATE TABLE IF NOT EXISTS public."My_Resource"
(
	resource_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	skill_id uuid NOT NULL,
	title varchar(100),
	url varchar(100),
	created_at timestamptz default now(),
	metadata jsonb,

	CONSTRAINT fk_skill_id
		FOREIGN KEY (skill_id)
		REFERENCES "Skill"(skill_id)
);

DROP TABLE IF EXISTS public."Learning_Resource";
CREATE TABLE IF NOT EXISTS public."Learning_Resource"
(
	resource_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	skill_id uuid NOT NULL,
	title varchar(100),
	url varchar(100),
	resource_type varchar(100),
	porvider varchar(100),
	created_at timestamptz default now(),
	metadata jsonb,

	CONSTRAINT fk_skill_id
		FOREIGN KEY (skill_id)
		REFERENCES "Skill"(skill_id)
);

DROP TABLE IF EXISTS public."User_Roadmap_Status";
CREATE TABLE IF NOT EXISTS public."User_Roadmap_Status"
(
	enrollment_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	user_id uuid NOT NULL,
	roadmap_id uuid NOT NULL,
	last_time timestamptz DEFAULT now(),
	status varchar(100),

	CONSTRAINT fk_user_id
		FOREIGN KEY (user_id)
		REFERENCES "User"(user_id),
	CONSTRAINT fk_roadmap_id
		FOREIGN KEY (roadmap_id)
		REFERENCES "Roadmap"(roadmap_id)
);

DROP TABLE IF EXISTS public."User_Skill_Progress";
CREATE TABLE IF NOT EXISTS public."User_Skill_Progress"
(
	progress_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	user_id uuid NOT NULL,
	skill_id uuid NOT NULL,
	status varchar(100),
	unlock_method jsonb, -- "studied", "profile", "github_insight"
	updated_at timestamptz DEFAULT now(),

	CONSTRAINT fk_user_id
		FOREIGN KEY (user_id)
		REFERENCES "User"(user_id),
	CONSTRAINT fk_skill_id
		FOREIGN KEY (skill_id)
		REFERENCES "Skill"(skill_id)
);

-- Table: Chatbot AI

DROP TABLE IF EXISTS public."Conversation";
CREATE TABLE IF NOT EXISTS public."Conversation"
(
	conversation_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	user_id uuid NOT NULL,
	resource_id uuid NOT NULL,
	title varchar(100),
	created_at timestamptz DEFAULT now(),
	updated_at timestamptz DEFAULT now(),

	CONSTRAINT fk_user_id
		FOREIGN KEY (user_id)
		REFERENCES "User"(user_id),
	CONSTRAINT fk_resource_id
		FOREIGN KEY (resource_id)
		REFERENCES "Learning_Resource"(resource_id)
);

DROP TABLE IF EXISTS public."Chatbot_Message";
CREATE TABLE IF NOT EXISTS public."Chatbot_Message"
(
	request_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	conversation_id uuid NOT NULL,
	content_message varchar(100),
	metadata jsonb,

	CONSTRAINT fk_conversation_id
		FOREIGN KEY (conversation_id)
		REFERENCES "Conversation"(conversation_id)
);

DROP TABLE IF EXISTS public."User_Insight";
CREATE TABLE IF NOT EXISTS public."User_Insight"
(
	insight_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	user_id uuid NOT NULL,
	metadata jsonb,

	CONSTRAINT fk_user_Id
		FOREIGN KEY (user_id)
		REFERENCES "User"(user_id)
);

-- Table: Repo_Github

DROP TABLE IF EXISTS public."Repository";
CREATE TABLE IF NOT EXISTS public."Repository"
(
	repository_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	user_id uuid NOT NULL,
	repo_name varchar(100),
	repo_url varchar(100),
	description text,
	created_at timestamptz DEFAULT now(),

	CONSTRAINT fk_user_id
		FOREIGN KEY (user_id)
		REFERENCES "User"(user_id)
);

DROP TABLE IF EXISTS public."Repo_Insight";
CREATE TABLE IF NOT EXISTS public."Repo_Insight"
(
	insight_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	repository_id uuid NOT NULL,
	summary text,
	tech_stack jsonb,
	detected_skills jsonb,
	project_type varchar(100),
	analyzed_at timestamptz DEFAULT now(),

	CONSTRAINT fk_repository_id
		FOREIGN KEY (repository_id)
		REFERENCES "Repository"(repository_id)
);

-- Table: Job

DROP TABLE IF EXISTS public."Company";
CREATE TABLE IF NOT EXISTS public."Company"
(
	company_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	company_name varchar(100),
	company_location varchar(100),
	description text
);

DROP TABLE IF EXISTS public."Job";
CREATE TABLE IF NOT EXISTS public."Job"
(
	job_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	company_id uuid NOT NULL,
	title varchar(100),
	job_location varchar(100),
	job_extension jsonb,
	job_detected_extension jsonb,
	description text,
	apply_option jsonb,

	CONSTRAINT fk_company_id
		FOREIGN KEY (company_id)
		REFERENCES "Company"(company_id)
);

DROP TABLE IF EXISTS public."Skill_Trend";
CREATE TABLE IF NOT EXISTS public."Skill_Trend"
(
	trend_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	skill_name varchar(100),
	frequency int,
	analyzed_date timestamptz DEFAULT now()
);

-- Payment

DROP TABLE IF EXISTS public."Invoice";
CREATE TABLE IF NOT EXISTS public."Invoice"
(
	invoice_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	user_id uuid NOT NULL,
	total_amount decimal(12, 2),
	currency varchar(10) DEFAULT 'VND',
	status varchar(10), -- 'pending', 'paid', 'cancelled', 'refunded'
	description text,
	created_at timestamptz DEFAULT now(),

	CONSTRAINT fk_user_id
		FOREIGN KEY (user_id)
		REFERENCES "User"(user_id)
);

DROP TABLE IF EXISTS public."Payment_Transaction";
CREATE TABLE IF NOT EXISTS public."Payment_Transaction"
(
	transaction_id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
	invoice_id uuid NOT NULL,
	gateway varchar(30), -- 'vnpay', 'momo', 'stripe'
	gateway_transaction_id varchar(100),
	amount decimal(12, 2),
	status varchar(10), -- 'success', 'failed', 'pending'
	webhook_payload jsonb,
	created_at timestamptz DEFAULT now(),

	CONSTRAINT fk_invoice_id
		FOREIGN KEY (invoice_id)
		REFERENCES "Invoice"(invoice_id)
);

-- SELECT * FROM public."User";