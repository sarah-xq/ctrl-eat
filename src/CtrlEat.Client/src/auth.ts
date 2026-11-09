import { UserManager, WebStorageStateStore } from "oidc-client-ts";

const authority = import.meta.env.VITE_OIDC_AUTHORITY ?? "http://localhost:8082/realms/ctrl-eat";

export const userManager = new UserManager({
  authority,
  client_id: import.meta.env.VITE_OIDC_CLIENT_ID ?? "ctrleat-client",
  redirect_uri: import.meta.env.VITE_OIDC_REDIRECT_URI ?? "http://localhost:5174/callback",
  post_logout_redirect_uri: import.meta.env.VITE_OIDC_POST_LOGOUT_REDIRECT_URI ?? "http://localhost:5174/",
  response_type: "code",
  scope: import.meta.env.VITE_OIDC_SCOPE ?? "openid storage-locations.read storage-locations.write",
  loadUserInfo: false,
  automaticSilentRenew: false,
  monitorSession: false,
  userStore: new WebStorageStateStore({ store: window.sessionStorage })
});