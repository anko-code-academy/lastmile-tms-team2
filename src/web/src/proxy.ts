import { auth } from "@/lib/auth";
import { NextResponse } from "next/server";

/** Next.js 16+ expects a named `proxy` export; keep default for tooling that still resolves it. */
const proxy = auth((req) => {
  const { pathname } = req.nextUrl;

  // Allow public paths
  const publicPaths = ["/login", "/forgot-password", "/reset-password", "/api/auth"];
  const isPublic = publicPaths.some((p) => pathname.startsWith(p));

  if (isPublic) return NextResponse.next();

  // If not authenticated, redirect to login
  if (!req.auth) {
    const loginUrl = new URL("/login", req.url);
    loginUrl.searchParams.set("callbackUrl", pathname);
    return NextResponse.redirect(loginUrl);
  }

  // If refresh token failed, redirect to login
  if (req.auth.error === "RefreshTokenError") {
    const loginUrl = new URL("/login", req.url);
    return NextResponse.redirect(loginUrl);
  }

  return NextResponse.next();
});

export { proxy };
export default proxy;

export const config = {
  matcher: [
    // Match all routes except static files and Next.js internals
    "/((?!_next/static|_next/image|favicon.ico|.*\\.(?:svg|png|jpg|jpeg|gif|webp)$).*)",
  ],
};
