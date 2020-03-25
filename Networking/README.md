# Previous source for Nationwide https://github.com/MikeBarker-MSFT/AzL200
# Mike's updates https://github.com/MikeWedderburn-Clarke/AzL200/edit/master/Networking/README.md

# ----------- Cloud Drive --------------
# If you are using the Azure Cloud Shell and you haven't used it before, you will need to set up your Cloud Drive.
# Defaults should be fine
# Recommend you create a new folder in Cloud Drive to store your files in
cd ~/clouddrive
mkdir networking101
cd networking101


# ----------- Subscription -------------
#Check which Azure Subscription you are connected to
az account show 

#If it's not the right one then check which ones you have access to and use 'set' to choose the right subscription Id
az account list
az account set -s $SubscriptionId

# ------------ Variables -----------------------------
#Use export to declare variables so they can be re-used

#All Resources will be created in this Resource Group
export rg="1LbAndFrontDoor"

# We will create resources in 2 Azure Regions. By default WestEurope and NorthEurope but you can use others
export loc="WestEurope"
# Short code for location - will be appended to resource names
export locs="WEU"

#Create a Resource Group
az group create -n $rg -l $loc


# ---------- Cloud Init -----------------

# Creates cloud-init file which gets run by the Linux VM at 1st boot
# Cloud Init is very useful for configuring VMs automatically
# The VM will run Express to serve a simple web page on localhost:3000 and then use Nginx to proxy inbound connections on port 80 to that
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
        res.send('Hello World from host ' + os.hostname())
      })
      app.listen(3000, function () {
        console.log('Hello world app listening on port 3000!')
      })
runcmd:
  - service nginx restart
  - cd "/home/azureuser/myapp"
  - npm init
  - npm install express -y
  - npm install pm2 -g
  - pm2 start /home/azureuser/myapp/index.js
  - env PATH=$PATH:/usr/bin /usr/local/lib/node_modules/pm2/bin/pm2 startup systemd 

" > cloud-init.txt

# ------------- MAIN -----------------------------



# Create Public IP address
az network public-ip create -g $rg -l $loc -n Pip1-$locs --sku standard

# Create Standard Load Balancer with public IP (better than basic LB)
az network lb create \
    -g $rg \
    -l $loc \
    -n SLB1-$locs \
    --sku standard \
    --public-ip-address Pip1-$locs \
    --frontend-ip-name Fe1 \
    --backend-pool-name Be1

# Create health probe to check that VMs are serving the desired resource before sending them traffic
az network lb probe create \
    -g $rg \
    -n Probe1-$locs \
    --lb-name SLB1-$locs \
    --protocol http \
    --port 80 \
    --path / \
    --interval 5

# Create load balancing rule
az network lb rule create \
    -g $rg \
    -n HttpRule1-$locs \
    --lb-name SLB1-$locs \
    --protocol tcp \
    --frontend-port 80 \
    --backend-port 80 \
    --frontend-ip-name Fe1 \
    --backend-pool-name Be1 \
    --probe-name Probe1-$locs

Create a Virtual Network with Network Security Groups plus Network cards. The VM's will be created later
# Create a VNet to host the VM's being created in the next steps
az network vnet create \
    -g $rg \
    -l $loc \
    -n Vnet1-$locs \
    --subnet-name Subnet1

# Create a Network Security Group (NSG)
az network nsg create \
    -g $rg \
    -l $loc \
    -n NSGWeb1-$locs

#Create a NSG rule to allow inbound http traffic
az network nsg rule create \
    -g $rg \
    -n Http \
    --nsg-name NSGWeb1-$locs \
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
        -l $loc \
        -n NicVm$i-$locs \
        --vnet-name Vnet1-$locs \
        --subnet Subnet1 \
        --network-security-group NSGWeb1-$locs \
        --lb-name SLB1-$locs \
        --lb-address-pools Be1 \
        --no-wait
done

# Check if all 3 nics have been created (because we used --no-wait)
az network nic list -g $rg --output table

Create Availability Set and VM's
#Create Availability Set for the VM's plus simple Web Page
az vm availability-set create \
   -g $rg \
   -l $loc \
   -n AvSet1-$locs


# Creates the 3 VMs using the cloud-init above and --no-wait to make it faster.
for i in {1..3}; do
    az vm create \
    -g $rg \
    -l $loc \
    -n VM$i-$locs \
    --availability-set AvSet1-$locs \
    --nics NicVm$i-$locs \
    --size Standard_B1s \
    --image UbuntuLTS \
    --generate-ssh-keys \
    --custom-data cloud-init.txt \
    --no-wait
done

# Check if all 3 VMs have been created. Even after they have started it will take a few minutes for the web page to show.
az vm list -g $rg -d --output table

# ---------------- Test Load Balancing in Region 1 -----------------

#Get the Public IP Address for the Load Balancer
pip1=$(az network public-ip show -g $rg -n Pip1-$locs --query [ipAddress] --output tsv) ; echo "Pip1 = "$pip1

# Use this 'For' loop to test connecting to the public IP 20 times
# You should be load balanced between the VMs
for i in {1..20}; do curl $pip1 -m 1; echo -n $'\r\f'; sleep 0.1; done

# ----------------- Other Region ------------------------------------

# Now change the location variables to another Region and run MAIN again to create an identical setup in another region.
# The power of Infrastructure as Code !

export loc="NorthEurope"
export locs="NEU"

# ---------------- Test Load Balancing in Region 2 -----------------

#Get the Public IP Address for the Load Balancer
pip2=$(az network public-ip show -g $rg -n Pip1-$locs --query [ipAddress] --output tsv) ; echo "Pip2 = "$pip2

# Use this 'For' loop to test connecting to the public IP 20 times
# You should be load balanced between the VMs
for i in {1..20}; do curl $pip2 -m 1; echo -n $'\r\f'; sleep 0.1; done


# ----------------- Front Door ---------------------------------

The Azure Front Door extensions needed to be added to your Cloud Shell instance
az extension add --name front-door

# Set this to the name of your Front Door
export afd=mwcafd1

# TO DO - still have to build the AFD CLI part - I did it in portal for now
# Remember to set back-end timeout to > 100ms so that it round robins across Regions
az network front-door create -g $rg -n $afd --backend-address $pip1


# ----------------- Test Front Door -----------------------------


for i in {1..20}; do curl $afd.azurefd.net -m 1; echo -n $'\r\f'; sleep 0.1; done
