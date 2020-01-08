param (
    [Parameter(mandatory=$true)]
    [string]$participantId = $null,
    
    [Parameter(mandatory=$true)]
    [string]$sqlAdmin = $null,

    [Parameter(mandatory=$true)]
    [SecureString]$sqlAdminPassword = $null
)


$Location = 'West Europe'

$ResourceGroupName = "az$($participantId)-monitor-rg"

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
    -TemplateFile .\monitorlab.template.json `
    -participantId $participantId `
    -sqlAdmin $sqlAdmin `
    -sqlAdminPassword $sqlAdminPassword `
    -localPublicIp $localPublicIp

