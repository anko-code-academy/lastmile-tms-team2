import { getSession } from "next-auth/react";

import { apiBaseUrl, parseApiErrorMessage } from "@/lib/network/api";

function parseFileNameFromContentDisposition(
  contentDisposition: string | null,
): string | null {
  if (!contentDisposition) {
    return null;
  }

  const utf8Match = contentDisposition.match(/filename\*=UTF-8''([^;]+)/i);
  if (utf8Match?.[1]) {
    try {
      return decodeURIComponent(utf8Match[1]);
    } catch {
      return utf8Match[1];
    }
  }

  const fileNameMatch = contentDisposition.match(/filename="([^"]+)"/i);
  if (fileNameMatch?.[1]) {
    return fileNameMatch[1];
  }

  const bareFileNameMatch = contentDisposition.match(/filename=([^;]+)/i);
  return bareFileNameMatch?.[1]?.trim() ?? null;
}

export function saveBlobAsFile(blob: Blob, fileName: string) {
  const objectUrl = URL.createObjectURL(blob);
  const link = document.createElement("a");

  link.href = objectUrl;
  link.download = fileName;
  link.style.display = "none";

  document.body.appendChild(link);
  link.click();
  link.remove();

  setTimeout(() => {
    URL.revokeObjectURL(objectUrl);
  }, 0);
}

export async function downloadAuthenticatedFile(
  path: string,
  init?: RequestInit,
): Promise<string> {
  const session = await getSession();
  const headers = new Headers(init?.headers);

  if (session?.accessToken) {
    headers.set("Authorization", `Bearer ${session.accessToken}`);
  }

  const response = await fetch(`${apiBaseUrl().replace(/\/$/, "")}${path}`, {
    ...init,
    headers,
  });

  if (!response.ok) {
    throw new Error(await parseApiErrorMessage(response));
  }

  const blob = await response.blob();
  const fileName =
    parseFileNameFromContentDisposition(
      response.headers.get("content-disposition"),
    ) ?? "download";

  saveBlobAsFile(blob, fileName);
  return fileName;
}
