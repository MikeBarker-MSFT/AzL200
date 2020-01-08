param (
    [Parameter(mandatory=$true)]
    [string]$participantId = $null
)


$Location = 'West Europe'

$ResourceGroupName = "az$($participantId)-apim-rg"

$sqlAdmin = 'AzureApimLabAdministrator'
$guid = [Guid]::NewGuid()
$sqlAdminPassword = ConvertTo-SecureString -String "$($guid)" -AsPlainText -Force

New-AzResourceGroup `
    -Name $ResourceGroupName `
    -Location $Location

New-AzResourceGroupDeployment `
    -ResourceGroupName $ResourceGroupName `
    -TemplateFile .\apimlab.template.json `
    -participantId $participantId `
    -sqlAdmin $sqlAdmin `
    -sqlAdminPassword $sqlAdminPassword

