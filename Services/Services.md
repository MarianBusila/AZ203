# Connect to and consume Azure and third-party services (20-25%)

## Develop an App Service Logic App

* create a Logic App [Quickstart: Create your first automated workflow with Azure Logic Apps - Azure portal](https://docs.microsoft.com/en-us/azure/logic-apps/quickstart-create-first-logic-app-workflow)
    - Azure Logic Apps is a cloud service that helps you schedule, automate, and orchestrate tasks, business processes, and workflows when you need to integrate apps, data, systems, and services across enterprises or organizations.
    - Every logic app workflow starts with a trigger. Each time that the trigger fires, the Logic Apps engine creates a logic app instance that runs the actions in the workflow. These actions can also include data conversions and flow controls, such as conditional statements, switch statements, loops, and branching. 

* create a custom connector for Logic Apps [Custom Connectors](https://docs.microsoft.com/en-ca/connectors/custom-connectors/)
    - A custom connector is a wrapper around a REST API. Once you have an API with authenticated access, you can use OpenAPI or Postman collection to describe the API
    - Each connector offers a set of operations classified as 'Actions' and 'Triggers'
    - [Create a custom connector from an OpenAPI definition](https://docs.microsoft.com/en-ca/connectors/custom-connectors/define-openapi-definition)
    - [Use a webhook as a trigger for Azure Logic Apps ](https://docs.microsoft.com/en-ca/connectors/custom-connectors/create-webhook-trigger#create-webhook-triggers-from-the-ui)

* create a custom template for Logic Apps [Overview: Automate deployment for Azure Logic Apps by using Azure Resource Manager templates] (https://docs.microsoft.com/en-ca/azure/logic-apps/logic-apps-azure-resource-manager-templates-overview)
    - a Resource Manager template structure
    ```json
    {
    "$schema": "https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#",
    "contentVersion": "1.0.0.0",
    // Template parameters
    "parameters": {
        "<template-parameter-name>": {
            "type": "<parameter-type>",
            "defaultValue": "<parameter-default-value>",
            "metadata": {
                "description": "<parameter-description>"
            }
        }
    },
    "variables": {},
    "functions": [],
    "resources": [
        {
            // Start logic app resource definition
            "properties": {
                <other-logic-app-resource-properties>,
                "definition": {
                "$schema": "https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#",
                "actions": {<action-definitions>},
                // Workflow definition parameters
                "parameters": {
                    "<workflow-definition-parameter-name>": {
                        "type": "<parameter-type>",
                        "defaultValue": "<parameter-default-value>",
                        "metadata": {
                            "description": "<parameter-description>"
                        }
                    }
                },
                "triggers": {
                    "<trigger-name>": {
                        "type": "<trigger-type>",
                        "inputs": {
                            // Workflow definition parameter reference
                            "<attribute-name>": "@parameters('<workflow-definition-parameter-name')"
                        }
                    }
                },
                <...>
                },
                // Workflow definition parameter value
                "parameters": {
                "<workflow-definition-parameter-name>": "[parameters('<template-parameter-name>')]"
                },
                "accessControl": {}
            },
            <other-logic-app-resource-definition-attributes>
        }
        // End logic app resource definition
    ],
    "outputs": {}
    }
    ```
    - 
## Integrate Azure Search within solutions

* create an Azure Search index

* import searchable data

* query the Azure Search index

## Establish API Gateways

* create an APIM instance

* configure authentication for APIs

* define policies for APIs

## Develop event-based solutions

* implement solutions that use Azure Event Grid

* implement solutions that use Azure Notification Hubs

* implement solutions that use Azure Event Hub

## Develop message-based solutions

* implement solutions that use Azure Service Bus

* implement solutions that use Azure Queue Storage queues