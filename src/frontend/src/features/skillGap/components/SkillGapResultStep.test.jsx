import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import SkillGapResultStep from "./SkillGapResultStep";

function createResult() {
  return {
    roadmapId: "roadmap-1",
    roadmapVersionId: "version-1",
    roadmapSlug: "backend-developer",
    roadmapName: "Backend Developer",
    matchedSkills: 1,
    missingSkills: 1,
    totalSkills: 2,
    categories: [
      {
        categoryName: "Core",
        displayOrder: 1,
        matchedSkills: 1,
        missingSkills: 1,
        totalSkills: 2,
        skills: [
          {
            skillId: "skill-1",
            skillName: "C#",
            skillSlug: "csharp",
            isMatched: true,
          },
          {
            skillId: "skill-2",
            skillName: "PostgreSQL",
            skillSlug: "postgresql",
            isMatched: false,
          },
        ],
      },
    ],
  };
}

describe("SkillGapResultStep", () => {
  it("links every skill to its skill learning page and links the roadmap", () => {
    render(
      <MemoryRouter>
        <SkillGapResultStep result={createResult()} onBack={vi.fn()} onReset={vi.fn()} />
      </MemoryRouter>,
    );

    expect(screen.getByRole("link", { name: "View learning modules for C#" }))
      .toHaveAttribute("href", "/learning-modules/skills/csharp");
    expect(screen.getByRole("link", { name: "View learning modules for PostgreSQL" }))
      .toHaveAttribute("href", "/learning-modules/skills/postgresql");
    expect(screen.getByRole("link", { name: "View roadmap" }))
      .toHaveAttribute("href", "/roadmaps/backend-developer");
  });

  it("labels the roadmap as current when rendering a history result", () => {
    render(
      <MemoryRouter>
        <SkillGapResultStep
          result={createResult()}
          isHistoryView
          onBackToHistory={vi.fn()}
          onReset={vi.fn()}
        />
      </MemoryRouter>,
    );

    expect(screen.getByRole("link", { name: "View current roadmap" }))
      .toHaveAttribute("href", "/roadmaps/backend-developer");
  });
});
