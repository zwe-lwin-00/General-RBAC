import type { RbacMenu, RbacSnapshot, RbacUser } from "@general-rbac/react";

const tokenKey = "rbac.sample.token";

export function getToken() {
  return localStorage.getItem(tokenKey);
}

export function setToken(token: string | null) {
  if (token) localStorage.setItem(tokenKey, token);
  else localStorage.removeItem(tokenKey);
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const token = getToken();
  const response = await fetch(path, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(init?.headers ?? {}),
    },
  });
  if (response.status === 204) return undefined as T;
  const body = await response.json().catch(() => ({}));
  if (!response.ok) {
    const error = new Error(body.error ?? `Request failed (${response.status})`);
    (error as Error & { status: number }).status = response.status;
    throw error;
  }
  return body as T;
}

export const api = {
  login: (username: string, password: string) =>
    request<{ accessToken: string; displayName: string; username: string }>("/api/auth/login", {
      method: "POST",
      body: JSON.stringify({ username, password }),
    }),
  me: () => request<RbacSnapshot>("/api/rbac/me"),
  passengers: () => request<Passenger[]>("/api/passengers"),
  createPassenger: (payload: Omit<Passenger, "id">) =>
    request<Passenger>("/api/passengers", { method: "POST", body: JSON.stringify(payload) }),
  deletePassenger: (id: string) => request<void>(`/api/passengers/${id}`, { method: "DELETE" }),
  exportPassengers: () => request<{ generatedAt: string; rows: Passenger[] }>("/api/passengers/export"),
  reports: () => request<{ title: string; total: number }>("/api/reports"),
  exportReports: () => request<{ format: string; content: string }>("/api/reports/export"),
  users: () => request<Paged<RbacUser>>("/api/rbac/users"),
  roles: () => request<Paged<Role>>("/api/rbac/roles"),
  permissions: () => request<Paged<Permission>>("/api/rbac/permissions?pageSize=200"),
  programs: () => request<Paged<Program>>("/api/rbac/programs"),
  menus: () => request<RbacMenu[]>("/api/rbac/menus"),
};

export type Passenger = { id: string; fullName: string; documentNo: string; nationality: string };
export type Role = { id: string; code: string; name: string; description?: string; isSystemRole: boolean; permissions: string[] };
export type Permission = { id: string; code: string; name: string; resource: string; action: string; isSystemPermission: boolean };
export type Program = { id: string; code: string; name: string; module?: string; permissions: string[] };
export type Paged<T> = { items: T[]; totalCount: number; page: number; pageSize: number };
