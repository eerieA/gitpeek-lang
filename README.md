# Endpoints

#### {depl_svr}/api/GitHubStats/{username}
<table style="max-width: 70%; width: auto; border-collapse: collapse;">
  <tbody>
    <tr>
      <td>Query Param</td>
      <td>Type</td>
      <td>Required</td>
      <td>Description</td>
    </tr>
    <tr>
      <td><code>username</code></td>
      <td><code>string</code></td>
      <td>✅</td>
      <td>The GitHub username for which the data is retrieved.</td>
    </tr>
  </tbody>
</table>

#### {depl_svr}/api/GitHubStats/{username}/graph
<table style="max-width: 70%; width: auto; border-collapse: collapse;">
  <tbody>
    <tr>
      <td>Query Param</td>
      <td>Type</td>
      <td>Required</td>
      <td>Description</td>
    </tr>
    <tr>
      <td><code>username</code></td>
      <td><code>string</code></td>
      <td>✅</td>
      <td>The GitHub username for which the graph is generated.</td>
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
      <td>The max number of legend items to display.</td>
    </tr>
  </tbody>
</table>

Note: Any optional query parameter uses a default value if unspecified.

# Deploying

Currently tested to be deployable on render.com. Set env var GH_AC_TOKEN to get higher rate quota ([GitHub Docs](https://docs.github.com/en/rest/rate-limit/rate-limit?apiVersion=2022-11-28)).

# Building & running

Local without docker: `dotnet run`
- If want to input GitHub token (you probably want to), set env var GH_AC_TOKEN. For example on Windows command line, that could be done by:

    set GH_AC_TOKEN=<your_github_token> && dotnet run

Local with docker: `docker-compose up --build`
- --build is only necessary if there are source code changes.

## Dev log
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
