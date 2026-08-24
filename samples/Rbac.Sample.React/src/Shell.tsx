import { HasPermission, useMenus, useRbacUser, type RbacMenu } from "@general-rbac/react";
import { NavLink, Outlet } from "react-router-dom";
import { setToken } from "./api";

const icons: Record<string, string> = {
  home: "⌂",
  users: "☉",
  list: "☰",
  chart: "▦",
  shield: "⬡",
  badge: "▣",
  key: "⚿",
  blocks: "⊞",
  menu: "≡",
};

function MenuLinks({ menus }: { menus: RbacMenu[] }) {
  return (
    <ul className="nav">
      {menus.map((menu) => (
        <li key={menu.id}>
          {menu.route ? (
            <NavLink to={menu.route} className={({ isActive }) => (isActive ? "active" : undefined)}>
              <span className="glyph">{icons[menu.icon ?? ""] ?? "•"}</span>
              {menu.displayName}
            </NavLink>
          ) : (
            <div className="nav-group">
              <span className="glyph">{icons[menu.icon ?? ""] ?? "•"}</span>
              {menu.displayName}
            </div>
          )}
          {menu.children.length > 0 ? <MenuLinks menus={menu.children} /> : null}
        </li>
      ))}
    </ul>
  );
}

export function Shell() {
  const user = useRbacUser();
  const menus = useMenus();

  return (
    <div className="shell">
      <aside className="sidebar">
        <div className="brand">
          <strong>General RBAC</strong>
          <span>Permission-centric access</span>
        </div>
        <MenuLinks menus={menus} />
        <HasPermission anyOf={["rbac.users.read", "rbac.roles.read"]}>
          <p className="sidebar-note">Administration is visible because this user has RBAC catalog permissions, not because a menu was assigned directly.</p>
        </HasPermission>
      </aside>
      <div className="main">
        <header className="topbar">
          <div>
            <div className="kicker">Authenticated identity stays outside the library</div>
            <strong>{user?.displayName}</strong>
            <span className="muted"> {user?.username} · {(user?.roles ?? []).join(", ") || "no roles"}</span>
          </div>
          <button
            className="ghost"
            onClick={() => {
              setToken(null);
              window.location.assign("/login");
            }}
          >
            Sign out
          </button>
        </header>
        <section className="content">
          <Outlet />
        </section>
      </div>
    </div>
  );
}
