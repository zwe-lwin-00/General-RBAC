import { useEffect, useState } from "react";
import { api, type Permission, type Program, type Role } from "../api";
import type { RbacMenu, RbacUser } from "@general-rbac/react";

function CollectionPage<T>({
  title,
  kicker,
  loader,
  columns,
}: {
  title: string;
  kicker: string;
  loader: () => Promise<T[]>;
  columns: { header: string; cell: (row: T) => string }[];
}) {
  const [rows, setRows] = useState<T[]>([]);
  const [error, setError] = useState<string | null>(null);
  useEffect(() => {
    void loader()
      .then(setRows)
      .catch((err: Error) => setError(err.message));
    // Intentional: load once per mount. Callers pass inline lambdas.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <div>
      <p className="kicker">{kicker}</p>
      <h1>{title}</h1>
      {error ? <p className="error">{error}</p> : null}
      <table>
        <thead>
          <tr>
            {columns.map((c) => (
              <th key={c.header}>{c.header}</th>
            ))}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, index) => (
            <tr key={index}>
              {columns.map((c) => (
                <td key={c.header}>{c.cell(row)}</td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export function UsersPage() {
  return (
    <CollectionPage<RbacUser>
      kicker="Administration"
      title="Users"
      loader={async () => (await api.users()).items}
      columns={[
        { header: "Username", cell: (u) => u.username },
        { header: "Display name", cell: (u) => u.displayName },
        { header: "External id", cell: (u) => u.externalId },
        { header: "Roles", cell: (u) => u.roles.join(", ") },
      ]}
    />
  );
}

export function RolesPage() {
  return (
    <CollectionPage<Role>
      kicker="Administration"
      title="Roles"
      loader={async () => (await api.roles()).items}
      columns={[
        { header: "Code", cell: (r) => r.code },
        { header: "Name", cell: (r) => r.name },
        { header: "Permissions", cell: (r) => r.permissions.join(", ") },
      ]}
    />
  );
}

export function PermissionsPage() {
  return (
    <CollectionPage<Permission>
      kicker="Administration"
      title="Permissions"
      loader={async () => (await api.permissions()).items}
      columns={[
        { header: "Code", cell: (p) => p.code },
        { header: "Resource", cell: (p) => p.resource },
        { header: "Action", cell: (p) => p.action },
        { header: "Name", cell: (p) => p.name },
      ]}
    />
  );
}

export function ProgramsPage() {
  return (
    <CollectionPage<Program>
      kicker="Administration"
      title="Programs"
      loader={async () => (await api.programs()).items}
      columns={[
        { header: "Code", cell: (p) => p.code },
        { header: "Name", cell: (p) => p.name },
        { header: "Module", cell: (p) => p.module ?? "" },
        { header: "Permissions", cell: (p) => p.permissions.join(", ") },
      ]}
    />
  );
}

function flattenMenus(menus: RbacMenu[], depth = 0): { menu: RbacMenu; depth: number }[] {
  return menus.flatMap((menu) => [{ menu, depth }, ...flattenMenus(menu.children, depth + 1)]);
}

export function MenusPage() {
  return (
    <CollectionPage<{ menu: RbacMenu; depth: number }>
      kicker="Administration"
      title="Menus"
      loader={async () => flattenMenus(await api.menus())}
      columns={[
        { header: "Name", cell: (r) => `${"— ".repeat(r.depth)}${r.menu.displayName}` },
        { header: "Route", cell: (r) => r.menu.route ?? "" },
        { header: "Type", cell: (r) => r.menu.menuType },
        { header: "Code", cell: (r) => r.menu.code },
      ]}
    />
  );
}
