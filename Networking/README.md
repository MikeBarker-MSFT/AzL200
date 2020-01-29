# Azure Basic Networking Lab
 
1. Single Linux VM with simple web site in a Vnet with NSG

	1a. Create a Linux VM with a public IP
  
	1b. Install Nginx and open port 80 (http). Edit homepage so you can identify the VM

2. Understanding Load Balancing

	2a. Install 3 servers in an Availability Set with a Public Load Balancer. Test load balancing

3. Multi-Region Deployment

	3a. Add 3 more servers in another Region. 
	
	3b. Add Azure Front Door to see global load-balancing. Test availability by turning some off

>**Note**: For these simple examples, curl is better to see the results than opening a web browser because the browser caches the response and doesn't switch hosts nearly as easily as curl
---

## Prerequisites
- Access to the Azure Portal (https://portal.azure.com)
- An Azure account with Contributor / Owner access to an Azure Subscription
- All can be run via a browser
- Azure Shell
---
## Initial Setup of Environment
Set your own variables here. These will be used for unique naming. We use 'export' so they are available in other sessions
- Login to Azure and open the Azure Shell to be able to run the Bash script commands in the below tasks
- `act` is the Azure Subscription used for the lab. Change the value to your own subscription ID
- `rg` is the resource group where the resources will be created. Use a unique name (in your subscription)
- `loc` is the region where the resources with be created
```bash
export act="4a4ed193-9a6a-4413-8ee1-12b17e185e73"
export rg="RG1"
export loc="WestEurope"

# Check that we are connected to the right Azure account
az account list
az account set -s $act
az account show 

```
---
## Part 1a - Create a Linux VM with a public IP
This creates a simple VM and showing how to connect to it via a Public IP Address
```bash
#Create a Resource Group
az group create -n $rg -l $loc

#Create the VM
az vm create \
  --resource-group $rg \
  --name vm1 \
  --image UbuntuLTS \
  --admin-username azureuser \
  --generate-ssh-keys 

# Copy public ip and paste in below
export ip1="65.52.140.235"

# Note that the default 'az vm create' command for Linux creates a VM with a public IP and a default 'allow ssh inbound from anywhere'!!!
# Can use 'az network nsg rule' to update them: https://docs.microsoft.com/en-us/cli/azure/network/nsg/rule?view=azure-cli-latest 
ssh azureuser@$ip1
```
> Tip:
> -  Goto the Azure Portal and have a look at the resources created. 
> -  Look at Azure `Network Watcher / Topology` to see what was created
---
## Part 1b - Show a web-page on the VM created above
- Install Nginx and open port 80 (http)
```bash
# This updates packages and then installs Nginx 
sudo apt-get update
sudo apt-get install -y nginx

# Exit out of the VM and see if it is serving a web page yet. This won't work because we haven't opened port 80. 
# We set '-m 5' so it will timeout after 5 seconds else you would be waiting a long time
exit
curl $ip1 -m 5

# Now open port 80
az vm open-port --port 80 -g $rg -n vm1 
# This takes a few mins to become effective
curl $ip1 -m 5
```
- Edit homepage so you can identify the VM
```bash
# Uniquely identify this VM so we can recognise it when connecting over http. Sed -i does an in-place string replace
ssh azureuser@$ip1
sudo sed -i.bak 's/Welcome to nginx/VM1/' /var/www/html/index.nginx-debian.html

#logout of the VM
exit

#Serving up the web page with the unique name updated above
curl $ip1 -m 5
```
> Tip: Open a web browser and try the IP Address above.
---
## Part 2 - Understanding Load Balancing 
Install 3 servers in an Availability Set with a Public Load Balancer. Test load balancing
Ref: https://docs.microsoft.com/en-us/azure/load-balancer/quickstart-load-balancer-standard-public-cli 
- Create a new resource group 
```bash
# Change the Resource Group Name and Region below
export rg="RG1-WE"
export loc="WestEurope"


```
- Create a Public Load Balancer. 
```bash
#Create a Resource Group
az group create -n $rg -l $loc

# Create Public IP address
az network public-ip create -g $rg -n Pip1 --sku standard

# Create Standard Load Balancer with public IP (better than basic LB)
az network lb create \
    -g $rg \
    -n SLB1 \
    --sku standard \
    --public-ip-address Pip1 \
    --frontend-ip-name Fe1 \
    --backend-pool-name Be1

# Create health probe to check that VMs are serving the desired resource before sending them traffic
az network lb probe create \
    -g $rg \
    -n Probe1 \
    --lb-name SLB1 \
    --protocol tcp \
    --port 80

# Create load balancing rule
az network lb rule create \
    -g $rg \
    -n HttpRule1 \
    --lb-name SLB1 \
    --protocol tcp \
    --frontend-port 80 \
    --backend-port 80 \
    --frontend-ip-name Fe1 \
    --backend-pool-name Be1 \
    --probe-name Probe1

```
- Create a VNet with Network Security Groups plus Network cards for the VM's that will be created later

```bash
# Create a VNet to host the VM's being created in the next steps
az network vnet create \
    -g $rg \
    -n Vnet1 \
    --subnet-name Subnet1
# Create a Network Security Group (NSG)
az network nsg create \
    -g $rg \
    -n NSGWeb1

#Create a NSG rule to allow inbound http traffic
az network nsg rule create \
    -g $rg \
    -n Http \
    --nsg-name NSGWeb1 \
    --protocol tcp \
    --direction inbound \
    --source-address-prefix '*' \
    --source-port-range '*' \
    --destination-address-prefix '*' \
    --destination-port-range 80 \
    --access allow \
    --priority 200

# Create 3x Network Cards for the VM's being created next. 
# They are linked to the Vnet, default Subnet, NSG and Load Balancer created above.
for i in {1..3}; do
    az network nic create \
        -g $rg \
        -n NicVm$i \
        --vnet-name Vnet1 \
        --subnet Subnet1 \
        --network-security-group NSGWeb1 \
        --lb-name SLB1 \
        --lb-address-pools Be1 \
        --no-wait
done

# Check if all 3 nics have been created (because we used --no-wait)
az network nic list -g $rg --output table
```

- Create Availability Set and VM's 
```bash

#Create Availability Set for the VM's plus simple Web Page
az vm availability-set create \
   -g $rg \
   -n AvSet1


# Creates cloud-init file which gets run by the Linux VM at 1st boot. 
# It runs Express to serve a simple web page on localhost:3000 and then uses Nginx to proxy inbound connections on port 80 to that
echo "#cloud-config
package_upgrade: true
packages:
  - nginx
  - nodejs
  - npm
write_files:
  - owner: www-data:www-data
  - path: /etc/nginx/sites-available/default
    content: |
      server {
        listen 80;
        location / {
          proxy_pass http://localhost:3000;
          proxy_http_version 1.1;
          proxy_set_header Upgrade \$http_upgrade;
          proxy_set_header Connection keep-alive;
          proxy_set_header Host \$host;
          proxy_cache_bypass \$http_upgrade;
        }
      }
  - owner: azureuser:azureuser
  - path: /home/azureuser/myapp/index.js
    content: |
      var express = require('express')
      var app = express()
      var os = require('os');
      app.get('/', function (req, res) {
        res.send('Hello World from host ' + os.hostname() + ' in WestEurope!')
      })
      app.listen(3000, function () {
        console.log('Hello world app listening on port 3000!')
      })
runcmd:
  - service nginx restart
  - cd "/home/azureuser/myapp"
  - npm init
  - npm install express -y
  - nodejs index.js
" > cloud-init.txt

# Creates the 3 VMs using the cloud-init above and --no-wait to make it faster.
for i in {1..3}; do
    az vm create \
    -g $rg \
    -n VM$i \
    --availability-set AvSet1 \
    --nics NicVm$i \
    --image UbuntuLTS \
    --generate-ssh-keys \
    --custom-data cloud-init.txt \
    --no-wait
done

# Check if all 3 VMs have been created. Even after they have started it will take a few minutes for the web page to show.
az vm list -g $rg -d --output table

#Get the Public IP Address for the Load Balancer
pipwe=$(az network public-ip show -g $rg -n Pip1 --query [ipAddress] --output tsv) ; echo $pipwe

#Test the Load Balancer. Run this a number of times to see different VM's being served up.
#While the web server is starting you may get a 'bad gateway' error but keep trying
curl $pipwe -m 5
```
> Tip:
> -  Go to the Azure Portal and have a look at the resources created. 
> -  Look a Azure `Network Watcher / Topology` to see what was created
---
## Part 3 - Multi-Region Deployment
Add 3 more servers in another Region.
This is a repeat of part 3, but targeting another Region. 
Ref: https://docs.microsoft.com/en-us/azure/load-balancer/quickstart-load-balancer-standard-public-cli 

- Change the Resource Group Name and Region below. Key to have a new Resource Name and different Region from Step 3. 
```bash
#Create a new Resource Group for
export rg="MwcNe"
export loc="NorthEurope"

```
- Create a Public Load Balancer.
```bash
#Create a Resource Group
az group create -n $rg -l $loc

# Create Public IP address
az network public-ip create -g $rg -n Pip1 --sku standard

# Create Standard Load Balancer with public IP (better than basic LB)
az network lb create \
    -g $rg \
    -n SLB1 \
    --sku standard \
    --public-ip-address Pip1 \
    --frontend-ip-name Fe1 \
    --backend-pool-name Be1

# Create health probe to check that VMs are serving the desired resource before sending them traffic
az network lb probe create \
    -g $rg \
    -n Probe1 \
    --lb-name SLB1 \
    --protocol tcp \
    --port 80

# Create load balancing rule
az network lb rule create \
    -g $rg \
    -n HttpRule1 \
    --lb-name SLB1 \
    --protocol tcp \
    --frontend-port 80 \
    --backend-port 80 \
    --frontend-ip-name Fe1 \
    --backend-pool-name Be1 \
    --probe-name Probe1

#Create a VNet to host the VM's being created in the next steps
az network vnet create \
    -g $rg \
    -n Vnet1 \
    --subnet-name Subnet1


#Create a Network Security Group (NSG)
az network nsg create \
    -g $rg \
    -n NSGWeb1

#Create a NSG rule to allow inbound http traffic
az network nsg rule create \
    -g $rg \
    -n Http \
    --nsg-name NSGWeb1 \
    --protocol tcp \
    --direction inbound \
    --source-address-prefix '*' \
    --source-port-range '*' \
    --destination-address-prefix '*' \
    --destination-port-range 80 \
    --access allow \
    --priority 200

# Create 3x Network Cards for the VM's being created next. 
# They are linked to the Vnet, default Subnet, NSG and Load Balancer created above.
for i in {1..3}; do
    az network nic create \
        -g $rg \
        -n NicVm$i \
        --vnet-name Vnet1 \
        --subnet Subnet1 \
        --network-security-group NSGWeb1 \
        --lb-name SLB1 \
        --lb-address-pools Be1 \
        --no-wait
done

# Check if all 3 nics have been created (because we used --no-wait)
az network nic list -g $rg --output table


az vm availability-set create \
   -g $rg \
   -n AvSet1


# Creates cloud-init file which gets run by the Linux VM at 1st boot. 
# It runs Express to serve a simple web page on localhost:3000 and then uses Nginx to proxy inbound connections on port 80 to that
echo "#cloud-config
package_upgrade: true
packages:
  - nginx
  - nodejs
  - npm
write_files:
  - owner: www-data:www-data
  - path: /etc/nginx/sites-available/default
    content: |
      server {
        listen 80;
        location / {
          proxy_pass http://localhost:3000;
          proxy_http_version 1.1;
          proxy_set_header Upgrade \$http_upgrade;
          proxy_set_header Connection keep-alive;
          proxy_set_header Host \$host;
          proxy_cache_bypass \$http_upgrade;
        }
      }
  - owner: azureuser:azureuser
  - path: /home/azureuser/myapp/index.js
    content: |
      var express = require('express')
      var app = express()
      var os = require('os');
      app.get('/', function (req, res) {
        res.send('Hello World from host ' + os.hostname() + ' in NorthEurope!')
      })
      app.listen(3000, function () {
        console.log('Hello world app listening on port 3000!')
      })
runcmd:
  - service nginx restart
  - cd "/home/azureuser/myapp"
  - npm init
  - npm install express -y
  - nodejs index.js
" > cloud-init.txt

# Creates the 3 VMs using the cloud-init above and --no-wait to make it faster.
for i in {1..3}; do
    az vm create \
    -g $rg \
    -n VM$i \
    --availability-set AvSet1 \
    --nics NicVm$i \
    --image UbuntuLTS \
    --generate-ssh-keys \
    --custom-data cloud-init.txt \
    --no-wait
done

# Check if all 3 VMs have been created. Even after they have started it will take a few minutes for the web page to show.
az vm list -g $rg -d --output table

#Get the Public IP Address for the Load Balancer
pipne=$(az network public-ip show -g $rg -n Pip1 --query [ipAddress] --output tsv) ; echo $pipne

#Test the Load Balancer. Run this a number of times to set different VM's being served up.
curl $pipne -m 5
```
> Tip:
> -  Goto the Azure Portal and have a look at the resources created. 
> -  Look a Azure `Network Watcher / Topology` to see what was created
---
## Step 3b - Azure Front Door
Azure Front Door use used to support the creation of a scalable and secure end point for fast delivery of your global applications.
This section will link both Regions created above to Front Door and show Global Load Balancing across these regions. Including failover when a Region going down.
ref: https://azure.microsoft.com/en-us/services/frontdoor/
This part of the Lab will be using the Azure Portal. (https://portal.azure.com)

### Create Azure Front Door
* Click on `Create a Resource` and search for `Front Door` and click `Create`
* Basics Section
  - Select Subscription and the last Resource Group created above for placing Azure Frond Door
  > **Note**: This is a `Global` service, so the `Resource group location` is greyed out. Just stores meta data for Azure Front Door

* Configuration Section
  + **Frontend Hosts**
    This creates the URL (Entry-Point) for your Application
    - Click `+` and give it an unique `Host Name`
    - Keep `Session Affinity` and `Web Application Firewall` options `Disabled`

  + **Backend pools**
    This linked back to the load balancers created previously to allow Azure Front Door to route traffic too.
    - Click `+` and give a name for the backend pool
    - Click `Add a backend`
      - `Backend host type` select `Custom host`
      - `Backend host name` enter the Load Balancer's Public IP Address for Region 1
      - Click `Add`
      > *Repeat for the the second Region created in Part 4 above*
    - Change `Protocol` to `HTTP` 
    > Note: Normally HTTPS would be used, but as no certificate has been created in this Lab HTTP needs to be used.
    - `Latency sensitivity (in milliseconds)` to 500 (To make sure when testing both regions show). This is the minimum latency for it to include a back-end in the round robin.
    - Click `Update` to save the backend pool

  + **Routing Rules**
    - Click `+`
    - Enter a `Name` for the rule
    - Make sure the `Frontend hosts` shows the one setup above
    - Leave `Route type` to `Forward`
    - Make sure the `Backend pool` shows the one setup above
    - Change `Forwarding Protocol` to `HTTP` (As no certificate setup in Lab)
    - Leave `URL rewrite` and `Caching` disabled
    - Click `Add`
    - Click `Review + create`
    - Click `Create` to create the Azure Frond Door resource linked to the Load Balancers created previously.

### Test Azure Front Door 
Follow the steps below to test the service. If The above setup can take a few minutes to complete. If you get `*Out services aren't available right now*` wait a few minutes for it to become available.
- To test copy the `Frontend host` URL (e.g. https://<name>.azurefd.net)
- Go to a web browser and paste it in. Before pressing enter, please change it to HTTP (e.g.  http://<name>.azurefd.net)
- See that the web page opens. Try reloasing a few times to see it jump between the 2 regions.
- You can also do this through curl, from your own machine or cloud shell.

### Test by removing Backend pools from one of the the Load Balancers
The goal here is to show Azure Front Door routing traffic seamlessly to another Region when a Backend poll goes down.

To do this you'll be removing the backend pools from one of the Load Balancers and checking that the service is still up and running.

- Go to one of the Load Balancers create above (From within Region)
- Click `Load balancing rules` and delete the rule for the backend pool.
- Click `Backend pools` and the `...` next to the pool you have.
- Click `Delete`. Waiting for the change to complete and then test the Azure Front Door URL again and see that it's now going to the other Region only.
- You can also just shutdown one or a few of the VMs to simulate a failure.
---

Thank You for going through this Lab. 

---

Here a few links for your information:
- Azure Front Door: https://azure.microsoft.com/en-us/services/frontdoor/
- Azure Load Balancer: https://docs.microsoft.com/en-us/azure/load-balancer/load-balancer-overview
- Azure Virtual Network (VNET): https://docs.microsoft.com/en-us/azure/virtual-network/virtual-networks-overview
- Azure Subnets: https://docs.microsoft.com/en-us/azure/virtual-network/virtual-network-manage-subnet
- Azure Virtual Machines: https://azure.microsoft.com/en-us/services/virtual-machines/
- Azure Availability Sets: https://docs.microsoft.com/en-us/azure/virtual-machines/windows/manage-availability
- Azure Network Cards: https://docs.microsoft.com/en-us/azure/virtual-network/virtual-network-network-interface-vm

---

    


 
    





