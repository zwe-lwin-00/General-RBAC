import { usePermission, useRbacUser } from "@general-rbac/react";

export function DashboardPage() {
  const user = useRbacUser();
  const canCreate = usePermission("passenger.create");
  const canAdmin = usePermission("rbac.users.read");

  return (
    <div>
      <p className="kicker">Dashboard</p>
      <h1>Welcome, {user?.displayName}</h1>
      <p className="lede">
        Menus describe navigation. Permissions decide what this user can actually do. Try signing in as
        different demo users to see both change independently.
      </p>
      <div className="stat-grid">
        <article className="card">
          <span className="kicker">Passenger create</span>
          <strong>{canCreate ? "Allowed" : "Denied"}</strong>
        </article>
        <article className="card">
          <span className="kicker">RBAC administration</span>
          <strong>{canAdmin ? "Allowed" : "Denied"}</strong>
        </article>
        <article className="card">
          <span className="kicker">Roles</span>
          <strong>{user?.roles.join(" · ") || "None"}</strong>
        </article>
      </div>
    </div>
  );
}

export function ForbiddenPage() {
  return (
    <div>
      <p className="kicker">403</p>
      <h1>Missing permission</h1>
      <p className="lede">The React guard hid this route. The API would return the same decision.</p>
    </div>
  );
}
