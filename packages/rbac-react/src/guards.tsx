import { type ReactNode } from "react";
import { Navigate } from "react-router-dom";
import { useRbac } from "./RbacProvider";

type PermissionProps = {
  permission?: string;
  anyOf?: string[];
  allOf?: string[];
  fallback?: ReactNode;
  children: ReactNode;
};

function allowed(props: Pick<PermissionProps, "permission" | "anyOf" | "allOf">, rbac: ReturnType<typeof useRbac>) {
  if (props.permission) return rbac.hasPermission(props.permission);
  if (props.anyOf?.length) return rbac.hasAnyPermission(props.anyOf);
  if (props.allOf?.length) return rbac.hasAllPermissions(props.allOf);
  return true;
}

/** Hide or swap UI when the current user lacks a permission. Never use this as the only security check. */
export function HasPermission({ fallback = null, children, ...rest }: PermissionProps) {
  const rbac = useRbac();
  return allowed(rest, rbac) ? <>{children}</> : <>{fallback}</>;
}

/** Route-level guard. The API must still enforce the same permission. */
export function PermissionRoute({
  permission,
  anyOf,
  allOf,
  redirectTo = "/forbidden",
  children,
}: PermissionProps & { redirectTo?: string }) {
  const rbac = useRbac();
  if (!rbac.isReady) return null;
  return allowed({ permission, anyOf, allOf }, rbac) ? <>{children}</> : <Navigate to={redirectTo} replace />;
}
