import { z } from "zod";

/**
 * Hyphenated 32-hex segment form used by `Guid` / GraphQL IDs.
 * Zod 4's `z.string().uuid()` follows RFC variant bits and rejects some values
 * that .NET and our seeds still use (e.g. `00000000-0000-0000-0000-000000000001`).
 */
const GUID_HEX =
  /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

export function guidString(message: string) {
  return z.string().min(1, message).regex(GUID_HEX, message);
}

export function isGuidString(value: string | null | undefined): boolean {
  return typeof value === "string" && GUID_HEX.test(value.trim());
}
