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

**Symptom:** `npm run dev` or `npm run build` exits with an error.

**Resolution:**

1. Confirm Node.js 20+ and npm are installed:
   ```bash
   node --version
   npm --version
   ```
2. Use the direct dotnet command as a fallback:
   ```bash
   dotnet watch --project backend/PriorityHub.Ui/PriorityHub.Ui.csproj run
   ```

## No Data in Dashboard

**Symptom:** The dashboard loads but shows no work items.

**Resolution:**

1. Open **Settings** and verify that at least one connector is configured.
2. Check that the provider credentials and query (WIQL / JQL / board ID) are valid.
3. Confirm network connectivity to the provider APIs.

## Missing Local Config

**Symptom:** `config/providers.local.json` does not exist or is empty.

**Resolution:**

- The app starts normally without this file and returns an empty dashboard.
- Add connectors via **Settings** to create the file.
- Do not commit `config/providers.local.json` to version control.

## Connector Shows as Unhealthy

**Symptom:** A connector displays an error or "unhealthy" status on the dashboard.

**Resolution:**

1. Verify the credentials for that connector in **Settings**.
2. Test connectivity to the provider from the same host.
3. Check that the PAT / API token / OAuth token has not expired.
4. Review application logs for detailed error messages.

## Related

- [Configuration](../configuration/README.md) – provider field reference.
- [Features](../features/README.md) – overview of dashboard behavior.
