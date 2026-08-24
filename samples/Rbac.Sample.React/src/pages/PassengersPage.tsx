import { HasPermission } from "@general-rbac/react";
import { type FormEvent, useEffect, useState } from "react";
import { api, type Passenger } from "../api";

export function PassengersPage() {
  const [rows, setRows] = useState<Passenger[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState({ fullName: "", documentNo: "", nationality: "MM" });

  async function refresh() {
    try {
      setRows(await api.passengers());
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unable to load passengers");
    }
  }

  useEffect(() => {
    void refresh();
  }, []);

  async function onCreate(event: FormEvent) {
    event.preventDefault();
    await api.createPassenger(form);
    setForm({ fullName: "", documentNo: "", nationality: "MM" });
    await refresh();
  }

  return (
    <div>
      <p className="kicker">Passenger listing</p>
      <h1>Passengers</h1>
      <p className="lede">Buttons are gated by <code>HasPermission</code>. Removing the button would not grant API access.</p>
      {error ? <p className="error">{error}</p> : null}
      <HasPermission permission="passenger.create">
        <form className="inline-form" onSubmit={onCreate}>
          <input placeholder="Full name" value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} required />
          <input placeholder="Document no." value={form.documentNo} onChange={(e) => setForm({ ...form, documentNo: e.target.value })} required />
          <input placeholder="Nationality" value={form.nationality} onChange={(e) => setForm({ ...form, nationality: e.target.value })} required />
          <button type="submit">Create</button>
        </form>
      </HasPermission>
      <div className="toolbar">
        <HasPermission permission="passenger.export">
          <button
            className="ghost"
            onClick={async () => {
              const exported = await api.exportPassengers();
              alert(`Exported ${exported.rows.length} rows`);
            }}
          >
            Export
          </button>
        </HasPermission>
      </div>
      <table>
        <thead>
          <tr>
            <th>Name</th>
            <th>Document</th>
            <th>Nationality</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={row.id}>
              <td>{row.fullName}</td>
              <td>{row.documentNo}</td>
              <td>{row.nationality}</td>
              <td>
                <HasPermission permission="passenger.delete">
                  <button
                    className="ghost compact"
                    onClick={async () => {
                      await api.deletePassenger(row.id);
                      await refresh();
                    }}
                  >
                    Delete
                  </button>
                </HasPermission>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export function ReportsPage() {
  const [summary, setSummary] = useState<string>("Loading…");
  useEffect(() => {
    api
      .reports()
      .then((r) => setSummary(`${r.title}: ${r.total} passengers`))
      .catch((err: Error) => setSummary(err.message));
  }, []);

  return (
    <div>
      <p className="kicker">Reports</p>
      <h1>Operational reports</h1>
      <p className="lede">{summary}</p>
      <HasPermission
        permission="report.export"
        fallback={<p className="muted">Export is hidden. John has Supervisor but an explicit user DENY on <code>report.export</code>.</p>}
      >
        <button
          onClick={async () => {
            const exported = await api.exportReports();
            alert(exported.content);
          }}
        >
          Export report
        </button>
      </HasPermission>
    </div>
  );
}
