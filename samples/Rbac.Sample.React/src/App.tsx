import { PermissionRoute, RbacProvider, type RbacSnapshot } from "@general-rbac/react";
import { useEffect, useState } from "react";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { api, getToken, setToken } from "./api";
import { MenusPage, PermissionsPage, ProgramsPage, RolesPage, UsersPage } from "./pages/AdminPages";
import { DashboardPage, ForbiddenPage } from "./pages/DashboardPage";
import { LoginPage } from "./pages/LoginPage";
import { PassengersPage, ReportsPage } from "./pages/PassengersPage";
import { RequireAuth } from "./RequireAuth";
import { Shell } from "./Shell";

export default function App() {
  const token = getToken();
  const [snapshot, setSnapshot] = useState<RbacSnapshot | null>(null);
  const [ready, setReady] = useState(!token);

  useEffect(() => {
    if (!token) return;
    api
      .me()
      .then((me) => {
        setSnapshot(me);
        setReady(true);
      })
      .catch(() => {
        setToken(null);
        setReady(true);
      });
  }, [token]);

  return (
    <RbacProvider
      user={snapshot?.user ?? null}
      permissions={snapshot?.permissions ?? []}
      menus={snapshot?.menus ?? []}
      isReady={ready}
    >
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route element={<RequireAuth />}>
            <Route element={<Shell />}>
              <Route path="/" element={<DashboardPage />} />
              <Route
                path="/passengers"
                element={
                  <PermissionRoute permission="passenger.read">
                    <PassengersPage />
                  </PermissionRoute>
                }
              />
              <Route
                path="/reports"
                element={
                  <PermissionRoute permission="report.read">
                    <ReportsPage />
                  </PermissionRoute>
                }
              />
              <Route
                path="/admin/users"
                element={
                  <PermissionRoute permission="rbac.users.read">
                    <UsersPage />
                  </PermissionRoute>
                }
              />
              <Route
                path="/admin/roles"
                element={
                  <PermissionRoute permission="rbac.roles.read">
                    <RolesPage />
                  </PermissionRoute>
                }
              />
              <Route
                path="/admin/permissions"
                element={
                  <PermissionRoute permission="rbac.permissions.read">
                    <PermissionsPage />
                  </PermissionRoute>
                }
              />
              <Route
                path="/admin/programs"
                element={
                  <PermissionRoute permission="rbac.programs.read">
                    <ProgramsPage />
                  </PermissionRoute>
                }
              />
              <Route
                path="/admin/menus"
                element={
                  <PermissionRoute permission="rbac.menus.read">
                    <MenusPage />
                  </PermissionRoute>
                }
              />
              <Route path="/forbidden" element={<ForbiddenPage />} />
            </Route>
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </RbacProvider>
  );
}
