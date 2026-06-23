import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { skillApi } from "../../../api/skillApi";
import SkillSearchPicker from "./SkillSearchPicker";

vi.mock("../../api/skillApi", () => ({
  skillApi: {
    searchSkills: vi.fn(),
    getSkillById: vi.fn(),
    getCategories: vi.fn(),
  },
}));

const sqlSkill = {
  skillId: "11111111-1111-1111-1111-111111111111",
  name: "SQL Fundamentals",
  slug: "sql-fundamentals",
  category: "Database",
  careerRoles: ["Backend Developer", "Data Analyst"],
  roadmapNodeCount: 12,
};

const cssSkill = {
  skillId: "22222222-2222-2222-2222-222222222222",
  name: "CSS Fundamentals",
  slug: "css-fundamentals",
  category: "Frontend",
  careerRoles: ["Frontend Developer"],
  roadmapNodeCount: 8,
};

describe("SkillSearchPicker", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    localStorage.clear();
    skillApi.getCategories.mockResolvedValue(["Database", "Frontend"]);
    skillApi.searchSkills.mockResolvedValue({
      items: [sqlSkill, cssSkill],
      totalCount: 2,
      limit: 20,
      offset: 0,
      hasMore: false,
    });
  });

  it("opens with recently used skills instead of the full catalog", async () => {
    localStorage.setItem(
      "content-manager.recent-skills",
      JSON.stringify([sqlSkill]),
    );

    const user = userEvent.setup();
    const { container } = render(
      <SkillSearchPicker value="" onChange={vi.fn()} />,
    );

    await user.click(screen.getByRole("button", { name: "Choose a skill" }));

    expect(await screen.findByText("Recently used")).toBeInTheDocument();
    expect(screen.getByText("SQL Fundamentals")).toBeInTheDocument();
    expect(
      container.contains(screen.getByRole("listbox", { name: "Skills" })),
    ).toBe(false);
    expect(skillApi.searchSkills).not.toHaveBeenCalled();
  });

  it("highlights matching text and selects with Enter", async () => {
    localStorage.setItem(
      "content-manager.recent-skills",
      JSON.stringify([cssSkill]),
    );

    const onChange = vi.fn();
    const user = userEvent.setup();
    render(<SkillSearchPicker value="" onChange={onChange} />);

    await user.click(screen.getByRole("button", { name: "Choose a skill" }));

    fireEvent.change(screen.getByRole("combobox", { name: "Search skills" }), {
      target: { value: "sql" },
    });

    await waitFor(() => {
      expect(skillApi.searchSkills).toHaveBeenCalledWith(
        expect.objectContaining({ search: "sql" }),
      );
    });

    const highlightedMatch = await screen.findByText("SQL");
    expect(highlightedMatch.tagName).toBe("MARK");

    await user.keyboard("{Enter}");

    expect(onChange).toHaveBeenCalledWith(sqlSkill.skillId, sqlSkill);
    expect(
      screen.queryByRole("listbox", { name: "Skills" }),
    ).not.toBeInTheDocument();
  });

  it("shows a clear no-results state", async () => {
    localStorage.setItem(
      "content-manager.recent-skills",
      JSON.stringify([cssSkill]),
    );
    skillApi.searchSkills.mockResolvedValue({
      items: [],
      totalCount: 0,
      limit: 20,
      offset: 0,
      hasMore: false,
    });

    const user = userEvent.setup();
    render(<SkillSearchPicker value="" onChange={vi.fn()} />);

    await user.click(screen.getByRole("button", { name: "Choose a skill" }));
    await user.type(
      screen.getByRole("combobox", { name: "Search skills" }),
      "xyz123",
    );

    expect(await screen.findByText("No skills found.")).toBeInTheDocument();
  });

  it("requires the actions menu before removing a selected skill", async () => {
    const onChange = vi.fn();
    const user = userEvent.setup();

    render(
      <SkillSearchPicker
        value={sqlSkill.skillId}
        initialSkill={sqlSkill}
        onChange={onChange}
      />,
    );

    expect(
      screen.queryByRole("button", { name: "Remove skill" }),
    ).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Skill actions" }));
    await user.click(screen.getByRole("button", { name: "Remove skill" }));

    expect(onChange).toHaveBeenCalledWith("", null);
    expect(
      screen.getByRole("button", { name: "Choose a skill" }),
    ).toBeInTheDocument();
  });

  it("filters suggestions by category", async () => {
    const user = userEvent.setup();
    render(<SkillSearchPicker value="" onChange={vi.fn()} />);

    await user.click(screen.getByRole("button", { name: "Choose a skill" }));
    await screen.findByText("Suggested");
    await user.click(await screen.findByRole("button", { name: "Database" }));

    await waitFor(() => {
      expect(skillApi.searchSkills).toHaveBeenLastCalledWith(
        expect.objectContaining({
          category: "Database",
          sort: "popular",
        }),
      );
    });
  });
});
