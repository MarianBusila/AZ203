# Implement Azure security (10-15%)

## Overview

* Microsoft Azure Active Directory (Azure AD) includes features, like Azure Multi-Factor Authentication (Azure MFA) and Azure AD self-service password reset (SSPR)
* 

## Implement authentication

* implement authentication by using certificates, forms-based authentication, or tokens
* implement multi-factor or Windows authentication by using Azure AD
* implement OAuth2 authentication [Microsoft identity platform](https://docs.microsoft.com/en-us/azure/active-directory/develop/active-directory-v2-protocols), [Application types for Microsoft identity platform](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-app-types)
    - In nearly all OAuth 2.0 and OpenID Connect flows, there are four parties involved in the exchange: Authorization Server(Microsoft identity platform),
    Resource Owner(party that owns the data), OAuth Client (the app), Resource Server (where data resides)
    - the app has to be registered in Azure portal and will have an Application ID
    - Once registered, the app communicates with Microsoft identity platform by sending requests to the endpoint:
    ```
    https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize
    https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token
    ```
    - A bearer token is a lightweight security token that grants the “bearer” access to a protected resource
    - **OpenID Connect** extends the OAuth 2.0 authorization protocol to use as an authentication protocol, so that you can do single sign-on using OAuth. OpenID Connect introduces the concept of an ID token, which is a security token that allows the client to verify the identity of the user.
    - 
* implement Managed Service Identity (MSI)/Service Principal authentication

## Implement access control

* implement CBAC (Claims-Based Access Control) authorization
* implement RBAC (Role-Based Access Control) authorization
* create shared access signatures

## Implement secure data solutions

* encrypt and decrypt data at rest and in transit
* create, read, update, and delete keys, secrets, and certificates by using the KeyVault API [Quickstart: Set and retrieve a secret from Azure Key Vault using Azure CLI](https://docs.microsoft.com/en-us/azure/key-vault/quick-create-cli)
    - Azure Key Vault is a cloud service that works as a secure secrets store. You can securely store keys, passwords, certificates.
    ```sh
    az group create --name myResourceGroup --location eastus
    az keyvault create --name myvault201909 --resource-group myResourceGroup --location eastus
    az keyvault secret set --vault-name myvault201909 --name ExamplePassword --value "12345"
    az keyvault secret show --vault-name myvault201909 --name ExamplePassword
    ```
    - The simplest way to authenticate an cloud-based .NET application is with a managed identity
    - Authenticating a desktop application with Azure requires the use of a service principal
        ```sh
        az ad sp create-for-rbac -n "http://mySP" --sdk-auth // returns a clientId and clientSecret
        ```
    - Create an access policy for your key vault that grants permission to your service principal by passing the clientId
        ```sh
        az keyvault set-policy -n <your-unique-keyvault-name> --spn <clientId-of-your-service-principal> --secret-permissions delete get list set --key-permissions create decrypt delete encrypt get list unwrapKey wrapKey
        ```
    ```cs
     KeyVaultClient kvClient = new KeyVaultClient(async (authority, resource, scope) =>
    {
        var adCredential = new ClientCredential(clientId, clientSecret);
        var authenticationContext = new AuthenticationContext(authority, null);
        return (await authenticationContext.AcquireTokenAsync(resource, adCredential)).AccessToken;
    });

    await kvClient.SetSecretAsync($"{kvURL}", secretName, secretValue);

    var keyvaultSecret = await kvClient.GetSecretAsync($"{kvURL}", secretName).ConfigureAwait(false);
    
    ```