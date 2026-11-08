import type { User } from "oidc-client-ts";

export type Ingredient = { id: number; name: string; foodGroup: string; calories: number };
export type StorageLocation = {
  id: number; name: string; type: string; minimumTemperature: number;
  maximumTemperature: number; ingredients: Ingredient[]; createdAt: string;
};
export type StorageLocationDraft = {
  name: string; type: string; minimumTemperature: number;
  maximumTemperature: number; ingredientIds: number[];
};

export class ApiError extends Error {
  constructor(public readonly status: number, message: string) { super(message); this.name = "ApiError"; }
}

const baseUrl = (import.meta.env.VITE_STORAGE_API_BASE_URL ?? "http://localhost:50241").replace(/\/$/, "");

async function request<T>(path: string, user: User, init?: RequestInit): Promise<T> {
  const response = await fetch(`${baseUrl}${path}`, {
    ...init,
    headers: { Accept: "application/json", "Content-Type": "application/json", Authorization: `Bearer ${user.access_token}`, ...init?.headers }
  });
  if (!response.ok) {
    if (response.status === 401) throw new ApiError(401, "Your session is no longer authorized. Sign in again.");
    if (response.status === 403) throw new ApiError(403, "Your account is not allowed to perform this action.");
    let detail = "The server rejected the request.";
    try { const body = await response.json() as { error?: string; title?: string }; detail = body.error ?? body.title ?? detail; } catch { /* non-JSON response */ }
    throw new ApiError(response.status, `${detail} (HTTP ${response.status})`);
  }
  if (response.status === 204) return undefined as T;
  return await response.json() as T;
}

export function listLocations(user: User, name = "", type = "") {
  const query = new URLSearchParams(); if (name) query.set("name", name); if (type) query.set("type", type);
  return request<StorageLocation[]>(`/api/StorageLocations?${query}`, user);
}
export function createLocation(user: User, draft: StorageLocationDraft) { return request<StorageLocation>("/api/StorageLocations", user, { method: "POST", body: JSON.stringify(draft) }); }
export function updateLocation(user: User, id: number, draft: StorageLocationDraft) { return request<StorageLocation>(`/api/StorageLocations/${id}`, user, { method: "PUT", body: JSON.stringify(draft) }); }
export function deleteLocation(user: User, id: number) { return request<void>(`/api/StorageLocations/${id}`, user, { method: "DELETE" }); }