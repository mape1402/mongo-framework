# NuGet Release

This fork publishes the main package as `MongoFramework.Fixed`.

## One-time setup

1. In nuget.org, create a Trusted Publishing policy for `MongoFramework.Fixed`.
2. Configure the policy for this GitHub repository and workflow:
   - Repository owner: `mape1402`
   - Repository name: `mongo-framework`
   - Workflow file: `build.yml`
   - Environment: leave empty
3. Add a GitHub repository variable named `NUGET_USER` with the nuget.org username that created the Trusted Publishing policy.
4. Ensure GitHub Actions has permission to publish packages:
   - Repository Settings
   - Actions
   - General
   - Workflow permissions
   - Read and write permissions

## Release process

1. Add a section to `CHANGELOG.md` for the release tag:

   ```markdown
   ## [v2.0.0]
   ```

2. Create or update `.release` with exactly one non-empty line:

   ```text
   v2.0.0
   ```

3. Commit and push the changelog and `.release` changes to `main`.

Publishing happens when `.release` changes on `main`. The workflow validates the build first, then checks:

- `.release` contains exactly one non-empty line
- the tag matches `vX.Y.Z`
- `CHANGELOG.md` contains a matching `## [vX.Y.Z]` section
- the changelog section is not empty

The workflow then creates `releases/vX.Y.Z`, creates or updates the tag, creates a GitHub Release, and publishes packages.

## Manual release rerun

Manual production releases must run from a branch named `releases/vX.Y.Z`, for example:

   ```powershell
   git switch releases/v2.0.0
   ```

Then run the `Build and Release` workflow with `workflow_dispatch`.

## Local package check

```powershell
dotnet pack src\MongoFramework\MongoFramework.csproj -c Release --no-restore
```
