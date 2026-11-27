import { useEffect, useRef, useState } from "react";
import type { ChangeEvent, FormEvent } from "react";
import type { User } from "oidc-client-ts";
import { ApiError, readGoal, readProfilePicture, saveGoal, uploadProfilePicture } from "./api";
import { userManager } from "./auth";
import "./styles.css";

export default function App() {
  const [user, setUser] = useState<User | null>(null);
  const [authLoading, setAuthLoading] = useState(true);
  const [authError, setAuthError] = useState<string | null>(null);
  const [goal, setGoal] = useState<number | null>(null);
  const [draft, setDraft] = useState("");
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [profilePicture, setProfilePicture] = useState<Blob | null>(null);
  const [profilePictureUrl, setProfilePictureUrl] = useState<string | null>(null);
  const [pictureLoading, setPictureLoading] = useState(false);
  const [pictureUploading, setPictureUploading] = useState(false);
  const [pictureError, setPictureError] = useState<string | null>(null);
  const initialized = useRef(false);

  useEffect(() => {
    if (initialized.current) return;
    initialized.current = true;

    const initialize = async () => {
      try {
        if (window.location.pathname === "/callback") {
          await userManager.signinRedirectCallback();
          window.history.replaceState({}, "", "/");
        }
        setUser(await userManager.getUser());
      } catch (reason) {
        setAuthError(reason instanceof Error ? reason.message : "Sign-in could not be completed.");
      } finally {
        setAuthLoading(false);
      }
    };

    void initialize();
  }, []);

  useEffect(() => {
    if (!user) return;
    setLoading(true);
    setError(null);
    void readGoal(user)
      .then((response) => {
        setGoal(response.calorieGoal);
        setDraft(response.calorieGoal === null ? "" : String(response.calorieGoal));
      })
      .catch((reason) => setError(toMessage(reason, "The saved goal could not be loaded.")))
      .finally(() => setLoading(false));
  }, [user]);

  useEffect(() => {
    if (!user) return;
    let cancelled = false;
    setPictureLoading(true);
    setPictureError(null);
    void readProfilePicture(user)
      .then((picture) => {
        if (!cancelled) setProfilePicture(picture);
      })
      .catch((reason) => {
        if (!cancelled) setPictureError(toMessage(reason, "Your profile picture could not be loaded."));
      })
      .finally(() => {
        if (!cancelled) setPictureLoading(false);
      });
    return () => { cancelled = true; };
  }, [user]);

  useEffect(() => {
    if (!profilePicture) {
      setProfilePictureUrl(null);
      return;
    }
    const url = URL.createObjectURL(profilePicture);
    setProfilePictureUrl(url);
    return () => URL.revokeObjectURL(url);
  }, [profilePicture]);

  const signIn = () => void userManager.signinRedirect();
  const signOut = async () => {
    await userManager.signoutRedirect();
  };

  const uploadPicture = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.currentTarget.files?.[0];
    event.currentTarget.value = "";
    if (!file || !user) return;

    if (!["image/jpeg", "image/png", "image/webp"].includes(file.type)) {
      setPictureError("Choose a JPEG, PNG, or WebP image.");
      return;
    }
    if (file.size === 0 || file.size > 5 * 1024 * 1024) {
      setPictureError("Choose an image that is no larger than 5 MiB.");
      return;
    }

    setPictureUploading(true);
    setPictureError(null);
    try {
      await uploadProfilePicture(user, file);
      setProfilePicture(file);
    } catch (reason) {
      setPictureError(toMessage(reason, "The profile picture could not be uploaded."));
    } finally {
      setPictureUploading(false);
    }
  };

  const save = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const value = Number(draft);
    if (!Number.isInteger(value) || value <= 0) {
      setError("Enter a positive whole number of calories.");
      return;
    }
    if (!user) return;

    setSaving(true);
    setError(null);
    try {
      const response = await saveGoal(user, value);
      setGoal(response.calorieGoal);
      setDraft(response.calorieGoal === null ? "" : String(response.calorieGoal));
    } catch (reason) {
      setError(toMessage(reason, "The goal could not be saved. Your previous saved goal is unchanged."));
    } finally {
      setSaving(false);
    }
  };

  if (authLoading) return <main className="card" aria-busy="true">Loading sign-in…</main>;
  if (!user) {
    return (
      <main className="card">
        <h1>Ctrl+Eat</h1>
        <p>Sign in to view and update your daily calorie goal.</p>
        {authError && <p className="error" role="alert">{authError}</p>}
        <button type="button" onClick={signIn}>Sign in</button>
      </main>
    );
  }

  return (
    <main className="card">
      <header className="header">
        <div><h1>Daily calorie goal</h1><p>Only your profile is accessible.</p></div>
        <button className="secondary" type="button" onClick={() => void signOut}>Sign out</button>
      </header>
      <section className="profile-picture" aria-labelledby="profilePictureHeading">
        <h2 id="profilePictureHeading">Profile picture</h2>
        {pictureLoading ? <p aria-busy="true">Loading your profile picture…</p> : profilePictureUrl ? (
          <img className="profile-picture-preview" src={profilePictureUrl} alt="Your profile" />
        ) : <p className="picture-placeholder">No profile picture uploaded.</p>}
        <label htmlFor="profilePictureFile">Choose a picture</label>
        <input id="profilePictureFile" type="file" accept="image/jpeg,image/png,image/webp"
          onChange={uploadPicture} disabled={pictureLoading || pictureUploading} />
        <p className="picture-hint">JPEG, PNG, or WebP. Maximum size: 5 MiB.</p>
        {pictureUploading && <p aria-live="polite">Uploading profile picture…</p>}
        {pictureError && <p className="error" role="alert">{pictureError}</p>}
      </section>
      {loading ? <p aria-busy="true">Loading your saved goal…</p> : (
        <>
          <p className="current">{goal === null ? "No goal saved yet." : `Current saved goal: ${goal} calories`}</p>
          <form onSubmit={save} noValidate>
            <label htmlFor="calorieGoal">Daily calorie goal</label>
            <input id="calorieGoal" name="calorieGoal" type="number" min="1" step="1" required
              value={draft} onChange={(event) => setDraft(event.target.value)} />
            <button type="submit" disabled={saving}>{saving ? "Saving…" : "Save goal"}</button>
          </form>
        </>
      )}
      {error && <p className="error" role="alert">{error}</p>}
    </main>
  );
}

function toMessage(reason: unknown, fallback: string) {
  if (reason instanceof ApiError) return reason.message;
  if (reason instanceof TypeError) return "The API could not be reached. Check that the local API is running.";
  return reason instanceof Error ? reason.message : fallback;
}