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
  }

  tags = ["api"]
  unprivileged = true
  started = true
}
