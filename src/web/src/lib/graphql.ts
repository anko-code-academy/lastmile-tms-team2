import { getSession } from "next-auth/react";
import { apiBaseUrl, parseApiErrorMessage } from "./api";

export interface GraphQLErrorItem {
  message: string;
  extensions?: Record<string, unknown>;
}

export interface GraphQLResponse<T> {
  data?: T;
  errors?: GraphQLErrorItem[];
}

/** Prefer real text when HC still sends a generic top-level `message`. */
export function messageFromGraphQLError(e: GraphQLErrorItem): string {
  const raw = e.message?.trim() ?? "";
  const generic =
    /^Unexpected Execution Error$/i.test(raw) ||
    /^Unexpected Error$/i.test(raw);

  const ext = e.extensions;
  if (generic && ext && typeof ext === "object") {
    const candidates = [ext["message"], ext["exceptionMessage"], ext["detail"]];
    for (const c of candidates) {
      if (typeof c === "string" && c.trim()) return c.trim();
    }
  }

  return raw || "Unknown GraphQL error";
}

export function graphqlEndpointUrl(): string {
  return `${apiBaseUrl().replace(/\/$/, "")}/api/graphql`;
}

export async function graphqlRequest<TData>(
  query: string,
  variables?: Record<string, unknown>,
  accessToken?: string
): Promise<TData> {
  const session = accessToken ? null : await getSession();
  const token = accessToken ?? session?.accessToken;
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
  };

  if (token) {
    headers.Authorization = `Bearer ${token}`;
  }

  const res = await fetch(graphqlEndpointUrl(), {
    method: "POST",
    headers,
    body: JSON.stringify({ query, variables }),
    cache: "no-store",
  });

  if (!res.ok) {
    const message = await parseApiErrorMessage(res);
    throw new Error(message);
  }

  const json: GraphQLResponse<TData> = await res.json();
  if (json.errors?.length) {
    throw new Error(json.errors.map((e) => messageFromGraphQLError(e)).join("; "));
  }
  if (!json.data) {
    throw new Error("No data in GraphQL response");
  }

  return json.data;
}
