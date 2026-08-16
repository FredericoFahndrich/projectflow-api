# Security policy

Please do not open public issues for suspected vulnerabilities. Report them privately through GitHub's **Security → Report a vulnerability** flow.

## Operational checklist

- Replace every development credential before deploying.
- Supply `Jwt__Secret`, database credentials and bootstrap credentials through a secret manager.
- Serve the API only behind HTTPS.
- Disable or protect Swagger UI when the deployment policy requires it.
- Store attachments in object storage and add malware scanning for production workloads.
- Back up PostgreSQL and the attachment store together.

