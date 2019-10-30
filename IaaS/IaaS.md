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

    ```sh
    az vm create \
    --resource-group myResourceGroup \
    --name myVM \
    --image win2016datacenter \
    --admin-username azureuser \
    --admin-password myPassword

    az vm open-port --port 80 --resource-group myResourceGroup --name myVM
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
    - Example ARM template
    ```json
    {
    "$schema": "https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#",
    "contentVersion": "1.0.0.0",
    "parameters": {
        "storagePrefix": {
            "type": "string",
            "minLength": 3,
            "maxLength": 11
        },
        "storageSKU": {
            "type": "string",
            "defaultValue": "Standard_LRS",
            "allowedValues": [
                "Standard_LRS",
                "Standard_GRS",
                "Standard_RAGRS",
                "Standard_ZRS",
                "Premium_LRS",
                "Premium_ZRS",
                "Standard_GZRS",
                "Standard_RAGZRS"
            ]
        },
        "location": {
            "type": "string",
            "defaultValue": "[resourceGroup().location]"
        }
    },
    "variables": {
        "uniqueStorageName": "[concat(parameters('storagePrefix'), uniqueString(resourceGroup().id))]"
    },
    "resources": [
        {
            "type": "Microsoft.Storage/storageAccounts",
            "apiVersion": "2019-04-01",
            "name": "[variables('uniqueStorageName')]",
            "location": "[parameters('location')]",
            "sku": {
                "name": "[parameters('storageSKU')]"
            },
            "kind": "StorageV2",
            "properties": {
                "supportsHttpsTrafficOnly": true
            }
        }
    ],
    "outputs": {
        "storageEndpoint": {
            "type": "object",
            "value": "[reference(variables('uniqueStorageName')).primaryEndpoints]"
        }
    }
    }
    ```
    - Deploy template
    ```
    New-AzResourceGroupDeployment `
    -Name addoutputs `
    -ResourceGroupName myResourceGroup `
    -TemplateFile $templateFile `
    -storagePrefix "store" `
    -storageSKU Standard_LRS

    az group deployment create \
    --name addoutputs \
    --resource-group myResourceGroup \
    --template-file $templateFile \
    --parameters storagePrefix=store storageSKU=Standard_LRS
    ```

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

    az batch account create --name marianbatchaccount --storage-account marianstorageaccount --resource-group marianresourcegroup --location eastus2

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
    ```cs
    BatchSharedKeyCredentials sharedKeyCredentials = new BatchSharedKeyCredentials(BatchAccountUrl, BatchAccountName, BatchAccountKey);

    using (BatchClient batchClient = BatchClient.Open(sharedKeyCredentials))
    {
        // create pool
         ImageReference imageReference = new ImageReference(
                    publisher: "MicrosoftWindowsServer",
                    offer: "WindowsServer",
                    sku: "2012-R2-Datacenter-smalldisk",
                    version: "latest");

        VirtualMachineConfiguration virtualMachineConfiguration =
            new VirtualMachineConfiguration(
                imageReference: imageReference,
                nodeAgentSkuId: "batch.node.windows amd64");

        CloudPool pool = batchClient.PoolOperations.CreatePool(
                    poolId: PoolId,
                    virtualMachineSize: PoolVMSize,
                    virtualMachineConfiguration: virtualMachineConfiguration,
                    targetDedicatedComputeNodes: DedicatedNodeCount,
                    targetLowPriorityComputeNodes: LowPriorityNodeCount);
         pool.ApplicationPackageReferences = new List<ApplicationPackageReference>
                {
                    new ApplicationPackageReference
                    {
                        ApplicationId = appPackageId,
                        Version = appPackageVersion
                    }
                };
         await pool.CommitAsync();

        // create job
        CloudJob job = batchClient.JobOperations.CreateJob();
        job.Id = jobId;
        job.PoolInformation = new PoolInformation { PoolId = poolId };

        await job.CommitAsync();

        // add task
         CloudTask task = new CloudTask(taskId, taskCommandLine);
        task.ResourceFiles = new List<ResourceFile> { inputFiles[i] };
        List<OutputFile> outputFileList = new List<OutputFile>();
        OutputFileBlobContainerDestination outputContainer = new OutputFileBlobContainerDestination(outputContainerSasUrl);
        OutputFile outputFile = new OutputFile(outputMediaFile,
            new OutputFileDestination(outputContainer),
            new OutputFileUploadOptions(OutputFileUploadCondition.TaskSuccess));
        outputFileList.Add(outputFile);
        task.OutputFiles = outputFileList;
        await batchClient.JobOperations.AddTaskAsync(jobId, tasks);

        // monitor task
         ODATADetailLevel detail = new ODATADetailLevel(selectClause: "id");
        List<CloudTask> addedTasks = await batchClient.JobOperations.ListTasks(jobId, detail).ToListAsync();

        TaskStateMonitor taskStateMonitor = batchClient.Utilities.CreateTaskStateMonitor();
        try
        {
            await taskStateMonitor.WhenAll(addedTasks, TaskState.Completed, timeout);
        }
        catch (TimeoutException)
        {
            await batchClient.JobOperations.TerminateJobAsync(jobId);
            Console.WriteLine(incompleteMessage);
            return false;
        }
        await batchClient.JobOperations.TerminateJobAsync(jobId);

         detail.SelectClause = "executionInfo";
        // Filter for tasks with 'Failure' result.
        detail.FilterClause = "executionInfo/result eq 'Failure'";

        List<CloudTask> failedTasks = await batchClient.JobOperations.ListTasks(jobId, detail).ToListAsync();

    }

    ```

* write code to run an Azure Batch Services batch job [Github samples](https://github.com/Azure-Samples/azure-batch-samples)

## Create containerized solutions

* create an Azure Managed Kubernetes Service (AKS) cluster ([Azure CLI](https://docs.microsoft.com/en-us/azure/aks/kubernetes-walkthrough), [Azure Portal](https://docs.microsoft.com/en-us/azure/aks/kubernetes-walkthrough-portal))
    - [Kubernetes core concepts](https://docs.microsoft.com/en-us/azure/aks/concepts-clusters-workloads): cluster master, nodes and nodes pools, pods
    - [Docker core concepts](https://docs.docker.com/engine/docker-overview/): images, containers, volumes, networks, services (for scaling containers)
    - Create AKS cluster
    ```sh
    az group create --name myResourceGroup --location eastus
    az aks create --resource-group myResourceGroup --name myAKSCluster --node-count 1 --enable-addons monitoring --generate-ssh-keys
    ```
    - Connect to the cluster
    ```sh
    az aks install-cli
    az aks get-credentials --resource-group myResourceGroup --name myAKSCluster
    kubectl get nodes
    ```
    - Run the application
    ```sh
    kubectl apply -f azure-vote-all-in-one-redis.yaml
    kubectl get service azure-vote-front --watch
    ```
* create container images for solutions [ACI Tutorial](https://docs.microsoft.com/en-us/azure/container-instances/container-instances-tutorial-prepare-app)
    - Azure Container Instances enables deployment of Docker containers onto Azure infrastructure without provisioning any virtual machines or adopting a higher-level service.
    - Dockerfile
    ```docker
    FROM node:8.9.3-alpine
    RUN mkdir -p /usr/src/app
    COPY ./app/ /usr/src/app/
    WORKDIR /usr/src/app
    RUN npm install
    CMD node /usr/src/app/index.js
    ```
    - Build image & run container
    ```sh 
    docker build . -t aci-tutorial-app 
    docker images
    docker run -d -p 8080:80 aci-tutorial-app
    docker ps
    ```
    - Example dot netmvc app
    ```sh
    // create and run asp.net core app
    mkdir webapp
    cd webapp
    dotnet new mvc
    dotnet build
    dotnet run

    // docker file example
    FROM microsoft/dotnet:sdk AS build-env
    WORKDIR /app

    #copy csproj and restore
    COPY webapp/*.csproj ./
    RUN dotnet restore

    # copy everthing else and build
    COPY ./webapp ./
    RUN dotnet publish -c Release -o out

    # build runtime image
    FROM microsoft/dotnet:aspnetcore-runtime
    WORKDIR ./app
    COPY --from=build-env /app/out .
    ENTRYPOINT ["dotnet", "webapp.dll"]

    // build container and run
    docker build -t webapp .
    docker run -d -p 8080:80 --name myapp webapp
    ```    
    
* publish an image to the Azure Container Registry [ACI Tutorial](https://docs.microsoft.com/en-us/azure/container-instances/container-instances-tutorial-prepare-acr)
    - Azure Container Registry is your private Docker registry in Azure
    - Create Azure Container Registry
    ```sh
    az group create --name myResourceGroup --location eastus
    az acr create --resource-group myResourceGroup --name myAzureContainerRegistry2019 --sku Basic --admin-enabled true
    az acr login --name myAzureContainerRegistry2019
    ```
    - Tag image with full name of the registry's login server
    ```sh
    az acr show --name myAzureContainerRegistry2019 --query loginServer --output table
    docker tag aci-tutorial-app myazurecontainerregistry2019.azurecr.io/aci-tutorial-app:v1
    ```
    - Push image to Azure Container Registry
    ```sh
    docker push myazurecontainerregistry2019.azurecr.io/aci-tutorial-app:v1
    ```
     - List images in Azure Container Registry
    ```sh
    az acr repository list --name myazurecontainerregistry2019 --output table
    az acr repository show-tags --name myazurecontainerregistry2019 --repository aci-tutorial-app --output table
    ```

* run containers by using Azure Container Instance or AKS [ACI Tutorial]()
    - Create and configure an Azure AD service principal with pull permissions to your registry
    ```sh
    #!/bin/bash

    # Modify for your environment.
    # ACR_NAME: The name of your Azure Container Registry
    # SERVICE_PRINCIPAL_NAME: Must be unique within your AD tenant
    ACR_NAME=myazurecontainerregistry2019
    SERVICE_PRINCIPAL_NAME=acr-service-principal

    # Obtain the full registry ID for subsequent command args
    ACR_REGISTRY_ID=$(az acr show --name $ACR_NAME --query id --output tsv)

    # Create the service principal with rights scoped to the registry.
    # Default permissions are for docker pull access. Modify the '--role'
    # argument value as desired:
    # acrpull:     pull only
    # acrpush:     push and pull
    # owner:       push, pull, and assign roles
    SP_PASSWD=$(az ad sp create-for-rbac --name http://$SERVICE_PRINCIPAL_NAME --scopes $ACR_REGISTRY_ID --role acrpull --query password --output tsv)
    SP_APP_ID=$(az ad sp show --id http://$SERVICE_PRINCIPAL_NAME --query appId --output tsv)

    # Output the service principal's credentials; use these in your services and
    # applications to authenticate to the container registry.
    echo "Service principal ID: $SP_APP_ID"
    echo "Service principal password: $SP_PASSWD"
    ```
    - Deploy container
    ```sh
    az container create --resource-group myResourceGroup --name aci-tutorial-app --image myazurecontainerregistry2019.azurecr.io/aci-tutorial-app:v1 --cpu 1 --memory 1 --registry-login-server myazurecontainerregistry2019.azurecr.io --registry-username <service-principal-ID> --registry-password <service-principal-password> --dns-name-label aci-tutorial-app2019 --ports 80
    ```
    - Verify deployment progress and application dns
    ```sh
    az container show --resource-group myResourceGroup --name aci-tutorial-app --query instanceView.state
    az container show --resource-group myResourceGroup --name aci-tutorial-app --query ipAddress.fqdn
    ```
    - Visit http://aci-tutorial-app2019.eastus.azurecontainer.io/
    - View logs
    ```sh
    az container logs --resource-group myResourceGroup --name aci-tutorial-app
    ```