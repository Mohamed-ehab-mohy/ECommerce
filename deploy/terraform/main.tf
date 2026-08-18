terraform {
  required_version = ">= 1.5"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
  backend "s3" {
    bucket = "ecommerce-terraform-state"
    key    = "prod/terraform.tfstate"
    region = "us-east-1"
  }
}

provider "aws" {
  region = var.aws_region
}

locals {
  name = "ecommerce-${var.environment}"
  tags = {
    Project     = "ECommerce"
    Environment = var.environment
    ManagedBy   = "Terraform"
  }
}
