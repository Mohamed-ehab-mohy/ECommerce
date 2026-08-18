# ECommerce Terraform Infrastructure

## Prerequisites

- Terraform >= 1.5
- AWS CLI configured with valid credentials

## Setup

1. Initialize Terraform:
   ```bash
   terraform init
   ```

2. Create a `terraform.tfvars` file:
   ```hcl
   db_password          = "your-secure-db-password"
   redis_password       = "your-secure-redis-password"
   jwt_key_pem          = "-----BEGIN RSA PRIVATE KEY-----\n...\n-----END RSA PRIVATE KEY-----"
   stripe_webhook_secret = "whsec_..."
   ```

3. Preview changes:
   ```bash
   terraform plan
   ```

4. Apply infrastructure:
   ```bash
   terraform apply
   ```
