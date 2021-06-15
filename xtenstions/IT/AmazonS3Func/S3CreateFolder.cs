using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Amazon.S3.Model;
using Amazon.Runtime;
using Amazon.S3;

namespace functions
{
    public static class S3CreateFolder
    {
        [FunctionName("S3CreateFolder")]
        public static async Task<String> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            var bucketName = req.Query["bucketName"];
            var access = req.Headers["access"];
            var secret = req.Headers["secret"];
            var region = req.Query["region"];
            var folderName = req.Query["folderName"];

            {
                var credentials = new BasicAWSCredentials(access, secret);
                var config = new AmazonS3Config

                {
                    RegionEndpoint = andys.function.S3Region.getAWSRegion(region)
                };

                using var client = new AmazonS3Client(credentials, config);

                try
                {
                    PutObjectRequest request = new PutObjectRequest()
                    {
                        BucketName = bucketName,
                        Key = folderName
                    };
                    PutObjectResponse response = await client.PutObjectAsync(request);
                }

                catch (AmazonS3Exception amazonS3Exception)
                {
                    if (amazonS3Exception.ErrorCode != null && (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                    {
                        Console.WriteLine("Incorrect AWS Credentials.");
                        return ("Incorrect AWS Credentials.");
                    }
                    else
                    {
                        Console.WriteLine("Error: ", amazonS3Exception.ErrorCode, amazonS3Exception.Message);
                        return ("Error: ", amazonS3Exception.ErrorCode, amazonS3Exception.Message).ToString();
                    }
                }
                
                return folderName;

            }
        }
    }
}
