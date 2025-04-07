# Jamper Financial

## Overview



## Getting Started

## Financial

## Finacial Web

## Financial Shared



## Prerequisites

## Troubleshooint guide


### Running without Docker
1. Clean and Build solution
2. Select Jamper-Financial.Web project as startup project
3. Run the project

### Running Docker Locally
docker-compose up -d --build

### Running Docker in Azure
Login to docker gchr.io: docker login ghcr.io 
 - user your username and personal access token
Build your docker image: 
 - docker-compose --build
Repush: Repush the updated image to GHCR: 
 - docker push ghcr.io/jamper-financial/jamper-financial-web:latest

In Azure, go to Home > Web Apps > Jamper-Financial-Web > Container settings
>> Start the application