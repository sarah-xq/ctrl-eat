import type { User } from "oidc-client-ts";

export type CalorieGoalResponse = { calorieGoal: number | null };

export class ApiError extends Error {
  constructor(public readonly status: number, message: string) {
    super(message);
    this.name = "ApiError";
  }
}

const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5103").replace(/\/$/, "");

async function callApi(path: string, user: User, init?: RequestInit): Promise<CalorieGoalResponse> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
      Authorization: `Bearer ${user.access_token}`,
      ...init?.headers
    }
  });

  await ensureSuccess(response);

  return (await response.json()) as CalorieGoalResponse;
}

export async function uploadProfilePicture(user: User, file: File): Promise<void> {
  const form = new FormData();
  form.append("file", file);
  const response = await fetch(`${apiBaseUrl}/api/users/me/profile-picture`, {
    method: "PUT",
    headers: {
      Accept: "application/json",
      Authorization: `Bearer ${user.access_token}`
    },
    body: form
  });
  await ensureSuccess(response);
}

export async function readProfilePicture(user: User): Promise<Blob | null> {
  const response = await fetch(`${apiBaseUrl}/api/users/me/profile-picture`, {
    headers: {
      Accept: "image/jpeg, image/png, image/webp",
      Authorization: `Bearer ${user.access_token}`
    }
  });
  if (response.status === 404) return null;
  await ensureSuccess(response);
  return response.blob();
}

async function ensureSuccess(response: Response): Promise<void> {
  if (response.ok) return;
  if (response.status === 401) throw new ApiError(401, "Your session is no longer authorized.");
  if (response.status === 403) throw new ApiError(403, "Your account is not allowed to perform this action.");
  let detail = "The server rejected the request.";
  try {
    const body = (await response.json()) as { error?: string; title?: string };
    detail = body.error ?? body.title ?? detail;
  } catch {
    // Keep the useful status-based message when the response is not JSON.
  }
  throw new ApiError(response.status, `${detail} (HTTP ${response.status})`);
}

export function readGoal(user: User) {
  return callApi("/api/users/me", user);
}

export function saveGoal(user: User, calorieGoal: number) {
  return callApi("/api/users/me/calorie-goal", user, {
    method: "PUT",
    body: JSON.stringify({ calorieGoal })
  });
}