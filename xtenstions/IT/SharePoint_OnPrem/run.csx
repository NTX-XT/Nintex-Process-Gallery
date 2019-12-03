#r "Newtonsoft.Json"
#r "Newtonsoft.Json.Schema.dll"

using System;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using System.Security.Cryptography;
using System.Text;

private static string SecurityHeader = "SecurityKey";

private static string ExternalStartUrl = "https://run.nintex.io/x-start/";

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    IEnumerable<string> securityKeys;
    if (!req.Headers.TryGetValues(SecurityHeader, out securityKeys))
    {
        return req.CreateResponse(HttpStatusCode.Unauthorized, "SecurityKey is not provided.");
    }

    var securityKey = securityKeys.FirstOrDefault().Trim();

    if (string.IsNullOrWhiteSpace(securityKey))
    {
        return req.CreateResponse(HttpStatusCode.Unauthorized, "SecurityKey is not provided.");
    }

    string id = req.GetQueryNameValuePairs()
        .FirstOrDefault(q => string.Compare(q.Key, "workflowId", true) == 0)
        .Value.Trim();

    if(string.IsNullOrWhiteSpace(id)) return req.CreateResponse(HttpStatusCode.BadRequest, "Workflow Id required.");

    // Set name to query string or body data
    string endpointUrl = ExternalStartUrl + id;

    // This is for introspection, getting the structure of all start workflow variables
    if(req.Method == System.Net.Http.HttpMethod.Get)
    {
        JObject[] wfVariables = await GetWorkflowVariables(endpointUrl);

        foreach(var ob in wfVariables)
        {
            log.Info(ob.ToString());
        }

        var schema = new JSchema
        {
            Type = JSchemaType.Object,
            Title = "Start Variables"
        };
        
        foreach(var wfVariable in wfVariables)
        {
            schema.Properties.Add(
                wfVariable["Name"].ToString(), 
                GetSchemaValue(wfVariable["Type"].ToString(), wfVariable["Name"].ToString())
            );

            if((bool)wfVariable["Required"])
            {
                schema.Required.Add(wfVariable["Name"].ToString());
            }
        }
        return req.CreateResponse(HttpStatusCode.OK, schema);
    }

    // Get request body
    JObject inputVariables = await req.Content.ReadAsAsync<JObject>();

    //log.Info(endpointUrl);
    //log.Info(securityKey);
    //log.Info(inputVariables.ToString());

    try
    {     
        if(inputVariables != null)
        {
            JObject[] wfVariables = await GetWorkflowVariables(endpointUrl);
            // Validate the input
            foreach(var variable in wfVariables.Where(v => (bool)v["Required"]))
            {
                if(inputVariables[variable["Name"].ToString()] == null)
                {
                    return req.CreateResponse(HttpStatusCode.BadRequest, $"Required start variable {variable["Name"]} not provided.");
                }
            }
        }
    }
    catch(Exception ex)
    {
        return req.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
    }

    var correlationId = await StartWorkflow(endpointUrl, securityKey, inputVariables);

    var result = new JObject();
    result.Add("correlationId", correlationId);

    return req.CreateResponse(HttpStatusCode.OK, result);
}

private static JSchema GetSchemaValue(string wfType, string name)
{
    var schema = new JSchema { Title = name };
    schema.ExtensionData.Add("x-ntx-summary", name);

    switch(wfType)
    {
        case "Text":
            schema.Type = JSchemaType.String;
            break;
        case "Boolean":
            schema.Type =  JSchemaType.Boolean;
            break;
        case "DateTime":
            schema.Type =  JSchemaType.String;
            schema.Format = "date-time";
            break;
        case "GUID":
            schema.Type =  JSchemaType.String;
            break;
        case "Number":
            schema.Type =  JSchemaType.Number;
            break;
        case "Integer":
            schema.Type =  JSchemaType.Integer;
            break;
        case "Choice":
            schema.Type =  JSchemaType.String;
            break;    
        default:
            schema.Type =  JSchemaType.String;
            break;
    }

    return schema;
}

private static async Task<JObject[]> GetWorkflowVariables(string endpointUrl)
{
    // Send a GET request to the specified endpoint URL. No authorization or authentication is
    // required for this request.
    HttpClient client = new HttpClient();
    client.DefaultRequestHeaders.Accept.Add(new   System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    client.BaseAddress = new Uri(endpointUrl);
    HttpResponseMessage response = await client.GetAsync(endpointUrl);

    // If a response is returned, check the HTTP status code.
    if (response != null)
    {
        switch (response.StatusCode)
        {
            case HttpStatusCode.OK:
                // Success - deserialize and return the list of workflow variables.
                return await response.Content.ReadAsAsync<JObject[]>();
            case HttpStatusCode.BadRequest:
                // Failure - the endpoint URL could not access a workflow.
                throw new ArgumentException("The request could not be processed.");
            default:
                throw new Exception("An unexpected error has occurred.");
        }
    }
    else
    {
        return null;
    }
}

private static string CalculateDigest(string securityKey, string httpMethod, string path, string nonce, string timestamp, string requestBody)
{
    // The data values are concatenated into a single string, in which each data value is delimited by 
    // a colon (:) character, which is then encoded as a UTF-8 byte array.
    var dataBytes = Encoding.UTF8.GetBytes(String.Join(":", httpMethod, path, nonce, timestamp, requestBody));

    // The security key is encoded as a UTF-8 byte array.
    var keyBytes = Encoding.UTF8.GetBytes(securityKey);

    // Using the HMACSHA256 object provided by .NET Framework, the
    // data values are hashed, using the security key, and any dashes are removed.
    using (var hasher = new HMACSHA256(keyBytes))
    {
        var hashBytes = hasher.ComputeHash(dataBytes);
        return BitConverter.ToString(hashBytes).Replace("-", "");
    }
}

private static async Task<string> StartWorkflow(string endpointUrl, string securityKey, JObject workflowVariables = null)
{
    // If no workflow variable values are provided, send an empty request body; otherwise, send
    // a serialized JSON object, in which each workflow variable value is represented as a property value.
    string requestBody = workflowVariables == null ? string.Empty : workflowVariables.ToString();

    // Retrieve and configure the values used to calculate the digest value for the request.
    var path = new Uri(endpointUrl).AbsolutePath.ToLower();
    var httpMethod = HttpMethod.Post.ToString().ToLower();
    var nonce = Guid.NewGuid().ToString();
    var timestamp = DateTime.UtcNow.ToString("O");

    // Calculate and return the digest value for the request.
    var digest = CalculateDigest(securityKey, httpMethod, path, nonce, timestamp, requestBody);

    // Send the request to the endpoint URL. 
    HttpClient client = new HttpClient();
    var request = new HttpRequestMessage(HttpMethod.Post, endpointUrl);

    // Specify the header values for the request.
    request.Headers.Add("X-Api-Digest", digest);
    request.Headers.Add("X-Api-Timestamp", timestamp);
    request.Headers.Add("X-Api-Nonce", nonce);
    request.Headers.Add("X-Api-Source", "ExternalStart");

    request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

    var response = await client.SendAsync(request);

    if (response != null)
    {
        switch (response.StatusCode)
        {
            case HttpStatusCode.OK:
                // Success - the message was successfully sent.
                IEnumerable<string> correlationIds;
                response.Headers.TryGetValues("X-CorrelationId", out correlationIds);
                return correlationIds.FirstOrDefault();
            case HttpStatusCode.BadRequest:
                // Failure - the endpoint URL could not access a workflow.
                throw new ArgumentException("The request could not be processed.");
            default:
                throw new Exception("An unexpected error has occurred.");
        }
    }
    else
    {
        throw new Exception("An unexpected error has occurred.");
    }
}