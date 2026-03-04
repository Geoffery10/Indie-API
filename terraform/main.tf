terraform {
  required_providers {
    proxmox = {
      source  = "bpg/proxmox"
      version = "0.71.0"
    }
  }
}

provider "proxmox" {
  endpoint = var.proxmox_api_url
  api_token = var.proxmox_api_token
  insecure = true
}

resource "proxmox_virtual_environment_container" "indie_api" {
  vm_id = 123
  node_name = var.proxmox_node

  initialization {
    hostname = "indie-api"
    
    ip_config {
      ipv4 {
        address = "192.168.0.123/24"
        gateway = "192.168.0.1"
      }
    }

    user_account {
      password = var.container_password
    }
  }

  network_interface {
    name    = "eth0"
    bridge  = "vmbr0"
    firewall = true
  }

  operating_system {
    template_file_id = var.proxmox_template_id
    type             = "debian"
  }

  disk {
    datastore_id = "local-lvm"
    size         = 8
  }

  cpu {
    cores = 2
  }

  memory {
    dedicated = 2048
    swap      = 2048
  }

  features {
    nesting = true
    keyctl  = true
    fuse    = true
  }

  connection {
    type     = "ssh"
    user     = "root"
    password = var.container_password
    host     = "192.168.0.123"
  }

  provisioner "remote-exec" {
    inline = [
      "apt-get update",
      "apt-get install -y ca-certificates curl gnupg",
      "install -m 0755 -d /etc/apt/keyrings",
      "curl -fsSL https://download.docker.com/linux/debian/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg",
      "chmod a+r /etc/apt/keyrings/docker.gpg",
      "echo \"deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/debian $(. /etc/os-release && echo \"$VERSION_CODENAME\") stable\" | tee /etc/apt/sources.list.d/docker.list > /dev/null",
      "apt-get update",
      "apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin",
      "mkdir -p /app"
    ]
  }

  provisioner "file" {
    content     = var.APP_ENV_FILE
    destination = "/app/.env"
  }

  provisioner "remote-exec" {
    inline = [
      "echo \"${var.ghcr_pat}\" | docker login ghcr.io -u \"${var.ghcr_username}\" --password-stdin",
      "docker pull ghcr.io/${var.ghcr_username}/indie-api:latest",
      "docker run -d --name indie-api --restart unless-stopped --env-file /app/.env -p 5000:5000 ghcr.io/${var.ghcr_username}/indie-api:latest"
    ]
  }

  tags = ["api"]
  unprivileged = true
  started = true
}
