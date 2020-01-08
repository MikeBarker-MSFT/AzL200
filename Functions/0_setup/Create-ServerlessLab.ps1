param (
    [Parameter(mandatory=$true)]
    [string]$participantId = $null
)


$Location = 'West Europe'

$ResourceGroupName = "az$($participantId)-serverless-rg"

New-AzResourceGroup `
    -Name $ResourceGroupName `
    -Location $Location

New-AzResourceGroupDeployment `
    -ResourceGroupName $ResourceGroupName `
    -TemplateFile .\serverlesslab.template.json `
    -participantId $participantId

