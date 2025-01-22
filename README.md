# Endpoints

{depl_svr}/api/GitHubStats/{username}

{depl_svr}/api/GitHubStats/{username}/graph

# Building

Local without docker: `dotnet run`

Local with docker: `docker-compose up --build`

## Pfx cert for HTTPS

Only if needed for developer testing.

1. Run `setup-cert.bat`, provide a password such as "pwd123456" for the SSL cert, and a `aspnetapp.pfx` will be generated in the app root.

2. Create a `.env` file in the app root if not exist, and define the password env variable like this in it:

    CERT_PASSWORD=pwd123456

3. Locally run `docker-compose up` and then visit `https://localhost:{port}/` to test if the cert works.

Note: Currently only have setup-cert.bat (for Win), but more will be added for other systems.

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
