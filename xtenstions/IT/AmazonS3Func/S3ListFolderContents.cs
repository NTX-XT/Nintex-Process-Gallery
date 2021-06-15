using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace Andys.Function
{
    public static class S3ListFolderContents
    {
        [FunctionName("S3ListFolderContents")]
        public static async Task<List<s3mod>> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            var bucketName = req.Query["bucketName"];
            var access = req.Headers["access"];
            var secret = req.Headers["secret"];
            var region = req.Query["region"];
            var prefix = req.Query["prefix"];

            List<s3mod> folderContents = new List<s3mod>();

            var credentials = new BasicAWSCredentials(access, secret);
            var config = new AmazonS3Config

            {
                RegionEndpoint = andys.function.S3Region.getAWSRegion(region)
            };

            try
            {
                using var client = new AmazonS3Client(credentials, config);
                ListObjectsRequest request = new ListObjectsRequest
                {
                    BucketName = bucketName,
                    Prefix = prefix
                };

                ListObjectsResponse response = await client.ListObjectsAsync(request);
                foreach (S3Object o in response.S3Objects)
                {
                    Console.WriteLine(o.Key);
                    folderContents.Add(new s3mod() { name = "Folder Item", value = o.Key });
                }


            }

            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null && (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    Console.WriteLine("Incorrect AWS Credentials.");
                    folderContents.Add(new s3mod() { name = "Error", value = "Incorrect AWS Credentials" });
                }
                else
                {
                    Console.WriteLine("Error: ", amazonS3Exception.ErrorCode, amazonS3Exception.Message);
                    folderContents.Add(new s3mod() { name = amazonS3Exception.ErrorCode, value = amazonS3Exception.Message });
                }

            }

            return folderContents;
        }
    }
}
