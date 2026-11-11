import { useEffect, useRef, useState } from "react";
import type { FormEvent } from "react";
import type { User } from "oidc-client-ts";
import { ApiError, createLocation, deleteLocation, listLocations, updateLocation } from "./api";
import type { StorageLocation, StorageLocationDraft } from "./api";
import { userManager } from "./auth";

const emptyDraft: StorageLocationDraft = { name: "", type: "", minimumTemperature: 0, maximumTemperature: 0, ingredientIds: [] };

export default function App() {
  const [user, setUser] = useState<User | null>(null);
  const [authLoading, setAuthLoading] = useState(true);
  const [authError, setAuthError] = useState<string | null>(null);
  const [locations, setLocations] = useState<StorageLocation[]>([]);
  const [draft, setDraft] = useState<StorageLocationDraft>(emptyDraft);
  const [editing, setEditing] = useState<StorageLocation | null>(null);
  const [filterName, setFilterName] = useState("");
  const [filterType, setFilterType] = useState("");
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const initialized = useRef(false);

  useEffect(() => {
    if (initialized.current) return; initialized.current = true;
    void (async () => {
      try { if (location.pathname === "/callback") { await userManager.signinRedirectCallback(); history.replaceState({}, "", "/"); } setUser(await userManager.getUser()); }
      catch (reason) { setAuthError(message(reason, "Sign-in could not be completed.")); }
      finally { setAuthLoading(false); }
    })();
  }, []);

  useEffect(() => { if (!user) return; void loadLocations(user, filterName, filterType); }, [user, filterName, filterType]);

  const loadLocations = async (currentUser: User, name = filterName, type = filterType) => {
    setLoading(true); setError(null);
    try { setLocations(await listLocations(currentUser, name, type)); }
    catch (reason) { setError(message(reason, "Storage locations could not be loaded.")); }
    finally { setLoading(false); }
  };
  const submit = async (event: FormEvent) => {
    event.preventDefault(); if (!user) return;
    if (!draft.name.trim() || !draft.type.trim()) { setError("Name and type are required."); return; }
    if (draft.minimumTemperature > draft.maximumTemperature) { setError("Minimum temperature cannot exceed maximum temperature."); return; }
    setSaving(true); setError(null);
    try { if (editing) await updateLocation(user, editing.id, { ...draft, name: draft.name.trim(), type: draft.type.trim() }); else await createLocation(user, { ...draft, name: draft.name.trim(), type: draft.type.trim() }); setEditing(null); setDraft(emptyDraft); await loadLocations(user); }
    catch (reason) { setError(message(reason, "The storage location could not be saved.")); }
    finally { setSaving(false); }
  };
  const remove = async (item: StorageLocation) => {
    if (!user || !window.confirm(`Delete ${item.name}?`)) return;
    setError(null); try { await deleteLocation(user, item.id); await loadLocations(user); } catch (reason) { setError(message(reason, "The storage location could not be deleted.")); }
  };

  if (authLoading) return <main className="card">Loading sign-in…</main>;
  if (!user) return <main className="card"><h1>Ctrl+Eat</h1><p>Sign in to manage storage locations.</p>{authError && <p className="error" role="alert">{authError}</p>}<button onClick={() => void userManager.signinRedirect()}>Sign in</button></main>;
  return <main className="page">
    <header className="header"><div><h1>Storage locations</h1><p>Manage temperature-controlled food storage.</p></div><button className="secondary" onClick={() => void userManager.signoutRedirect()}>Sign out</button></header>
    {error && <p className="error" role="alert">{error}</p>}
    <section className="card"><h2>Search</h2><div className="filters"><label>Name<input value={filterName} onChange={e => setFilterName(e.target.value)} /></label><label>Type<input value={filterType} onChange={e => setFilterType(e.target.value)} /></label></div></section>
    <section className="card"><h2>{editing ? "Edit storage location" : "Add storage location"}</h2><form onSubmit={submit} noValidate><div className="filters"><label>Name<input required value={draft.name} onChange={e => setDraft({ ...draft, name: e.target.value })} /></label><label>Type<input required value={draft.type} onChange={e => setDraft({ ...draft, type: e.target.value })} /></label><label>Minimum temperature<input type="number" step="0.01" value={draft.minimumTemperature} onChange={e => setDraft({ ...draft, minimumTemperature: Number(e.target.value) })} /></label><label>Maximum temperature<input type="number" step="0.01" value={draft.maximumTemperature} onChange={e => setDraft({ ...draft, maximumTemperature: Number(e.target.value) })} /></label><label>Ingredient IDs<input inputMode="numeric" placeholder="1, 2, 3" value={draft.ingredientIds.join(", ")} onChange={e => setDraft({ ...draft, ingredientIds: parseIngredientIds(e.target.value) })} /></label></div><button disabled={saving}>{saving ? "Saving…" : editing ? "Update" : "Create"}</button>{editing && <button type="button" className="secondary" onClick={() => { setEditing(null); setDraft(emptyDraft); }}>Cancel</button>}</form></section>
    <section className="card"><h2>Locations</h2>{loading ? <p aria-busy="true">Loading locations…</p> : locations.length === 0 ? <p>No storage locations found.</p> : <div className="table-wrap"><table><thead><tr><th>Name</th><th>Type</th><th>Temperature range</th><th>Ingredients</th><th>Actions</th></tr></thead><tbody>{locations.map(item => <tr key={item.id}><td>{item.name}</td><td>{item.type}</td><td>{item.minimumTemperature}–{item.maximumTemperature}°</td><td>{item.ingredients.length}</td><td><button className="secondary" onClick={() => { setEditing(item); setDraft({ name: item.name, type: item.type, minimumTemperature: item.minimumTemperature, maximumTemperature: item.maximumTemperature, ingredientIds: item.ingredients.map(i => i.id) }); }}>Edit</button> <button className="danger" onClick={() => void remove(item)}>Delete</button></td></tr>)}</tbody></table></div>}</section>
  </main>;
}

function message(reason: unknown, fallback: string) { if (reason instanceof ApiError) return reason.message; if (reason instanceof TypeError) return "The API could not be reached. Check that the local API is running."; return reason instanceof Error ? reason.message : fallback; }
function parseIngredientIds(value: string) { return value.split(",").map(item => Number(item.trim())).filter(item => Number.isInteger(item) && item > 0); }
