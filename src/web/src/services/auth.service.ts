import type { TokenResponse, TokenErrorResponse } from "@/types/auth";

// Use internal Docker network URL for server-side requests, public URL for client-side
const isServer = typeof window === "undefined";

// Auth endpoints (connect/token) use base URL without /api prefix
const AUTH_BASE_URL =
  isServer
    ? process.env.API_URL ?? process.env.NEXT_PUBLIC_AUTH_URL ?? process.env.NEXT_PUBLIC_API_URL!
    : process.env.NEXT_PUBLIC_AUTH_URL ?? process.env.NEXT_PUBLIC_API_URL!;

/**
 * Authenticate with username/password via OpenIddict password grant.
 * Requests `offline_access` scope to receive a refresh token.
 */
export async function loginWithPassword(
  email: string,
  password: string
): Promise<TokenResponse> {
  const body = new URLSearchParams({
    grant_type: "password",
    username: email,
    password,
    scope: "offline_access",
  });

  const res = await fetch(`${AUTH_BASE_URL}/connect/token`, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: body.toString(),
  });

  if (!res.ok) {
    const error: TokenErrorResponse = await res.json();
    throw new Error(error.error_description ?? error.error ?? "Login failed");
  }

  return res.json() as Promise<TokenResponse>;
}

/**
 * Refresh access token using a refresh token via OpenIddict refresh_token grant.
 */
export async function refreshAccessToken(
  refreshToken: string
): Promise<TokenResponse> {
  const body = new URLSearchParams({
    grant_type: "refresh_token",
    refresh_token: refreshToken,
  });

  const res = await fetch(`${AUTH_BASE_URL}/connect/token`, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: body.toString(),
  });

  if (!res.ok) {
    const error: TokenErrorResponse = await res.json();
    throw new Error(
      error.error_description ?? error.error ?? "Token refresh failed"
    );
  }

  return res.json() as Promise<TokenResponse>;
}
