
# API Management Lab

## Clone or download the lab content
+ From clone or download the git repo

# Getting setup

## Step 1 - Create an API Management instance
An instance of API Management takes a long time to complete (approx 30 min). Complete this step, ahead of the lab session.

+ In the Azure portal, click on "**+ Create a resource**" and search for "API Management".
+ Supply the following details for the API Management:
  + *Name*: `az{your_id}-apim-apimanagement`
  + *Resource Group*: **Create New**
    + *Name*: `az{your_id}-apim-rg`
  + *Location*: `West Europe`
  + *Organization Name*: `az{your_id}`
  + Administrator: <default>
  + Pricing Tier: Developer (No ALS)
+ Leave all other defaults and click **Create** to create the API Management instance.

## Step 2 - Deploy a function app and SQL database
+ Open a powershell window
+ Execute the **Create-ApiMLab** PowerShell script found in the **0_setup** folder, with your lab ID: `.\Create-ApiMLab.ps1 -participantId {your_id}`
  + This will create a new resource group, `az{your_id}-apim-rg`
  + The resource group will contain a blank function app, and a database with the **AdventureWorksLT** sample.

## Step 3 - Publish the function app 
Once the deployment in Step 2 has finished:
+ Open the `Lab.APIManager.sln` file in Visual Studio. The solution contains a single C# project for a .NET Core function app.
+ Build the project to ensure it builds corrects.
+ Publish the function app to the your function app `az{your_id}-apim-funcapp`

Take a moment to examin the functions we have published. There are five REST-ful functions for interacting with customers in the AdventureWorksLT database.
The functions are:
1. HTTP GET /customer to get a list of all customers.
1. HTTP GET /customer/{id} to get a single customer, by id.
1. HTTP PUT /customer to insert a customer.
1. HTTP POST /customer/{id} to update a customer, by id.
1. HTTP DELETE /customer/{id} to delete a customer, by id.

# Lab 1 - Create an API within API Management
Now that you have API Management and an Azure Function created, we will use the API Management to create an API that links to the backend function.

**Note:** The API will comprise five operations, one for each of the functions in the function app. However, an API is just a logical grouping of operations. There is no requirement that these operations belong to the same backend service. We could (if we desired) create a sixth operation backed by (say) a SOAP API hosted on a VM.

+ In the blade of your API Management instance:
+ Select **API Management** -> **APIs**
+ Select “Function App”
+ Click “browse” to find and link your function app to APIM
  + Ensure all of the function are selected (ticked), and click **Select**
+ Change the **Display name** to `Customer`, (this will also change the **Name** to `customer`)
+ Remove the API URL suffix 
+ Click **Create**

You will now see the newly create API in the APIM with the five operations created.
Having created the API and linked this to the Azure function backend, we can now test the APIs.
+ Change to the **Test** tab at the top of the operations pane
+ Select the GET operation `GetCustomerById`
+ Enter a value of **5** for the id parameter, an click **Send**.

Scroll down in the test pane to view the HTTP Response. This shows an HTTP 200 (OK) was obtain and in the body of the response we can see the JSON encoded details of customer 5, Mr. Lucy Harrington.

Try testing other operations. Notice that the PUT and POST operations require a well-formed JSON object in their body. Use the response from `GetCustomerById` to get the correct structure for a customer.

# Lab 2 - Create a Product for the API
When it comes to managing API you can group them into Products, which developers can subscribe to. This allows multiple APIs to be collated together that has business meaning, regardless of the backends. For example, we might wish to create an AdventureWorks product which comprises of the APIs for working with customers, orders and products. These APIs might reach out to a variety of different backend APIs, which could be cloud, on-premises, SaaS – anywhere.

Furthermore, you may choose to have different product offerings depending on your consumers needs. You may choose to define *Basic*, *Premium*, and *Internal* products, all offering the same APIs but where the *Basic* product only allows upto 5 calls a second, *Premium* allows 200 calls a second, and *Internal* is unlimited and only used by your internal teams.

In this step you’ll be creating a Product for the customer API:
+ In the API Management blade:
+ Click on **API Management** -> **Products**
+ Add a new product by clicing **+Add** from the top menu
+ Give it a name (e.g. `Workshop`) and a description
+ Make sure **Requires subscription** is ticked
+ Click **Select API** and select the Customer api created above
+ Click **Select**
+ Click **Create**

The **Workshop** product is now created. In order for a developer to call your APIs, specifying the product they are subscribed to, the developer must pass the "Subscription key" in the HTTP header when making a call. You can view the default subscription keys by selecting the product from the table, and choosing the **Subscriptions** menu item.


# Lab 3 - Exploring the Developer Portal
Developers who wish to use your APIs can discover what APIs and Products are available by visiting the Developer Portal of your API Management instance. This web UI interface has all the documentation and examples required to utilise an API, and the ability to request an access code.

Take some time to explore the information available in the Developer Portal. Notice the code samples 

+ From the APIM **Overview** blade (left-hand menu), click on **Developer portal** (top menu)
+ In the Developer Portal:
+ Click **APIS**. You will see the Customer API you created. Clicking on the Customer API will bring up a page with details about your API and code examples to run it.

Take some time to explore the information available in the Developer Portal. Notice the code samples in various languages provided to call you APIs. Notice that, in the top right, you can download the API definition in Open API YAML or JSON formats.  As you provide additional information against your APIs and operations (such as descriptions and documentation) this information is reflected here to aid developers in discovering and utilising your APIs.

+ Now click the **GetCustomerById** operation, and then click the blue **Try it** button.
+ Provide the id value `5`
+ Notice an **Ocp-APim-Subscription-Key** which is populated already. This header passes the subscription key for the Product you created previously.
+ Click **Send**

Try running other operations too.

### Going further
The Developer Portal can be locked-down to users, requiring Azure AD, or certificates to gain access. Explore this under the **Security** menu of API Management.

It is possible to customise the appearance of the Developer Portal, try exploring the options in fly-in menu (two crossed pencils) on the Devloper Portal. **Note:** This option is only available to you when accessing the Developer Portal when signed-in as a user who has rights to edit this portal.


# Lab 4 - Apply a policy to the API
Policies define how the API Manager will behave when a call to an operation is received.
Some examples are:
+ You may wish to authenticate the call looking for a bearer token before passing the call to the backend services.
+ Response and value caching can take load off the backend by returning the same data for subsequent calls with the same input parameters.
+ One may wish to rate-limit the number of calls.
+ Transform a REST to a SOAP api.
+ A single operation may result in multiple calls to backend services.
Policies can also be applied at the Global, Product, User, API or operation scope.

In this example we’ll going to override the ID parameter that is passed to the function for processing.
In the Azure portal, on APIM blade:
+ Click on **API Management** -> **APIs**
+ Select the Customer API and then the GET **GetCustomerById** operation.
+ Click in the Inbound processing panel the **+ Add Policy**
  (it is at the bottom of the panel, and you may need to scroll down to see it)
+ Click on the **Rewrite URL** policy
  + Provide the Backend URL as: `/customer/5`
  + Click **Save**

We can now test the policy:
+ Go back to the Developer Portal and run the **GetCustomerById** operation for your API.
+ Enter an ID value (say 12) and run the API.
+ Notice that the response was for customer id= 5, Mr Lucy Harrington.

Having demonstrated this policy, return to the Azure portal and remove it.
+ In the Inbound processing click the elipsis (...) next to the *Rewrite URL* policy and click **Delete**.
+ Click **Save**

# Lab 5 - API Versions

If you try to search for customer 8 an HTTP 500 response will be sent as an exception occurs in our function app, since the customer does not exist. We should rather return an HTTP 404 Not Found error code.

However, we may already have customers using our API and this would be a breaking change (they may be handling the 500 error in their calling applications). We need to be able to deploy a new version of our API which will not impact existing customers but allow us to change our API. In time we can help customers move off version 1 and on to version 2, without breaking their applications.

**Note**: Other real-world breaking changes which would require new versions would be:
+ Removing an operation
+ A new, required input parameter
+ Removing an output field

We are going to achieve this by creating a new version of our API and using policies to set the response code to HTTP 404 Not Found inplace of the 500 we were returning.

In the Azure portal, in the API Management blade:
+ Click on **API Management** -> **APIs**
+ Click the elipsis (...) next to the Customer API, and select **Add version**
+ Supply the following details for the new version:
  + *Name*: `CustomerWith404`
  + *Versioning scheme*: `Path`
  + *Version identiifer*: `v2`
  + **Products**: `Workshop`

Notice this has created a separate version under the **Customer** API, *Original* and *v2*.
+ Select the **v2** version of the Customer API
+ Select the **GetCustomerById** operation
+ In the **Output processing** pane, click the *Policy code editor*, vis: </>
+ Paste the following snippet in the `outbound` section:
  ```xml
  <outbound>
    <base />
    <choose>
      <when condition="@(context.Response.StatusCode >= 500)">
        <return-response>
          <set-status code="404" reason="Not Found" />
        </return-response>
      </when>
    </choose>
  </outbound>  
  ```
  **Note:** In this (advanced) policy code snippet we check for a response code of 500 (or greater), and if detected return a 404 Not Found instead.
+ Click **Save**

We can now test the new version and its policy:
+ Go back to the Developer Portal and select the **APIS** menu option
+ Notice there are two version listed for the Customer API. Select v2
+ Select the **GetCustomerById** operation for your API
+ Click **Try it**
+ Enter an ID value (say 12) and run the API.
  Notice that a HTTP 200 OK response with the customer details is recevied.
+ Now try enter an ID value of 8 (missing customer).
  Notice that a HTTP 404 Not Found response is recevied.


## Going further
Notice that we used still referenced the same backend function app. We could also deploy a new version of our backend function (into a new function app instance so as not to overwrite the old one), and direct the new APIM version to this instance (instead of using policy).
Note: You will need to create a new function app and deploy into this new backend, so as to leave version 1 in-place. Then create a new version of the customer API to point to this backend.

Try to complete this.


# Complete
In this lab you have explored some of the features of Azure API Management.

You may now delete the `az{your_id}-apim-rg` resource group.