import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import SkillLearningModulesPage from "./SkillLearningModulesPage";

const storeMock = vi.hoisted(() => ({
  state: {},
}));

vi.mock("../../stores/useLearningModuleStore", () => ({
  useLearningModuleStore: (selector) => selector(storeMock.state),
}));

function renderPage() {
  return render(
    <MemoryRouter initialEntries={["/learning-modules/skills/postgresql"]}>
      <Routes>
        <Route
          path="/learning-modules/skills/:skillSlug"
          element={<SkillLearningModulesPage />}
        />
      </Routes>
    </MemoryRouter>,
  );
}

describe("SkillLearningModulesPage", () => {
  beforeEach(() => {
    storeMock.state = {
      skillModulesBySlug: {},
      skillModulesLoadingBySlug: {},
      skillModulesLoadedBySlug: { postgresql: true },
      skillModulesErrorBySlug: {},
      loadModulesBySkillSlug: vi.fn().mockResolvedValue(null),
    };
  });

  it("shows a skill-specific empty state when no module is published", () => {
    storeMock.state.skillModulesBySlug.postgresql = {
      skillId: "skill-1",
      skillName: "PostgreSQL",
      skillSlug: "postgresql",
      isActive: true,
      modules: [],
    };

    renderPage();

    expect(screen.getByRole("heading", { name: "PostgreSQL" })).toBeInTheDocument();
    expect(screen.getByText("Learning modules for this skill are not available yet"))
      .toBeInTheDocument();
  });

  it("renders every published module returned for the skill", () => {
    storeMock.state.skillModulesBySlug.postgresql = {
      skillId: "skill-1",
      skillName: "PostgreSQL",
      skillSlug: "postgresql",
      isActive: true,
      modules: [
        {
          skillModuleId: "module-1",
          title: "PostgreSQL Fundamentals",
          slug: "postgresql-fundamentals",
          lessonCount: 4,
          questionCount: 10,
          estimatedHours: 3,
          enrollment: null,
        },
        {
          skillModuleId: "module-2",
          title: "PostgreSQL Performance",
          slug: "postgresql-performance",
          lessonCount: 3,
          questionCount: 8,
          estimatedHours: 2,
          enrollment: null,
        },
      ],
    };

    renderPage();

    expect(screen.getByText("PostgreSQL Fundamentals")).toBeInTheDocument();
    expect(screen.getByText("PostgreSQL Performance")).toBeInTheDocument();
    expect(screen.getAllByRole("button", { name: /Start learning/ })).toHaveLength(2);
  });
});
