# OIDC setup

The Razor client uses authorization code flow with PKCE and stores the resulting access token in the server-side authentication cookie. The identity provider must issue that access token with audience `ctrleat-storage-locations` and the `storage-locations.read` and `storage-locations.write` scopes. The application does not invent an OAuth audience parameter because the supported mechanism is identity-provider-specific.

Configure the provider's client registration and supply `Authentication:ClientSecret` through user secrets or deployment environment configuration when the provider requires a confidential client. Production must provide HTTPS `Authentication:Authority` and `StorageLocationApi:BaseUrl` values; localhost HTTP values are development-only.