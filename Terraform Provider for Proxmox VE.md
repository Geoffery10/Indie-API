Proxmox Provider¶
This provider for Terraform / OpenTofu is used for interacting with resources supported by Proxmox VE. The provider needs to be configured with the proper endpoint and credentials before it can be used.

Use the navigation to the left to read about the available resources.

Getting Started¶
To use this provider, you only need:

API access to your Proxmox VE server (endpoint URL + username/password or API token)
That's it for most use cases!

Example Usage¶
Minimal configuration (no SSH):


provider "proxmox" {
  endpoint = "https://10.0.0.2:8006/"

  # TODO: use terraform variable or remove the line, and use PROXMOX_VE_USERNAME environment variable
  username = "root@pam"
  # TODO: use terraform variable or remove the line, and use PROXMOX_VE_PASSWORD environment variable
  password = "the-password-set-during-installation-of-proxmox-ve"

  # because self-signed TLS certificate is in use
  insecure = true
}

Our ip in this case is: https://192.168.0.169:8006/

Authentication¶
The provider supports three authentication methods (in order of precedence):

API Token — recommended for production and CI/CD
Auth Ticket — for automated scripts with TOTP support
Username/Password — simplest, good for development
Danger

Hard-coding credentials into any Terraform configuration is not recommended. Use environment variables or a .tfvars file (add to .gitignore) instead.

Authentication Methods Comparison¶
Method	Use Case	Pros	Cons	Security Level
API Token	Production, CI/CD	- No password needed
- Fine-grained permissions
- Revocable	- Some operations not supported
- Requires SSH username config	High
Auth Ticket	Automated scripts	- Short-lived
- No password storage
- TOTP support	- More complex setup
- Needs periodic renewal	High
Username/Password	Development, Testing	- Full API support
- Simple setup	- Password in config/env
- Not revocable individually	Medium
Quick Examples¶
Here are examples for each authentication method:

API Token (Recommended for Production):


provider "proxmox" {
  endpoint  = "https://10.0.0.2:8006/"
  api_token = "terraform@pve!provider=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
}
Username/Password (Development/Testing):


provider "proxmox" {
  endpoint = "https://10.0.0.2:8006/"
  insecure = true
  username = "username@realm"
  password = "a-strong-password"
}
Auth Ticket (Automated Scripts):


provider "proxmox" {
  endpoint              = "https://10.0.0.2:8006/"
  auth_ticket          = "PVE:username@realm:12345678::some_base64_payload=="
  csrf_prevention_token = "12345678:some_blob"
}
A better approach is to extract these values into Terraform variables and reference them instead:


provider "proxmox" {
  endpoint = var.virtual_environment_endpoint

  # Choose one authentication method:
  api_token = var.virtual_environment_api_token
  # OR
  username  = var.virtual_environment_username
  password  = var.virtual_environment_password
  # OR
  auth_ticket           = var.virtual_environment_auth_ticket
  csrf_prevention_token = var.virtual_environment_csrf_prevention_token
}
The variable values can be provided via a separate .tfvars file (add it to .gitignore). See the Terraform documentation for more information.

Security Best Practices¶
Use API tokens in production — they're revocable and support fine-grained permissions
Never commit credentials to version control — use environment variables or .tfvars files (in .gitignore)
Use HTTPS with valid certificates — only set insecure = true in development environments
Apply least privilege — create tokens/users with minimal required permissions
Rotate credentials regularly
Environment Variables¶
Credentials can also be provided via environment variables instead of static arguments. For example:


provider "proxmox" {
  endpoint = "https://10.0.0.2:8006/"
}

export PROXMOX_VE_USERNAME="username@realm"
export PROXMOX_VE_PASSWORD='a-strong-password'
terraform plan
See the Argument Reference section for the supported variable names and use cases.

API Token Authentication¶
API tokens allow password-less authentication with the Proxmox API. If you already have a token, use it like this:


provider "proxmox" {
  endpoint  = "https://10.0.0.2:8006/"
  api_token = "user@realm!tokenid=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
}
Creating an API Token on the Proxmox Server¶
You can create an API Token via the Proxmox UI or the command line on the Proxmox host:

Create a user:


pveum user add terraform@pve
Create a role for the user (you can skip this step if you want to use any of the existing roles):


pveum role add Terraform -privs "Realm.AllocateUser, VM.PowerMgmt, VM.GuestAgent.Unrestricted, Sys.Console, Sys.Audit, Sys.AccessNetwork, VM.Config.Cloudinit, VM.Replicate, Pool.Allocate, SDN.Audit, Realm.Allocate, SDN.Use, Mapping.Modify, VM.Config.Memory, VM.GuestAgent.FileSystemMgmt, VM.Allocate, SDN.Allocate, VM.Console, VM.Clone, VM.Backup, Datastore.AllocateTemplate, VM.Snapshot, VM.Config.Network, Sys.Incoming, Sys.Modify, VM.Snapshot.Rollback, VM.Config.Disk, Datastore.Allocate, VM.Config.CPU, VM.Config.CDROM, Group.Allocate, Datastore.Audit, VM.Migrate, VM.GuestAgent.FileWrite, Mapping.Use, Datastore.AllocateSpace, Sys.Syslog, VM.Config.Options, Pool.Audit, User.Modify, VM.Config.HWType, VM.Audit, Sys.PowerMgmt, VM.GuestAgent.Audit, Mapping.Audit, VM.GuestAgent.FileRead, Permissions.Modify"
Warning

The list of available privileges has changed in PVE 9.0. The above list is only an example (and likely too permissive for most use cases). Please review and adjust to your needs. Refer to the privileges documentation for more details.

Assign the role to the previously created user:


pveum aclmod / -user terraform@pve -role Terraform
Create an API token for the user:


pveum user token add terraform@pve provider --privsep=0
Info

Make sure you copy the token value, as it will not be displayed again.

Refer to the PVE User Management documentation for more details.

The command outputs a table with the token ID and secret. Concatenate them into a single string (e.g., user@realm!tokenid=secret) for the api_token field or the PROXMOX_VE_API_TOKEN environment variable:


provider "proxmox" {
  endpoint  = var.virtual_environment_endpoint
  api_token = "terraform@pve!provider=xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
  insecure  = true
  ssh {
    agent    = true
    username = "terraform"
  }
}
Info

Not all Proxmox API operations are supported via API Token. You may see errors like error creating container: received an HTTP 403 response - Reason: Permission check failed (changing feature flags for privileged container is only allowed for root@pam) or error creating VM: received an HTTP 500 response - Reason: only root can set 'arch' config or Permission check failed (user != root@pam) when using API Token authentication, even when Administrator role or the root@pam user is used with the token. The workaround is to use password authentication for those operations.

Info

You can also configure additional Proxmox users and roles using virtual_environment_user and virtual_environment_role resources of the provider.

Pre-Authentication, or Passing an Authentication Ticket into the provider¶
It is possible to generate a session ticket with the API, and to pass the ticket and csrf_prevention_token into the provider using environment variables PROXMOX_VE_AUTH_TICKET and PROXMOX_VE_CSRF_PREVENTION_TOKEN (or provider's arguments auth_ticket and csrf_prevention_token). See more details in the Proxmox Wiki.

An example of using curl and jq to query the Proxmox API to get a Proxmox session ticket; it is also very easy to pass in a TOTP password this way:


provider "proxmox" {
  endpoint = "https://10.0.0.2:8006/"
}

#!/usr/bin/bash

## assume vars are set: PROXMOX_VE_ENDPOINT, PROXMOX_VE_USERNAME, PROXMOX_VE_PASSWORD
## end-goal: automatically set PROXMOX_VE_AUTH_TICKET and PROXMOX_VE_CSRF_PREVENTION_TOKEN

_user_totp_password='123456' ## optional TOTP password


proxmox_api_ticket_path='api2/json/access/ticket' ## cannot have double "//" - ensure endpoint ends with a "/" and this string does not begin with a "/", or vice-versa

## call the auth api endpoint
resp=$( curl -q -s -k --data-urlencode "username=${PROXMOX_VE_USERNAME}"  --data-urlencode "password=${PROXMOX_VE_PASSWORD}"  "${PROXMOX_VE_ENDPOINT}${proxmox_api_ticket_path}" )
auth_ticket=$( jq -r '.data.ticket' <<<"${resp}" )
resp_csrf=$( jq -r '.data.CSRFPreventionToken' <<<"${resp}" )

## check if the response payload needs a TFA (totp) passed, call the auth-api endpoint again
if [[ $(jq -r '.data.NeedTFA' <<<"${resp}") == 1 ]]; then
  resp=$( curl -q -s -k  -H "CSRFPreventionToken: ${resp_csrf}" --data-urlencode  "username=${PROXMOX_VE_USERNAME}" --data-urlencode "tfa-challenge=${auth_ticket}" --data-urlencode "password=totp:${_user_totp_password}"  "${PROXMOX_VE_ENDPOINT}${proxmox_api_ticket_path}" )
  auth_ticket=$( jq -r '.data.ticket' <<<"${resp}" )
  resp_csrf=$( jq -r '.data.CSRFPreventionToken' <<<"${resp}" )
fi


export PROXMOX_VE_AUTH_TICKET="${auth_ticket}"
export PROXMOX_VE_CSRF_PREVENTION_TOKEN="${resp_csrf}"

terraform plan

VM and Container ID Assignment¶
When creating VMs and Containers, you can specify the optional vm_id attribute to set the ID. If omitted, the provider generates a unique ID automatically.

The Proxmox API requires unique IDs within the cluster but doesn't support reserving IDs before resource creation. The provider uses file-based locking to prevent duplicates, but conflicts can still occur when multiple provider instances create resources simultaneously.

To reduce conflicts, set random_vm_ids = true in the provider block. This generates random IDs (checked for uniqueness via the API) instead of sequential ones.

**We want our container to be 123**