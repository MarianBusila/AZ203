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

* manage batch jobs by using Batch Service API
* run a batch job by using Azure CLI, Azure portal, and other tools
* write code to run an Azure Batch Services batch job

## Create containerized solutions

* create an Azure Managed Kubernetes Service (AKS) cluster
* create container images for solutions
* publish an image to the Azure Container Registry
* run containers by using Azure Container Instance or AKS