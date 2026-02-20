// Azure リソース定義 - Okozukai インフラ
// デプロイ: az deployment sub create -f main.bicep -l japaneast

targetScope = 'resourceGroup'

@description('環境名 (dev / stg / prd)')
param environmentName string = 'dev'

@description('リージョン')
param location string = resourceGroup().location

var prefix = 'okozukai-${environmentName}'

// ─── Storage Account（ユーザーSQLite保存） ─────────────────────
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: replace('${prefix}storage', '-', '')
  location: location
  sku: {
    name: 'Standard_LRS' // 最安構成
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    supportsHttpsTrafficOnly: true
  }
}

// ユーザーデータ用 Blob コンテナ
resource userDataContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  name: '${storageAccount.name}/default/okozukai-userdata'
  properties: {
    publicAccess: 'None'
  }
}

// ─── App Service Plan（消費プラン = FaaS相当） ─────────────────
resource hostingPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${prefix}-plan'
  location: location
  sku: {
    name: 'Y1'    // Consumption（従量課金）
    tier: 'Dynamic'
  }
  kind: 'functionapp'
  properties: {
    reserved: true // Linux
  }
}

// ─── Function App（API） ────────────────────────────────────────
resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: '${prefix}-api'
  location: location
  kind: 'functionapp,linux'
  properties: {
    serverFarmId: hostingPlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|9.0'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'Azure__StorageConnectionString'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        // 認証設定は Azure Portal または別途 Key Vault 参照で設定
        { name: 'Auth__Authority', value: '' }
        { name: 'Auth__Audience',  value: '' }
      ]
      cors: {
        allowedOrigins: [
          'https://${staticWebApp.properties.defaultHostname}'
        ]
        supportCredentials: true
      }
    }
    httpsOnly: true
  }
}

// ─── Static Web App（Blazor WASM ホスティング） ─────────────────
resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: '${prefix}-web'
  location: 'eastasia' // Static Web Appsが利用可能なリージョン
  sku: {
    name: 'Free' // 無料プラン
    tier: 'Free'
  }
  properties: {
    stagingEnvironmentPolicy: 'Enabled'
    allowConfigFileUpdates: true
  }
}

// ─── Outputs ────────────────────────────────────────────────────
output apiUrl string = 'https://${functionApp.properties.defaultHostName}'
output webUrl string = 'https://${staticWebApp.properties.defaultHostname}'
output storageAccountName string = storageAccount.name
