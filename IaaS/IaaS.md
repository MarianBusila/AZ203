# Develop Azure Infrastructure as a Service compute solutions (10-15%)

## Implement solutions that use virtual machines (VM)

* provision VMs ([Windows](https://docs.microsoft.com/en-ca/azure/virtual-machines/windows/tutorial-manage-vm), [Linux](https://docs.microsoft.com/en-ca/azure/virtual-machines/linux/tutorial-manage-vm))
    ```ps
    New-AzResourceGroup `
    -ResourceGroupName "myResourceGroupVM" `
    -Location "EastUS"

    New-AzVm `
        -ResourceGroupName "myResourceGroupVM" `
        -Name "myVM1" `
        -Location "EastUS" `
        -VirtualNetworkName "myVnet" `
        -SubnetName "mySubnet" `
        -SecurityGroupName "myNetworkSecurityGroup" `
        -PublicIpAddressName "myPublicIpAddress1" `
        -ImageName "MicrosoftWindowsServer:WindowsServer:2016-Datacenter:latest" `
        -Size "Standard_B1ms" `
        -Credential (Get-Credential)
    ```
* create ARM templates ([Portal](https://docs.microsoft.com/en-us/azure/azure-resource-manager/resource-manager-quickstart-create-templates-use-the-portal), [VisualStudio](https://docs.microsoft.com/en-us/azure/azure-resource-manager/vs-azure-tools-resource-groups-deployment-projects-create-deploy)))
    - create a new project from Azure Resource Group template selecting WebApp
    - deploy resources for web app
        ```ps
        .\Deploy-AzTemplate.ps1 -ArtifactStagingDirectory . -Location centralus -TemplateFile WebSite.json -TemplateParametersFile WebSite.parameters.json
        ```
    - add WebApplication project to the solution
    - deploy the web app to azure
        ```ps
        .\Deploy-AzTemplate.ps1 -ArtifactStagingDirectory .\bin\Debug\staging\ExampleAppDeploy -Location centralus -TemplateFile WebSite.json -TemplateParametersFile WebSite.parameters.json -UploadArtifacts -StorageAccountName <storage-account-name>
        ```
    - besides the resources provided by Visual studio, other azure resources can be added to Website.json file (like a Dashboard, etc)

* configure Azure Disk Encryption for VMs([Windows](https://docs.microsoft.com/en-ca/azure/virtual-machines/windows/encrypt-disks), [Linux](https://docs.microsoft.com/en-ca/azure/virtual-machines/linux/encrypt-disks))
    - in order to encrypt a VM disk, the VM and the KeyVault must be in the same region
    - for encryption we are using Keys from KeyVault
    - encryption can be done only on Standard or above VMs
        ```ps
        $keyVaultName = "myKeyVault$(Get-Random)"
        New-AzKeyVault -Location "EastUS" `
            -ResourceGroupName "myResourceGroupVM" `
            -VaultName $keyVaultName `
            -EnabledForDiskEncryption

        Add-AzKeyVaultKey -VaultName $keyVaultName `
            -Name "myKey" `
            -Destination "Software" 

        $keyVault = Get-AzKeyVault -VaultName $keyVaultName -ResourceGroupName "myResourceGroupVM";
        $diskEncryptionKeyVaultUrl = $keyVault.VaultUri;
        $keyVaultResourceId = $keyVault.ResourceId;
        $keyEncryptionKeyUrl = (Get-AzKeyVaultKey -VaultName $keyVaultName -Name myKey).Key.kid;

        Set-AzVMDiskEncryptionExtension -ResourceGroupName "myResourceGroupVM" `
            -VMName "myVM1" `
            -DiskEncryptionKeyVaultUrl $diskEncryptionKeyVaultUrl `
            -DiskEncryptionKeyVaultId $keyVaultResourceId `
            -KeyEncryptionKeyUrl $keyEncryptionKeyUrl `
            -KeyEncryptionKeyVaultId $keyVaultResourceId    
        ```    

## Implement batch jobs by using Azure Batch Services

* manage batch jobs by using [Batch Service API Reference](https://docs.microsoft.com/en-us/rest/api/batchservice/)
* run a batch job by using [Azure CLI](https://docs.microsoft.com/en-ca/azure/batch/quick-create-cli), [Azure portal](https://docs.microsoft.com/en-ca/azure/batch/quick-create-portal), and other tools ([NET](https://docs.microsoft.com/en-ca/azure/batch/quick-run-dotnet))
    - CLI
    ```ps
    az group create --name marianResourceGroup --location eastus2
    
    az storage account create --resource-group marianResourceGroup --name marianstorageaccount --location eastus2 --sku Standard_LRS

    az batch account create --name marianbatchaccount --storage-account marianstorageaccount --resource-group myresourcegroup --location eastus2

    az batch account login --name marianbatchaccount --resource-group marianResourceGroup --shared-key-auth

    az batch pool create --id mypool --vm-size Standard_A1_v2 --target-dedicated-nodes 2 --image canonical:ubuntuserver:16.04-LTS --node-agent-sku-id "batch.node.ubuntu 16.04"

    az batch job create --id myjob --pool-id mypool

    for i in {1..4}
    do
    az batch task create --task-id mytask$i --job-id myjob --command-line "/bin/bash -c 'printenv | grep AZ_BATCH; sleep 90s'"
    done

    az batch task show --job-id myjob --task-id mytask1

    az batch task file list --job-id myjob --task-id mytask1 --output table

    az batch task file download --job-id myjob --task-id mytask1 --file-path stdout.txt --destination ./stdout-task1.txt
    ```
    - NET - [Parallel file processing](https://docs.microsoft.com/en-ca/azure/batch/tutorial-parallel-dotnet)
* write code to run an Azure Batch Services batch job

## Create containerized solutions

* create an Azure Managed Kubernetes Service (AKS) cluster
* create container images for solutions
* publish an image to the Azure Container Registry
* run containers by using Azure Container Instance or AKS