export const roadmapStatuses = ["draft", "published", "archived"];

export const roadmapPageSize = 6;

export const initialRoadmapListResult = {
  items: [],
  totalCount: 0,
  page: 1,
  pageSize: roadmapPageSize,
  totalPages: 0,
  statusCounts: {
    draft: 0,
    published: 0,
    archived: 0,
  },
};

export const roadmapSortOptions = [
  { value: "updated_desc", label: "Recently updated" },
  { value: "created_desc", label: "Recently created" },
  { value: "title_asc", label: "Title A-Z" },
  { value: "title_desc", label: "Title Z-A" },
];

export const difficultyOptions = [
  { value: "", label: "No difficulty" },
  { value: "beginner", label: "Beginner" },
  { value: "intermediate", label: "Intermediate" },
  { value: "advanced", label: "Advanced" },
];


export const draftNodeTypeOptions = [
  { value: "phase", label: "Phase" },
  { value: "resource_group", label: "Group" },
  { value: "topic", label: "Topic" },
  { value: "project", label: "Project" },
  { value: "checkpoint", label: "Checkpoint" },
];

export const draftNodePositionOptions = [
  { value: "end", label: "At the end" },
  { value: "before", label: "Before selected item" },
  { value: "after", label: "After selected item" },
];

export const checkpointTypeOptions = [
  { value: "review", label: "Review" },
  { value: "assessment", label: "Assessment" },
  { value: "gate", label: "Gate" },
];
