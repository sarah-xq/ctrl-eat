import type { User } from "oidc-client-ts";

export type CalorieGoalResponse = { calorieGoal: number | null };

export class ApiError extends Error {
  constructor(public readonly status: number, message: string) {
    super(message);
    this.name = "ApiError";
  }
}

const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5102").replace(/\/$/, "");

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

  if (!response.ok) {
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

  return (await response.json()) as CalorieGoalResponse;
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