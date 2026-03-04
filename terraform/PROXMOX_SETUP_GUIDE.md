# Proxmox Setup Guide for Terraform

This guide follows the setup required to use the Proxmox Terraform provider with the `terraform@pve!provider` user.

## 1. Create User and Role

Run these commands on your Proxmox host shell:

```bash
# Create the user
pveum user add terraform@pve --password YOUR_SECURE_PASSWORD

# Create a custom role for Terraform (Permissions limited to what's needed)
pveum role add TerraformRole -privs "VM.Allocate, VM.Clone, VM.Config.CDROM, VM.Config.CPU, VM.Config.Cloudinit, VM.Config.Disk, VM.Config.HWType, VM.Config.Memory, VM.Config.Network, VM.Config.Options, VM.Monitor, VM.Audit, VM.PowerMgmt, VM.Console, VM.Snapshot, VM.Backup, VM.Migrate, Datastore.Allocate, Datastore.AllocateSpace, Datastore.Audit, Sys.Audit, Sys.Console, Sys.Modify, Pool.Allocate, SDN.Use"

# Assign the role to the user
pveum aclmod / -user terraform@pve -role TerraformRole
```

## 2. API Token Creation

The user already mentioned they set up `terraform@pve!provider`. If you haven't yet, create the API token:

```bash
pveum user token add terraform@pve provider --privsep=0
```

> [!IMPORTANT]
> Save the token value immediately. It will look like `terraform@pve!provider=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`.

## 3. Terraform Configuration

Update `terraform/terraform.tfvars` with:
- `proxmox_api_url`: Your Proxmox host URL (e.g., `https://192.168.0.169:8006/`)
- `proxmox_api_token`: The full token string.
- `proxmox_template_id`: The template to use (e.g., `local:vztmpl/debian-12-standard_...`).

## 4. Run Terraform Locally

```powershell
cd terraform
terraform init
terraform plan
terraform apply
```

## 5. Configure GitHub Secrets

To enable automated deployment via GitHub Actions, add the following secrets to your repository (`Settings > Secrets and variables > Actions`):

| Secret Name | Description | Example Value |
|-------------|-------------|---------------|
| `PROXMOX_API_URL` | The URL of your Proxmox host | `https://192.168.0.169:8006/` |
| `PROXMOX_API_TOKEN` | The full API token string | `terraform@pve!provider=xxxx...` |
| `PROXMOX_NODE` | The name of the node (pve) | `proxmox` |
| `PROXMOX_TEMPLATE_ID` | The Debian template string | `local:vztmpl/debian-12...` |
| `CONTAINER_PASSWORD` | SSH password for the container | `YourSecurePassword` |
| `APP_ENV_FILE` | The full content of your `.env` file | (See below) |
| `TS_OAUTH_CLIENT_ID` | Tailscale OAuth Client ID | (Already defined) |
| `TS_OAUTH_CLIENT_SECRET` | Tailscale OAuth Client Secret | (Already defined) |

> [!TIP]
> `ghcr_username` and `ghcr_pat` are handled automatically by the GitHub Actions using `${{ github.actor }}` and `${{ secrets.GITHUB_TOKEN }}`.

## 6. How to create a GHCR PAT

To allow the LXC container to pull your private Docker image, you need a Personal Access Token (PAT).

1.  Go to **GitHub Settings** > **Developer Settings** > **Personal access tokens** > **Tokens (classic)**.
2.  Click **Generate new token (classic)**.
3.  Select the following scopes:
    - **`read:packages`**: Required to pull the image from your Proxmox container.
    - **`write:packages`**: (Optional) If you want to push from your local machine.
4.  Copy the token immediately.

> [!TIP]
> In GitHub Actions, the `GITHUB_TOKEN` is used automatically, so you only need this PAT for local `terraform apply` runs.

## 7. Container Login and Verification

By default, the container follows these login details:

- **User**: `root`
- **Password**: As defined in `container_password` in your `terraform.tfvars`.
- **IP Address**: `192.168.0.123`

### Verifying the Application

The Terraform script automatically installs Docker and runs your application. Once the apply is complete, verify it is running:

1.  **Check the logs**: On your Proxmox host, run `pct enter 123` then `docker ps`.
2.  **Test the endpoint**:
    ```bash
    curl http://192.168.0.123:5000/api/health
    ```

### Accessing via Shell

If you need to enter the container manually:

```bash
pct enter 123
```
