using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace Andys.Function
{
    public static class S3CreateObject
    {
        [FunctionName("S3CreateObject")]
        public static async Task<String> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous,"post", Route = null)] HttpRequest req,
            ILogger log)
        {

            var bucketName = req.Query["bucketName"];
            var access = req.Headers["access"];
            var secret = req.Headers["secret"];
            var key = req.Query["key"];
            var region = req.Query["region"];
            var contentBody = req.Body;



            var credentials = new BasicAWSCredentials(access, secret);
            var config = new AmazonS3Config

            {
                RegionEndpoint = andys.function.S3Region.getAWSRegion(region)
            };

            try
            {

                using var client = new AmazonS3Client(credentials, config);


                PutObjectRequest request = new PutObjectRequest();
                request.BucketName = bucketName;
                request.Key = key;
                request.ContentType = "text/plain";
                request.ContentBody = contentBody.ToString();
                await client.PutObjectAsync(request);

            }

            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null && (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    return ("Incorrect AWS Credentials.");
                    

                }
                else
                {
                    return ("Error: ", amazonS3Exception.ErrorCode, amazonS3Exception.Message).ToString();

                }

            }



            return key;
        }

    }
}
