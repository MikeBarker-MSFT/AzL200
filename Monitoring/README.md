# Azure Monitor Lab

## Clone or download the lab content
+ From clone or download the git repo

## Deploy a web app and SQL database
+ Open a PowerShell window
+ Execute the **Create-MonitorLab** PowerShell script found in the **0_setup** folder, with your lab ID: `.\Create-MonitorLab.ps1 -participantId {your_id}`
  + Specify a SQL user name and strong password (at least 6-characters, with uppercase, lowercase, and digits)
  + Remember, or make a note of your chosen user name and password as you will need them later.
  + This script will create a new resource group, `az{your_id}-monitor-rg`
  + The resource group will contain
    + a database with the **AdventureWorksLT** sample;
    + an empty function app, pre-configured to point to the database; and
    + an empty web app, pre-configured to point to the function app.

The script will take approximately 1.5 - 2 min to complete.

### Publish the function app to Azure
+ Open the `Lab.Monitor.sln` file in Visual Studio. The solution contains two C# projects for (a) .NET Core function application, and (b) a .NET Core MVC web application.
+ Right-click the `FunctionApp` project, **Publish...**
+ In the pop-up, select **Select Existing** and **Create Profile**.
  + Find your web application `az{your_id}-monitor-funcapp`, and select it.
  + Click **Ok**, and then **Publish** to begin publishing the app.

### Publish the web app to Azure
+ In Visual Studio:
+ Right-click the `WebApplication` project, **Publish...**
+ In the pop-up, select **Select Existing** and **Create Profile**.
  + Find your web application `az{your_id}-monitor-webapp`, and select it.
  + Click **Ok**, and then **Publish** to begin publishing the app.
+ When the application has been published to Azure it will open in your browser.
+ Try navigating to the **All Customers** page and selecting a customer from the list.

When clicking on the customer views in the Web app, a HTTP call is made to the Function App which implements a REST API. The function app will retrieve data from the database and return this to the web app to be displayed. This represents a standard multi-tier application.

Spend a few minutes navigating around the web application to get a feel for the application, and to generate some metric data.

# Lab 1 - Resources Metrics
Azure provides a number of metrics for your resources which can be queries and graphed within the portal.  Notice 

### Exploring Metrics
+ In the portal, in your web app's blade:
+ Navigate (using the left hand menu) to **Monitoring** > **Metrics**
+ This brings up a blade showing a graph. Above the graph is a "pill" with **Resource**, **Metric Namespace**, **Metric**, and **Aggregation**.
+ The Resource and Metric Namespace are pre-populated based on the resource we navigated from (i.e. the web app).
+ Within the **Metric** drop down select **Average Response Time** (Aggregation will default to `Avg`).
+ The graph now updates to show the average response time of the web app. This is currently showing the data over a 24 hour period, hence our recent activity shows as spike on the far right.
  + Change the x-axis time scale to a 30 min period.
  + In the top-right of the blade is another "pill" showing the time scale, currently defaulted to 24 hours.
  + Click the "pill" and change to display the Last 30 minutes.
  + Click Apply
  + The graph will update to show only the data for the last 30 minutes.

We can add multiple metrics to a graph. This allows us to get a view to determine if there is a correlation between metrics.
+ At the top of the graph click the **+ Add metric** button. (note: use "Add metric", not "New chart")
+ This presents a new "metric pill" (notice also the previous "metric pill" shrinks).
+ Within the **Metric** drop down select **CPU Time**, and set the **Aggregation** to **Sum**).
+ The graph now displays the both the average response time of our web app, and also the amount of CPU time consumed.
+ In this instance there appears to be some (but overall, little) correlation between CPU time and response time.

Lets try adding a new metric for the thread count of our web app:
+ At the top of the graph click the **+ Add metric** button.
+ Within the new pill, in the **Metric** drop down select **Thread Count**, and set the **Aggregation** to **Avg**).

_Urgh! What just happened to our chart?_
Unfortunately the units and scale of the response time and CPU time graphs (measuring milliseconds, i.e. 1e-3) is very different to the Thread Count (measuring units). Azure metrics doesn't handle the different scales, and tries to plot everything on one graph. Therefore the CPU and response times shrink down to appear as a flat line, relative to the Thread Count. To fix this we must separate the metrics onto different charts, and plot each with a different scale.

First remove the Thread count metric:
+ If not already done, shrink the Thread Count "metric pill" by clicking away from it.
+ In the far right of the shrunken pill is a X. Click this to remove the metric from the chart.

Now create a new chart for the Thread Count metric:
+ At the top of the blade click **+ New chart**. This brings places a new chart in our plot area.
+ On the new chart select the **Thread Count** metric.

This makes it easier to visualise and correlate peaks between the graphs.

#### Understanding metric aggregation
In the Thread Count metric we currently have the aggregation set to **Avg**. What does this mean? Azure Monitor receives metrics as a list of entries which comprise of a metric name, time, and value (other fields are included but these are relevant for this discussion).

We might get a list of underlying metric data similar to:
| Metric       | Time                    | Value   |
| ------------ | ----------------------- | ------: |
| Thread Count | 2000-01-01 18:54:02.789 | 60      |
| Thread Count | 2000-01-01 18:54:34.264 | 61      |
| Thread Count | 2000-01-01 18:54:59.647 | 60      |
| Thread Count | 2000-01-01 18:55:16.819 | 58      |
| Thread Count | 2000-01-01 18:55:36.291 | 61      |
| Thread Count | 2000-01-01 18:56:03.233 | 64      |
| Thread Count | 2000-01-01 18:56:28.452 | 60      |
| Thread Count | 2000-01-01 18:56:54.212 | 62      |

When we select the time scale, we also select the granularity (e.g. defaults to 1 minute when viewing a 30 min scale). This defines a windowing used to group metric entries together, in order to be displayed meaningfully in the charts. Because we now have multiple values which need to displayed as a single plot point, we need an aggregation function to combine these together.

Assuming a 1 minute granularity, if we select average (i.e. Avg) this will produce plot points for:
| Metric       | Time             | Avg     |
| ------------ | ---------------- | ------: |
| Thread Count | 2000-01-01 18:54 | 60.333  |
| Thread Count | 2000-01-01 18:55 | 59.500  |
| Thread Count | 2000-01-01 18:56 | 62.000  |

But what about the other aggregations? Here is the complete set:
| Metric       | Time             | Count   | Avg     | Min     | Max     | Sum     |
| ------------ | ---------------- | ------: | ------: | ------: | ------: | ------: |
| Thread Count | 2000-01-01 18:54 | 3       | 60.333  | 60      | 61      | 181     |
| Thread Count | 2000-01-01 18:55 | 2       | 60.333  | 58      | 61      | 119     |
| Thread Count | 2000-01-01 18:56 | 3       | 60.333  | 60      | 64      | 186     |

As can be seen from the aggregation values above, some make sense and are useful in this case (i.e. Avg, Min, Max) but others do not provide meaningful information (i.e. Count, Sum).It is important to consider this when selecting the aggregation function you will use to display data. Generally the metric will default to utilise a meaningful aggregation.

+ **Count** is generally useful when you need a plot of distinct occurrences of an event.
+ **Sum** is useful when value accumulation is important (e.g. as for CPU Time, above).
+ In most other cases **Avg**, **Min**, or **Max** will be utilised.

# Lab 2 - Alerts
Alerts in Azure Monitor can be used to send pro-active notifications before an application fails (e.g. due to running out of disk space), or when anomalies arise in the system.

### Setting up an Alert
Let's setup a pro-active rule to send us an email when the CPU usage of our web app becomes critically high. We will then be able to scale the host before users experience poor performance. This CPU utilisation metric is obtained from the under host of the web app, i.e. the app service plan.
+ In the portal, navigate to the app service plan for you app, `az{your_id}-monitor-asp`:
+ Using the left hand menu, go to **Monitoring** > **Alerts**
+ Click the **+ New Alert rule** button
  + In the new view your app service plan resource will already have been selected.
  + Under the condition, click **Add**:
    + Select the signal **CPU Percentage** 
    + *Operator*: `Greater than`
    + *Aggregation*: `Average`
    + *Threshold value*: 75
    + (notice the condition preview text now reads: *Whenever the cpu percentage is greater than 75 percent*)
    + We only want to get notified when the CPU is high for more than 5 minutes, so we leave the default *Aggregation granularity (Period)* at 5 minutes
    + Click **Done**
  + That has defined the condition when we must be notified, now we must define what to do when the condition is breached.
  + Under **Actions** select **Create action group**
    + *Action group name*: `Proactive az{your_id}-monitor system montioring team`
    + *Short name*: `webapp ops`
    + *Resource group*: `az{your_id}-monitor-rg`
    + Add an action:
      + *Action Name*: `Email Ops team`
      + *Action Type*: `Email/SMS/Push/Voice`
      + In the popup, select **Email**, and provide your email address. Then click OK
    + Click OK
  + Complete the Action Details section:
    + *Alert rule name*: `AppServicePlan CPU Percentage greater than 75`
  + Click **Create alert rule**

This will create the rule. Should the CPU of the host app service plan exceed 75% for a prolonged period of time we will get an email and can proactively scale out (or up) the app service plan to ensure our application stays responsive.

You may have noticed that actions do not necessarily have to be email notifications. Azure Monitor can also execute an automation runbook, logic app, function, call a
web hook, etc. This means that we can setup not just proactive monitoring, but also make our environment automatically respond and take the actions needed.

Note: For the specific case of scaling a web app (or rather the host app service plan) one would typically setup rules in the "Scale out" area of the app service plan,
and not the Alert section.

# Lab 3 - Application Insights

## Lab 3.a. - Enable Application Insights within the Portal
In this lab we will enable Application Insights to acquire data from the web and function app, and explore the interface and data insights this makes available.

## Web Apps

### Observe the web app configuration before enabling Application Insights
+ In the portal, navigate to your web application `az{your_id}-monitor-webapp`.
+ In the left hand menu of the web app blade select **Settings** > **Configuration**.
+ Take note of the configuration settings currently setup. There are only two settings (`CustomerUrlBaseAddress`, and `CustomerFunctionKey` which provide the function app URL and access key respectively).

### Enable Application Insights
+ In the portal, on your web application's blade:
+ From the left hand menu select **Settings** > **Application Insights**.
+ Click **Turn on Application Insights**.
+ From the options that appear:
  + *Collect application monitoring data using Application Insights*: `Enable`
  + *Change your resource* > *Create new resource*: Selected
  + *New resource name*: `az{your_id}-monitor-appinsights`  (defaults to the name of your web app)
  + Under *Instrument your application*:
    + Select the .Net Core tab
    + *Collection level*: `Recommended`
    + *Profiler*: `Off`
    + *Snapshot debugger*: `Off`
    + *SQL Commands*: `Off`
+ Click **Apply**. This will instruct you that your application will be restarted, select **Yes** to continue.

This will create a new Application Insights Azure resource within your resource group, and wire your web app up to send data to it.

### Observe the created Application Insights instance, and the configuration used to connect to it
+ In the portal, go to the resource group `az{your_id}-monitor-rg`.
+ Notice the Application Insights resource `az{your_id}-monitor-appinsights`. This was the name we provided in the steps above.
+ Select the Application Insights resource to navigate to it's blade.
+ In the top right of the Overview blade you will find the `Instrumentation Key` for your Application Insights instance. This is a GUID. Make a note of this instrumentation key, for validation in the next step.

+ Take a look at the web app's configuration again to see whats been added to configure it to send data to Application Insights.
+ Return to the web app blade's **Settings** > **Configuration**.
+ Notice that nine new application configuration settings have been added as follows:
  + *APPINSIGHTS_INSTRUMENTATIONKEY*: `{app_insights_instrumentation_key}`
    This setting contains the instrumentation key of your Application Insights instance, and will match the key we made a note of previously.
  + *APPINSIGHTS_PROFILERFEATURE_VERSION*: `disabled`
  + *APPINSIGHTS_SNAPSHOTFEATURE_VERSION*: `disabled`
  + *ApplicationInsightsAgent_EXTENSION_VERSION*: `~2`
  + *DiagnosticServices_EXTENSION_VERSION*: `disabled`
  + *InstrumentationEngine_EXTENSION_VERSION*: `disabled`
  + *SnapshotDebugger_EXTENSION_VERSION*: `disabled`
  + *XDT_MicrosoftApplicationInsights_BaseExtensions*: `disabled`
  + *XDT_MicrosoftApplicationInsights_Mode*: `recommended`
+ Try returning to the **Settings** > **Application Insights**, and re-enable optional settings under the .NET Core tab (such as `Profiler`, and `Snapshot debugger`).
  After doing this return to the configuration settings blade to observe the effects each setting has. For an in-depth study enable the settings one by one.

You can read more about the various configuration settings in the [Azure Monitor documentation](https://docs.microsoft.com/en-us/azure/azure-monitor/app/azure-web-apps#automate-monitoring "Monitor Azure App Service performance - Automate monitoring").

For this lab it does not matter which options are enabled, as long as the *Collection level* is set to *Recommended*.

## Function Apps
For this lab we are going to send all telemetry from the Function App to the same Application Insights instance as the web app.

### Enable Application Insights
+ In the portal, on your function app's blade:
+ At the top of the function app blade will be a warning message saying that Application Insights is not configured. Click on this message.
+ In the next page select **Select existing resource**, and click to select your `az{your_id}-monitor-appinsights` instance (the same on used for the web app).
+ Click **OK**


Now that Application Insights is setup for your application against we shall explore how Application Insights can be used to gain insights into your application.

### Single or multiple Application Insights instances?
In this lab we have configured both the web app and function app to send their telemetry to the same Application Insights instance. It is possible to send each resource's telemetry to a separate instance. Having all the data in one place per application can make it easier to diagnose the holistic system, whilst having separate instances can assist in reducing the noise to resolve issues.

Application Insights is able to automatically join telemetry data across instances (reducing the pain of having data across instance)... and filtering within the views allows us to quickly look at only the data we are interested in (removing the pain of having data in one instance).

A good blog on this (written by a Microsoft Engineer) is here: [Application Insights: One or many AI resources per app](https://www.danielstocker.net/application-insights-one-or-many-ai-resources-per-app/)

As a general rule we'd recommend having a single instance per "administered unit". i.e. have a single instance collecting data from all resources in the  application \ micro-service, and different instance per application \ micro-service.


## Lab 3.b - Exploring Application Insights
Before continuing, we will populate Application Insights with some data. The Application Insights client batches data before sending it to the server. Because of this there is generally a lag (of up to 5 minutes) before navigation data is available in application insights.
+ Return to your web application and navigate around the screens; retrieving and updating a few customers (or deleting if you wish).

When you are ready, in the Portal, navigate to your Application Insights (`az{your_id}-monitor-appinsights`) blade.

#### Overview
Select the ***Overview** blade.
+ You will notice that several graphs are available which show: Failed requests, Server response time, Server requests, and Availability.
+ The views can also be filtered to show data over the last 30 min, up to 30 days.
+ This view can allow you to quickly gain insights into the overall health of your application.
+ Placing you mouse pointer over the top of a graph will draw a line at the same time point for the other three graphs, to allow visual correlation between events.
+ Clicking on the *pin* of a graph will pin it to your current Azure Dashboard.


#### Application Map
The Application Map is created dynamically by Application Insights, by correlating the telemetry data from various sources to determine what components are connected.
+ Select **Investigate** > **Application Map**
+ From here you will be able to see that it has identified that the web app calls the function, which calls the SQL database.
+ The average number of milliseconds for each call is shown, as is the total number of calls.

Try selecting various components and calls to understand what data is available to you.

#### Smart Detections
Smart detections will identify anomalies in the behaviour of your application. There is not enough data to derive Smart Detection yet as it uses machine learning to understand what "normal" is. Therefore, this won't be demo'ed.

#### Live Metric Stream
As mentioned previously, Application Insights batches telemetry data to the server (to minimise the impact on performance). Sometimes, however, you need to see real-time performance metrics. When you select Live Metrics Application Insights will instruct its client instances to return some metric data as a live stream. It is not recommended that this be used constantly in production system, as there could be a negative affect on performance.
+ Select **Investigate** > **Live Metric Stream**
+ Return to you web application, and arrange the portal and web app side-by-side on your screen (so that you can see both views).
+ Navigate around your web application, and notice that, as you do so, the graphs in the Live Metrics Stream view update.

Take a moment to look at the graphs and understand what data is available to you.

This view is especially useful to monitor performance and failures directly after a release.

#### Search
Search can be used to zoom in on particular telemetry.
+ Select **Investigate** > **Search**
+ Reduce the time scale (top left) to the lowest available, 30 min, and click **Apply**.
+ From the graph one can identify the number of traces, views, requests, failures, etc.
+ The search feature can be used to identify all events related to a particular event. As an example try searching for `GetCustomerById`.

#### Availability
Probes can be setup to periodically test an end-point, to ensure your services are available.
+ Select **Investigate** > **Availability**
+ Add a new probe by selecting the **+ Add test** at the top left of the blade.
  + Give your test a name (e.g. "Web App Ping Test")
  + In the URL provide the base address of your website (https://az{your_id}-monitor-webapp.azurewebsites.net/)
  + Notice that under **Locations** you can specify the origin of the request to the URL provided. This is especially useful if you have a global system.
  + For now leave all other defaults.
  + Click **Create**
+ Once the test has been created Application Insights will continue to "ping" that URL and report on the availability of your web app.
+ You can also view the response time of the tests by selecting **Scatter Plot**.

You can also setup an Alert should the test fail a number of times in succession. This is done by selecting the "more item" ellipsis for your test, then **Edit Alerts**.

#### Failures
The Failures view is useful for identifying exception and errors thrown by your application.
+ In the web app create an error by trying to retrieve a customer which does not exist.
  + Go to `https://az{your_id}-monitor-webapp.azurewebsites.net/Customer/Edit/8`
  + This returns an error, because there is no customer ID=8.
+ Return to the Portal and navigate to the **Investigate** > **Failures** blade.
+ NOTE: You may need to wait a short while for the failure to appear, due to batching.
+ We can see the failure in the graph, and it also appears in the list of failed operations underneath, i.e. `GET Customer/Edit [id]`.
+ Select the failed operation. In the bottom right, click the **Drill into... 1 Operations** button.
+ This brings up a list of all failed operations of the type selected.
+ Select any failure from the list, or pick the **Suggested** item at the top.
+ This brings up a call graph where you can see the dependencies from the original call, through the function app to the database.
  + The database operation is in blue, indicating it complete successfully.
  + The function app is in red indicating it resulted in an exception, as is the web app.
  + Clicking on the red exception, function app, or web app rows shows more information on the error.
    + The first failure shows that this was the result of an `ArgumentOutOfRangeException`.
    + The call stack indicates that the error occurs in `CustomerApiFunctions.CustomersAPI+<GetCustomerById>d__1.MoveNext (FunctionApp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null)`
      (This is the only element in the call stack which is from our code.) We now have somewhere definitive to start diagnosing our error.

As you work through the call chain, exceptions and associated call stacks you can tease out that:
+ The call originates from the web app with a GET to the url `Customer/Edit [id]`.
+ The web app calls the function app, with a GET to the url `/api/customer/8`.
+ The function app call the database, passing the command `SELECT * FROM SalesLT.Customer WHERE CustomerID = 8`
+ The database call returns without exception.
+ The function app then fails with an `ArgumentOutOfRangeException`, during an itteration in `CustomersAPI`.
+ The function app returns an HTTP 500 response to the web app.
+ The web app fails with a `HttpRequestException` during a call to `HttpResponseMessage.EnsureSuccessStatusCode`

Therefore it is evident that the function app is the first place to look, and that the response from `SELECT * FROM SalesLT.Customer WHERE CustomerID = 8` was not as expected. Using this information we can return to our function app's code and begin to diagnose and fix the bug.

**BONUS:** In Visual Studio, goto the function app's `CustomersAPI.GetCustomerById` method. Can you spot the problem?
In line 52, we expect at least one element in a list, vis:
```cs
  return new JsonResult(customers[0]);
```
Since the database returns no elements this line fails.

**Going further**: There is a bug in our application which becomes apprent when trying to delete customer 42, Jeremey Los. Using only Application Insights pin point where you should look in the code base to fix this bug.

#### Performance
The Performance view shows much the same information as the failures view. You can trace through any operation and diagnose where slow operations are occurring.
+ Navigate to the **Investigate** > **Performance** blade.
+ In the operations list you can see the long running operations. (Unsurprisingly, it is the "All Customers" view which is slowest).
+ Just as for failures, you can drill down into the operations to see the dependency call chain, and the amount of time in each.

##### Roles
It is also possible to filter down to just web app, or just the function app telemetry. This aids resolving performance issues in a multi-component distributed system.
+ In the **Investigate** > **Performance** blade:
+ Select the **Roles** tab (above the graph)
+ Unselect the web app, leaving only the function app selected in the list
+ Returning to the **Operations** tabs we now see only the function app telemetry.
  + Notice however, that drilling down to the operations dependency call graph still show the function app operation in the context of the originating web app request.
+ Return to the **Roles** tab and reselect the web app

##### Further filtering
To home-in on specific cases additional filters can be added to the Performance view, to reduce the working set of data.
+ In the **Investigate** > **Performance** blade:
+ Click the add filter button above the graph (existing filters will be **Local Time**, and **Role**).
+ Observe the properties which are available to filter on.
+ Try filtering down to a subset of URLs.

##### Browser vs Server
We can also see telemetry related to browser performance.
+ In the top right of the **Performance** blade toggle between Server and Browser.


#### Going further - Usage Data in Application Insights
Because we are working with a very small website, with just one local user there is limited insight that can be demonstrated in the **Usage** section of Application Insights.

Explore the **Usage** area, including *Users*, *Sessions*, *Events*, *Funnels*, *User Flows* etc.
If you have access to a live application with a large user base, consider taking time to explore these features with live data.


## Lab 3.c - Instrumenting our code with Application Insights
Everything we have explored upto now has required no changes to our code base to allow us to utilise Application Insights. However, if we want to get enhanced custom telemetry we will need to add Application Insights instrumentation to our code.

### Create a dev instance of Application Insights
We don't want to log development data into the production instance of Application Insights. Therefore we will create a new instance for development.

In the portal:
+ Click **+ Create a resource** at the top of the far left menu.
+ Search for Application Insights
+ Click **Create**
  + *Resource Group*: `az{your_id}-monitor-rg`
  + *Name*: `az{your_id}-monitor-appinsights-dev`
  + *Region*: `West Europe`

### Add Application Insights to the Web App
In Visual Studio:
+ Right-click the **WebApplication** project, **Add** > **Application Insights telemetry...**
+ Click **Get Started**
+ Under **Resource** select the existing Application Insights instance `az{your_id}-monitor-appinsights-dev`
+ Click **Register**

This will..
1. add the NuGet package `Microsoft.ApplicationInsights.AspNetCore` to the project
1. add `.UseApplicationInsights()` to the `Program.CreateWebHostBuilder` method
1. add the Application Insights instrumentation key to appsettings.json
1. inject a java script snippet into the `_Layout.cshtml` file.

`UseApplicationInsights` will ensure that our application is now responsible for sending telemetry to Application Insights. Previously this was done by an agent on our web host. When the agent detects the presence of the Application Insights NuGet assembly, it will automatically stop sending telemetry to prevent both sources doing so. This is often a source of confusion, as the mere presence of the assembly is enough to stop telemetry from the agent... if you application does not take up the mantle (possibly due to misconfiguration) then no telemetry data is received.  **Be aware** that you could publish a new version which removes the NuGet package, but it may still remain on the host server after re-publishing... and so must be expressly deleted.

The java script will execute from the users browser and provide telemetry about client-side performance of our application.

Once Visual Studio has completed instrumenting your code, expand the **Configured** section. You will notice this includes **Publish annotation configured**. This will allow Visual Studio or Azure DevOps to publish a message to Application Insights when we Publish the web site. These annotation messages are highlighted in the Application Insights timelines, and allow you to track performance against released versions.

The application has been instrumented with the key to the Application Insights instance.  Be careful not to publish development telemetry to a production instance of Application Insights, as this will make debugging and performance tuning difficult.
**NOTE:** The web app config setting will override the setting in `appsettings.json` file. However, you may wish to consider moving the Application Insights key from `appsettings.json` to `appsettings.Development.json`.

### Re-Publish the web app to Azure
+ In Visual Studio:
+ Right-click the `WebApplication` project, **Publish...**
+ Click **Publish**.
+ When the application has been published to Azure it will open in your browser.

Again generate some telemetry data by click around the application, viewing and editing customers.


### Going further
We have instrumented our application with Application Insights, but so far we're not getting any additional benefit.

If time allows after the end of this lab, try implementing custom telemetry in your application to log events (e.g. customer edited or deleted) and metrics (e.g. the time taken to complete calls to our Function, or to deserialize the return list of all customers).

See the following documentation articles for more information on how this is done:
[Application Insights for ASP.NET Core applications](https://docs.microsoft.com/en-us/azure/azure-monitor/app/asp-net-core#enable-client-side-telemetry-for-web-applications)
[Application Insights API for custom events and metrics](https://docs.microsoft.com/en-us/azure/azure-monitor/app/api-custom-events-metrics)


# Lab 4 - Dashboard
Azure Dashboards are a useful way to setup a "one stop" view to get a overview on how your application is behaving.
We can combine charts from Azure Monitor, Application Insights, and include quick links to resources or runbooks.

+ In the portal, click Dashboard in the far left menu.
+ Click the **+New dashboard** link at the top of the dashboard page.
  + Provide a new for your dashboard, e.g. "Monitor Lab Dashboard"
  + From the left hand menu add a **Clock**. Set this up to show local time.
  + Add another **Clock**. Set this up to show the time in New York, i.e. (UTC-05:00) Eastern Time (US & Canada).
  + Add another **Clock**. Set this up to show the time in Tokyo, i.e. (UTC+09:00) Osaka, Sapporo, Tokyo.
  + Having multiple clocks available at a glance can be very useful when working on a global application.
  + The order of the clocks doesn't reflect the progression of timezones. Try re-arranging the clocks to be in the order Tokyo, London, New York.
    This is done by dragging-and-dropping the clock "tiles" on the dashboard.
  + From the left hand menu add **Application Map**.
    + Click **Configure tile**
    + *Resource group*: `az{your_id}-monitor-rg`
    + *Application Insights*: `az{your_id}-monitor-appinsights`
    + Click Apply
  + This has added the application map we saw previously in our Application Insights instance.
+ When you're done editing the dashboard, click the **Done customising** button at the top of the screen.

We now have the initial workings of a dashboard to monitor our application. But lets add a chart from Azure Monitor for the response time of our application.
+ Navigate to the web app's blade, **Monitoring** > **Metrics**.
+ Create a chart displaying Average Response Time (as done in Lab 1).
+ In the top right of the chart click the **Pin to dashboard** button, and select **Pin to current dashboard**.
This will add the chart to our dashboard.

It would also be useful to have a chart showing any failures. We can get this from Application Insights.
+ In your Application Insights instance, goto **Failures**
+ Filter the chart by the web app role, and in the chart click the pin to add the chart to the dashboard.
+ Now, filter the chart by the function app role, and in the chart click the pin to add the chart to the dashboard.
+ In the Application Insights **Overview**, pin the availability chart too.

We also want some quick links from the dashboard to the resources which we are monitoring.
+ From the overview blades of the web app, function app, and database, click the pin in the top right to add a quick link to the dashboard.

Lastly lets add a tile for notifications from Microsoft when issues arise in Azure:
+ Click the **Edit** link at the top of the dashboard
+ Add the **Service Health** tile.

#### Rearranging dashboards
Return to your dashboard. Our charts and links are all stacked in a vertical column. Click the **Edit** link at the top of the dashboard, and drag and drop the tiles to arrange them sensibly on the dashboard. Notice that for some tiles you can use the "grabber" on the bottom-right of the tile to resize the it, or click the ellipsis on the top-right of a tile and this presents pre-canned sizes.

We now have a good dashboard showing us the overall health of our application. With this single view we can quickly get access to any information we would need to get a holistic view of our application. We can also quickly navigate to our resources using the pinned resource links.

#### Sharing dashboards
We could share this dashboard with our wider team by click the **Share** link at the top of the dashboard.
+ When doing so, unclick **Publish to the 'dashboards' resource group** and instead select the `az{your_id}-monitor-rg` resource group.
+ Click **Publish**.

We can also download the definition of this Dashboard, using the **Download** link. This gives us a JSON file, which we can add to our code base or email to
colleagues (who would then use the **Upload** feature).

#### Multiple dashboards
From the drop down menu at the top of the Dashboard, we can switch between all of the dashboards we have available to us.
This means that we can have multiple dashboards and switch between them depending on our role at a particular time (e.g. developer, test environment support, production support, etc).

# Complete
In this lab you have explored some of the features of Azure Monitor.

You may now delete the `az{your_id}-monitor-rg` resource group, and the **Monitor Lab Dashboard**.