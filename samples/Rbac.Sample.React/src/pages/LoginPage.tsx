import { type FormEvent, useState } from "react";
import { api, setToken } from "../api";

const demos = [
  ["superadmin", "Full RBAC catalog"],
  ["admin", "Admin + passengers"],
  ["supervisor", "Passenger + reports"],
  ["officer", "Create/update passengers"],
  ["viewer", "Read only"],
  ["john", "Supervisor with report.export denied"],
];

export function LoginPage() {
  const [username, setUsername] = useState("officer");
  const [password, setPassword] = useState("Passw0rd!");
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    try {
      const result = await api.login(username, password);
      setToken(result.accessToken);
      window.location.assign("/");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Login failed");
    }
  }

  return (
    <div className="login-wrap">
      <form className="card login-card" onSubmit={onSubmit}>
        <p className="kicker">Sample host application</p>
        <h1>Sign in to exercise RBAC</h1>
        <p className="muted">
          Authentication is owned by the sample. The RBAC library only receives <code>sub</code> and
          evaluates permissions.
        </p>
        <label>
          Username
          <input value={username} onChange={(e) => setUsername(e.target.value)} autoComplete="username" />
        </label>
        <label>
          Password
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="current-password" />
        </label>
        {error ? <p className="error">{error}</p> : null}
        <button type="submit">Continue</button>
        <div className="demo-grid">
          {demos.map(([id, label]) => (
            <button key={id} type="button" className="ghost compact" onClick={() => setUsername(id)}>
              <strong>{id}</strong>
              <span>{label}</span>
            </button>
          ))}
        </div>
        <p className="muted">Password for every demo account: <code>Passw0rd!</code></p>
      </form>
    </div>
  );
}
