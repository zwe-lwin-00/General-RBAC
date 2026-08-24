import { createContext, useContext, type ReactNode } from "react";
import type { RbacMenu, RbacUser } from "./types";

export type RbacContextValue = {
  user: RbacUser | null;
  permissions: ReadonlySet<string>;
  menus: RbacMenu[];
  isReady: boolean;
  hasPermission: (permission: string) => boolean;
  hasAnyPermission: (permissions: string[]) => boolean;
  hasAllPermissions: (permissions: string[]) => boolean;
};

const RbacContext = createContext<RbacContextValue | null>(null);

export function RbacProvider({
  user,
  permissions,
  menus,
  isReady = true,
  children,
}: {
  user: RbacUser | null;
  permissions: readonly string[];
  menus: RbacMenu[];
  isReady?: boolean;
  children: ReactNode;
}) {
  const set = new Set(permissions.map((p) => p.toLowerCase()));
  const value: RbacContextValue = {
    user,
    permissions: set,
    menus,
    isReady,
    hasPermission: (permission) => set.has(permission.toLowerCase()),
    hasAnyPermission: (items) => items.some((p) => set.has(p.toLowerCase())),
    hasAllPermissions: (items) => items.every((p) => set.has(p.toLowerCase())),
  };

  return <RbacContext.Provider value={value}>{children}</RbacContext.Provider>;
}

export function useRbac(): RbacContextValue {
  const ctx = useContext(RbacContext);
  if (!ctx) {
    throw new Error("useRbac must be used inside RbacProvider.");
  }
  return ctx;
}

export function usePermission(permission: string): boolean {
  return useRbac().hasPermission(permission);
}

export function usePermissions(): ReadonlySet<string> {
  return useRbac().permissions;
}

export function useMenus(): RbacMenu[] {
  return useRbac().menus;
}

export function useRbacUser(): RbacUser | null {
  return useRbac().user;
}
