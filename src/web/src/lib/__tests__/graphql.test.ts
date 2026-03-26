import { beforeEach, describe, expect, it, vi } from "vitest";
import { getSession } from "next-auth/react";
import { graphqlRequest } from "../graphql";

vi.mock("next-auth/react", () => ({
  getSession: vi.fn(),
}));

const mockFetch = vi.fn();
global.fetch = mockFetch;
const mockedGetSession = vi.mocked(getSession);

describe("graphqlRequest", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    process.env.NEXT_PUBLIC_API_URL = "http://localhost";
    mockedGetSession.mockResolvedValue(null);
  });

  it("should send the query with the bearer token and return data", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        data: {
          viewer: {
            id: "1",
          },
        },
      }),
    });

    const result = await graphqlRequest<{ viewer: { id: string } }>(
      "query { viewer { id } }",
      { search: "admin" },
      "access-token"
    );

    expect(result.viewer.id).toBe("1");
    expect(global.fetch).toHaveBeenCalledWith(
      expect.stringContaining("/graphql"),
      expect.objectContaining({
        method: "POST",
        headers: expect.objectContaining({
          Authorization: "Bearer access-token",
          "Content-Type": "application/json",
        }),
      })
    );
  });

  it("should throw the combined GraphQL error messages", async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: async () => ({
        errors: [
          { message: "First error" },
          { message: "Second error" },
        ],
      }),
    });

    await expect(
      graphqlRequest("query { broken }")
    ).rejects.toThrow("First error; Second error");
  });
});
