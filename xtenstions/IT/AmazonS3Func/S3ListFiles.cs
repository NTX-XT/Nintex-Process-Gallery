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
using System.Collections.Generic;

namespace Andys.Function
{
    public static class S3ListFiles
    {
        [FunctionName("S3ListFiles")]
        public static async Task<List<s3mod>> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            var bucketName = req.Query["bucketName"];
            var access = req.Headers["access"];
            var secret = req.Headers["secret"];
            var region = req.Query["region"];

            List<s3mod> filenames = new List<s3mod>();

            var credentials = new BasicAWSCredentials(access, secret);
            var config = new AmazonS3Config

            {
                RegionEndpoint = andys.function.S3Region.getAWSRegion(region)
        };

            try
            {
                using var client = new AmazonS3Client(credentials, config);

                ListObjectsRequest request = new ListObjectsRequest();
                request.BucketName = bucketName;
                ListObjectsResponse response = await client.ListObjectsAsync(request);
                foreach (S3Object o in response.S3Objects)
                {
                    filenames.Add(new s3mod() { name = "File Name", value = o.Key });
                }

            }

            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null && (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    Console.WriteLine("Incorrect AWS Credentials.");
                    filenames.Add(new s3mod() { name = "Error", value = "Incorrect AWS Credentials" });
                }
                else
                {
                    Console.WriteLine("Error: ", amazonS3Exception.ErrorCode, amazonS3Exception.Message);
                    filenames.Add(new s3mod() { name = amazonS3Exception.ErrorCode, value = amazonS3Exception.Message });
                }


            }

            return filenames;
        }
    }
}
