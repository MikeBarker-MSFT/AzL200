# Azure Networking Lab

This lab should take around 15-20 minutes and contains all the steps you need to perform.
All of the Azure CLI commands are located in the AzureNetworking101.azcli file in this directory.
This can be opened in VS Code and the Azure CLI Tools extension will colourise it
All you need to do is change a few variables to be unique

# Steps

## Step 1 - Azure Cloud Shell
Log in to Azure Portal and open the Cloud Shell.
If this is the first time you are doing this it will prompt you to create a Cloud Drive to store your profile and files. Using the defaults should be fine.
Create a folder in the CloudDrive to store your files for this lab and keep them separate from other things.

## Step 2 - Variables
Declare some variables to be used later and run a quick test to check that you will have unique DNS names

## Step 3 - Subscription
Check you are connected to the right Azure subscription

## Step 4 - Resource Group
Create an Azure Resource Group to contain all of your resources

## Step 5 - Cloud-Init
Create a cloud-init file to automatically install Express and Nginx and configure them to serve a simple web page

## Step 6 - Main
Create:
+ Virtual Network
+ Network Security Group to allow port 80 to the VMs
+ VM Scale Set - this creates 3 identical VMs behind a Load Balancer using the cloud-init script
+ Load Balancing rule and health check for Http to the VMs

## Step 7 - Test Load Balancing
Run a simple repeating curl to test that connections are ebing load balanced across all instances

## Step 8 - Region 2
Update the location variables to your 2nd Region and run 'Main' again to deploy an identical environment in the 2nd Region.
This demonstrates the power of 'Infrastructure as Code'

## Step 9 - Test Region 2

## Step 10 - Azure Front Door
Deploy an Azure Front Door to load balance between the 2 Regions

## Step 11 - Test Front Door
Demonstrate it load balancing across all instances in both Regions

## Step 12 - Additional
A series of additional steps you can take, if you have time, to explore the solution you have deployed:

+ Try opening Network Watcher in the portal and viewing the Topolgy of your Resource Group to see the components you've deployed (doesn't show AFD yet)
+ Try scaling your VMSS out or in and see Azure adjust the load-balancing automatically
+ Try disabling one of the Backends in AFD or lower the acceptable 'latency sensitivity' to zero so it only sends traffic to the closest backend
+ Investigate Load Balancer metrics to see 'Data Path Availability' and 'Health Probe Status' to see how LB decides which backends to send traffic to. Optionally split Health Probe Status by Backend IP address
+ Add AFD metrics: Backend Health Percentage and Backend Request Latency and split by Backend 


