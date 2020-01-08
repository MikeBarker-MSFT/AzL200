param (
    [Parameter(mandatory=$true)]
    [string]$participantId = $null
)


$Location = 'West Europe'

$ResourceGroupName = "az$($participantId)-security-rg"

$sqlAdmin = 'AzureSecurityLabAdministrator'
$guid = [Guid]::NewGuid()
$sqlAdminPassword = ConvertTo-SecureString -String "$($guid)" -AsPlainText -Force

try
{
    $localPublicIp = Invoke-RestMethod 'http://ipinfo.io/json' | Select -exp ip
    
    Write-Host "Local public IP identified as $($localPublicIp)"
}
catch
{
    $localPublicIp = 0.0.0.0
}

New-AzResourceGroup `
    -Name $ResourceGroupName `
    -Location $Location

New-AzResourceGroupDeployment `
    -ResourceGroupName $ResourceGroupName `
    -TemplateFile .\securitylab.template.json `
    -participantId $participantId `
    -sqlAdmin $sqlAdmin `
    -sqlAdminPassword $sqlAdminPassword `
    -localPublicIp $localPublicIp

