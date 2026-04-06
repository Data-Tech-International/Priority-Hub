# Troubleshooting

## Build or SDK Mismatch

**Symptom:** `dotnet build` fails with a framework or SDK version error.

**Resolution:**

1. Confirm .NET 10 is installed:
   ```bash
   dotnet --version
   ```
2. If the version is not 10.x, install the [.NET 10 SDK](https://dotnet.microsoft.com/download).
3. Re-run the build:
   ```bash
   dotnet build PriorityHub.sln
   ```

## Script Command Failures

**Symptom:** `dotnet watch` or `dotnet build` exits with an error.

**Resolution:**

1. Confirm .NET 10 SDK is installed:
   ```bash
   dotnet --version
   ```
2. Re-run the command:
   ```bash
   dotnet watch --project backend/PriorityHub.Ui/PriorityHub.Ui.csproj run
   ```

## No Data in Dashboard

**Symptom:** The dashboard loads but shows no work items.

**Resolution:**

1. Open **Settings** and verify that at least one connector is configured and enabled.
2. Check that the provider credentials and query (WIQL / JQL / board ID) are valid.
3. Check the **connector health** status at the top of the dashboard for any error badges.
4. Confirm network connectivity to the provider APIs from the server host.

## Missing Local Config

**Symptom:** `config/providers.local.json` does not exist or is empty.

**Resolution:**

- The app starts normally without this file and returns an empty dashboard.
- Add connectors via **Settings** to create the file.
- Do not commit `config/providers.local.json` to version control.

---

## Connector-Specific Issues

### Azure DevOps: "returned HTML instead of JSON"

**Symptom:** Error message: _"Azure DevOps returned HTML instead of JSON. The sign-in token is not valid for Azure DevOps."_

**Cause:** The Microsoft OAuth token is not authorized for Azure DevOps, or the PAT has expired.

**Resolution:**

1. Sign out and sign back in with Microsoft to refresh the OAuth token.
2. If using a PAT, verify it has not expired and has **Work Items (Read)** scope:
   - Go to `https://dev.azure.com/<your-org>/_usersSettings/tokens`.
   - Regenerate or create a new PAT.
   - Update the PAT field in **Settings**.
3. See [Create a PAT](https://learn.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate) for guidance.

### Azure DevOps: "Azure DevOps authentication required"

**Symptom:** Error message: _"Azure DevOps authentication required. Sign in with Microsoft to grant access, or add a Personal Access Token for this connection."_

**Resolution:**

- Either sign in with Microsoft, or open **Settings** and add a PAT to the connector.

### Azure DevOps: "WIQL query returned no results"

**Symptom:** Connector shows healthy but the dashboard is empty.

**Resolution:**

1. Run the same WIQL query in the [Azure DevOps query editor](https://learn.microsoft.com/en-us/azure/devops/boards/queries/using-queries) to verify it returns items.
2. Check that `@project` resolves to the correct project.
3. Check that work items are assigned to your account when using `@me`.

---

### Jira: 401 Unauthorized

**Symptom:** Connector shows as unhealthy; error mentions authentication failure.

**Resolution:**

1. Verify the **Email** field matches your Atlassian account email exactly.
2. Confirm the API token has not been revoked:
   - Go to `https://id.atlassian.com/manage-profile/security/api-tokens`.
   - Revoke the old token and generate a new one.
   - Update the API token field in **Settings**.
3. See [Manage API tokens](https://support.atlassian.com/atlassian-account/docs/manage-api-tokens-for-your-atlassian-account/) for guidance.

### Jira: "Missing Jira base URL, email, or API token"

**Symptom:** Connector fails immediately on save.

**Resolution:**

- Ensure all three required fields (Base URL, Email, API token) are filled in.
- The Base URL must include the scheme, e.g., `https://yourorg.atlassian.net` (not just `yourorg.atlassian.net`).

### Jira: JQL query returns no results

**Symptom:** Connector shows healthy but the dashboard is empty.

**Resolution:**

1. Test the JQL query directly in your Jira instance (open any project → **Filters** → **Advanced issue search**).
2. Confirm `currentUser()` resolves to your account.
3. Try a simpler baseline query:
   ```jql
   assignee = currentUser() AND statusCategory != Done ORDER BY updated DESC
   ```

---

### Trello: 401 Unauthorized / "invalid token"

**Symptom:** Connector shows as unhealthy; error mentions invalid key or token.

**Resolution:**

1. Verify the **API key** and **Token** in **Settings** match the values at [https://trello.com/app-key](https://trello.com/app-key).
2. Trello tokens can be revoked by visiting `https://trello.com/your-account/account` → **Applications**.
3. Generate a fresh token at [https://trello.com/app-key](https://trello.com/app-key) and update **Settings**.

### Trello: Board ID not found

**Symptom:** Error mentioning the board was not found or access denied.

**Resolution:**

1. Open the Trello board in a browser and copy the 8-character ID from the URL:
   ```
   https://trello.com/b/AbCd1234/board-name
                         ^^^^^^^^
   ```
2. Make sure your Trello account has access to the board.

---

### GitHub: "Missing GitHub token"

**Symptom:** Error message: _"Missing GitHub token. Provide a PAT or sign in with GitHub."_

**Resolution:**

- Either sign in with GitHub, or open **Settings** and add a fine-grained or classic PAT.
- The PAT needs **Issues (Read)** permission for the target repository.
- See [Create a PAT](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens) for guidance.

### GitHub: 404 on repository

**Symptom:** Connector shows as unhealthy; error mentions repository not found.

**Resolution:**

1. Verify the **Owner** and **Repository** fields are correct (case-sensitive).
2. Confirm your GitHub account or token has read access to the repository.
3. For private repositories, ensure the PAT includes **repo** (classic) or **Issues: Read** (fine-grained) scope.

---

### Microsoft Tasks / Outlook Flagged Mail: "Missing Microsoft sign-in token"

**Symptom:** Connector fails with a missing token error.

**Resolution:**

- These connectors require an active Microsoft sign-in.
- Sign in with Microsoft from the application and reload the dashboard.

## Connector Shows as Unhealthy

**Symptom:** A connector displays an error or "unhealthy" status on the dashboard.

**General resolution steps:**

1. Verify the credentials for that connector in **Settings**.
2. Test connectivity to the provider from the same host.
3. Check that the PAT / API token / OAuth token has not expired.
4. Review application logs for detailed error messages.
5. Refer to the connector-specific sections above for targeted guidance.

## Related

- [Configuration](../configuration/README.md) – provider field reference and credential setup.
- [Features](../features/README.md) – overview of dashboard behavior and scoring.

---

## Container Issues

### Container startup crash: missing environment variables

**Symptom:** Container exits immediately with an `InvalidOperationException` mentioning `ConfigStore:ConnectionString`.

**Resolution:**

1. Ensure `ConfigStore__Provider` is set to `Postgres` and `ConfigStore__ConnectionString` is a valid PostgreSQL connection string. Example run command:
   ```bash
   docker run --rm -it \
     -e ConfigStore__Provider=Postgres \
     -e ConfigStore__ConnectionString="Host=<db-host>;Database=priorityhub;Username=priorityhub;Password=<password>" \
     -p 8080:8080 \
     priority-hub:local
   ```
2. Check container logs for the exact error:
   ```bash
   docker logs <container-id>
   ```

### Container startup crash: database not reachable

**Symptom:** Container starts but exits shortly after, with a connection refused or timeout error.

**Resolution:**

1. Confirm the PostgreSQL container is running and healthy:
   ```bash
   docker compose ps
   ```
2. Use the appropriate hostname depending on your platform:
   - **Linux with `--network host`**: the container uses the host's network stack directly, so `localhost` reaches the host's PostgreSQL. No `-p` flag is needed in this mode.
   - **macOS / Windows (Docker Desktop)**: `--network host` is not supported. Use `host.docker.internal` as the database hostname and publish the port with `-p 8080:8080`:
     ```
     Host=host.docker.internal;Database=priorityhub;Username=priorityhub;Password=dev_password
     ```

### Container startup crash: schema migration mismatch

**Symptom:** Application logs show an error about unapplied migrations in a non-Development environment.

**Resolution:**

Apply the pending migration scripts manually before restarting the container:

```bash
psql -h <db-host> -U priorityhub -d priorityhub \
  -f backend/PriorityHub.Api/Data/Migrations/0001_initial_schema.sql
```

### Azure App Service: Blazor interactivity broken (dashboard/settings not responding)

**Symptom:** The app loads but interactive elements (Settings toggles, tab switching, drag-and-drop) do not respond after deployment to Azure App Service.

**Cause:** Blazor Server requires a persistent WebSocket connection for SignalR. Azure App Service disables WebSockets by default.

**Resolution:**

Enable WebSockets in Azure App Service:

- **Azure Portal:** Navigate to **App Service → Configuration → General settings** and set **Web sockets** to **On**, then click **Save**.
- **Bicep/ARM:** Add `webSocketsEnabled: true` to the `siteConfig` block in your infrastructure template.

### Azure App Service: OAuth redirect URI mismatch or login loop

**Symptom:** After sign-in, the app redirects to an incorrect URL, loops back to the login page, or the OAuth provider returns `redirect_uri_mismatch` when deployed on Azure App Service or behind a reverse proxy.

**Cause:** ASP.NET Core needs to know the public-facing scheme and host when building redirect URIs. Without forwarded-headers middleware, it uses the internal address. Additionally, the callback URLs registered in your OAuth app settings must exactly match the paths the application uses.

**Resolution:**

The application explicitly calls `UseForwardedHeaders()` in the middleware pipeline, so reverse-proxy headers are honored automatically. If the issue persists, also set the following application setting in Azure App Service to enable the built-in ASP.NET Core forwarded-headers handling as an additional layer:

| Name | Value |
|------|-------|
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | `true` |

Register the exact callback URLs in your OAuth app settings:

- GitHub: `https://<your-host>/api/auth/callback/github`
- Microsoft: `https://<your-host>/api/auth/callback/microsoft`

Callback paths are fixed and cannot be changed via configuration.

### Port conflict on startup

**Symptom:** `docker run` fails with `address already in use` on port `8080`.

**Resolution:**

Map the container port to a different host port:

```bash
docker run ... -p 8888:8080 priority-hub:local
```

Then access the application at `http://localhost:8888`.

