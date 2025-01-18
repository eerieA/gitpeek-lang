# Endpoints

{depl_svr}/api/GitHubStats/{username}

{depl_svr}/api/GitHubStats/{username}/graph

# Setting up

1. Run `setup-cert.bat`, provide a password such as "pwd123456" for the SSL cert, and a `aspnetapp.pfx` will be generated in the app root.

2. Create a `.env` file in the app root if not exist, and define the password env variable like this in it:

    CERT_PASSWORD=pwd123456

3. Locally run `docker-compose up` and then visit `https://localhost:8443/` to test if the cert works.

Note: Currently only have setup-cert.bat (for Win), but more will be added for other systems.

# Building

Local without docker: `dotnet run`

Local with docker: `docker-compose up --build`

## Dev log
<details>
<summary>Jan 18, 2025: Azure cost</summary>
I only have 1 API Management rule and 1 App Service. Azure's predicted cost is $68.71 per about a month. This probably be good for bulk management but will not be worth it for micro apps like this one.
<br>
<img alt="screenshot" src="https://live.staticflickr.com/65535/54269434801_40f2951791_b.jpg" width="320">

</details>

## Appendix: Acronyms

depl: deployment

svr: server
