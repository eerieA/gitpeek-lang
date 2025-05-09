# Endpoints

#### {depl_svr}/api/GitHubStats/{username}
<table style="max-width: 70%; width: auto; border-collapse: collapse;">
  <tbody>
    <tr>
      <td>Param</td>
      <td>Type</td>
      <td>Required</td>
      <td>Description</td>
    </tr>
    <tr>
      <td><code>username</code></td>
      <td><code>string</code></td>
      <td>✅</td>
      <td>The GitHub username for which the data is retrieved. If = `self`, it will return result for all public and private of the user to whom the Github token belongs.</td>
    </tr>
    <tr>
      <td><code>noCache</code></td>
      <td><code>bool</code></td>
      <td>No</td>
      <td>Whether to use the internal JSON Cache. Default is false.</td>
    </tr>
  </tbody>
</table>

#### {depl_svr}/api/GitHubStats/{username}/graph
<table style="max-width: 70%; width: auto; border-collapse: collapse;">
  <tbody>
    <tr>
      <td>Param</td>
      <td>Type</td>
      <td>Required</td>
      <td>Description</td>
    </tr>
    <tr>
      <td><code>username</code></td>
      <td><code>string</code></td>
      <td>✅</td>
      <td>The GitHub username for which the graph is generated. If = `self`, it will return result for all public and private of the user to whom the Github token belongs.</td>
    </tr>
    <tr>
      <td><code>width</code></td>
      <td><code>int</code></td>
      <td>No</td>
      <td>The width of the graph in pixels.</td>
    </tr>
    <tr>
      <td><code>barHeight</code></td>
      <td><code>int</code></td>
      <td>No</td>
      <td>The height of the bar in the graph.</td>
    </tr>
    <tr>
      <td><code>lgItemWidth</code></td>
      <td><code>int</code></td>
      <td>No</td>
      <td>The max width of individual legend items in the graph.</td>
    </tr>
    <tr>
      <td><code>lgItemMaxCnt</code></td>
      <td><code>int</code></td>
      <td>No</td>
      <td>The cut-off number of legend items to display. E.g. 8 means top 8 most used languages.</td>
    </tr>
    <tr>
      <td><code>fontSize</code></td>
      <td><code>int</code></td>
      <td>No</td>
      <td>Font size for legend items. Default is 14.</td>
    </tr>
    <tr>
      <td><code>noCache</code></td>
      <td><code>bool</code></td>
      <td>No</td>
      <td>Whether to use the internal JSON Cache. Default is false.</td>
    </tr>
  </tbody>
</table>

Note: Any optional query parameter uses a default value if unspecified.

# Deploying

## On [Render](https://render.com/)

Choose Docker as deployment language. Set environment variables:
  - `GH_AC_TOKEN` to get higher rate quota ([GitHub Docs](https://docs.github.com/en/rest/rate-limit/rate-limit?apiVersion=2022-11-28)).
  - `CACHE_TIMER` to set the JSON cache expiration time. It can only be integer, the unit is hour and default value is 24 (hours).

After deployment goes live, the endpoint can be called. A typical use case is to insert it into a Markdown rendered by GitHub:
  
  ```
  ![GitHub Language Statistics](https://<your-depl>.onrender.com/api/GitHubStats/<username>/graph?barHeight=15)
  ```

Please note that, since GitHub uses Camo *(camo.githubusercontent.com)* cache for external assets, and this app does not intend to implement cache busting, you would need to manually edit the Markdown to update the generated image each time. Therefore we recommend using an automation tool like GitHub Action to periodically re-fetch, and put the fetched image's relative path in the Markdown instead like so:
  ```
  ![GitHub Language Statistics](./assets/github-language-stats.svg)
  ```
. Below is a sample GitHub Action yml template.

<details>
<summary>Sample GitHub Action file: 🗒️update-stats.yml.</summary>

    name: Update GitHub Language Stats

    on:
      schedule:
        - cron: '0 */24 * * *' # Run every 24 hours
      workflow_dispatch:       # Allow manual trigger

    permissions:
      contents: write

    jobs:
      update-stats:
        runs-on: ubuntu-latest
        steps:
          - uses: actions/checkout@v4

          - name: Fetch stats from gitpeek-lang
            run: |
              mkdir -p assets
              curl "https://<your-depl>.onrender.com/api/GitHubStats/<username>/graph?barHeight=15" > assets/github-language-stats.svg

          - name: Commit and push if changed
            run: |
              git config user.name github-actions
              git config user.email github-actions@github.com
              git add assets/github-language-stats.svg
              git diff --quiet && git diff --staged --quiet || git commit -m "Update GitHub language stats"
              git push
</details>

## On Azure

Can be deployed without Docker containerization. Rough steps:
  1. Choose an Azure subscription.
  2. Create a Resource Group, an App Service plan and an App Service associated with it.
  3. Set environment variables as in deployment on Render.
  4. Locally publish using commands like    
      ```
      dotnet publish ./gitpeek-lang.csproj -c Release --no-restore --output ./publish
      ```
Azure CLI deployment command then can be:
  ```
  az webapp up --name <your-app-service> --runtime "dotnet:8" --resource-group <your-resource-group> --plan <your-plan>
  ```
.

# Building & running

## Local without docker

`dotnet run`

- If you want to use a GitHub token (recommended), set environment variable GH_AC_TOKEN. For example on Windows command line, that could be done by:

    set GH_AC_TOKEN=<your_github_token> && dotnet run

## Local with docker

`docker-compose up --build`

- `--build` is only necessary if there are source code changes.


<br>

# Appendices

## Appendix: Dev log
<details>
<summary>Jan 22, 2025: Deployment css and js files</summary>
It seems we need to use smth like Libman to restore dependencies such as bootstrap.bundle.min.js before deploying through a provide like render.com. Also need to correct paths in `_Layout.cshtml`. Otherwise there will be 404s when retrieving them.
</details>

<details>
<summary>Jan 18, 2025: Azure cost</summary>
I only have 1 API Management rule and 1 App Service. Azure's predicted cost is $68.71 per about a month. This probably be good for bulk management but will not be worth it for micro apps like this one.
<br>
<img alt="screenshot" src="https://live.staticflickr.com/65535/54269434801_40f2951791_b.jpg" width="320">

</details>

## Appendix: Acronyms

depl: deployment

svr: server

param: parameter
