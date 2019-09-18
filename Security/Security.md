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
* create, read, update, and delete keys, secrets, and certificates by using the KeyVault API