variable "proxmox_api_url" {
  description = "The URL of the Proxmox API"
  type        = string
}

variable "proxmox_api_token" {
  description = "The API token for Proxmox"
  type        = string
  sensitive   = true
}

variable "proxmox_node" {
  description = "The Proxmox node to deploy to"
  type        = string
  default     = "proxmox"
}

variable "proxmox_template_id" {
  description = "The ID of the Debian template (e.g., local:vztmpl/debian-12-standard_12.2-1_amd64.tar.zst)"
  type        = string
}

variable "container_password" {
  description = "The password for the container's root user"
  type        = string
  sensitive   = true
  default     = "temporary_password_change_me"
}

variable "container_ssh_public_keys" {
  description = "A list of SSH public keys to add to the container's root user"
  type        = list(string)
  default     = []
}

variable "ghcr_username" {
  description = "The GitHub username for GHCR"
  type        = string
}

variable "ghcr_pat" {
  description = "The Personal Access Token for GHCR"
  type        = string
  sensitive   = true
}

variable "APP_ENV_FILE" {
  description = "The content of the .env file for the application"
  type        = string
  sensitive   = true
}
