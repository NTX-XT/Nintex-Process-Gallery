using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Amazon.S3;
using Amazon.Runtime;
using Amazon.S3.Model;
using System.Collections.Generic;
using Andys.Function;

namespace Andys.Function
{
    public static class S3ListBuckets
    {
        [FunctionName("S3ListBuckets")]
        public static async Task<List<s3mod>> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            var region = req.Query["region"];
            string access = req.Headers["access"];
            string secret = req.Headers["secret"];

            List<s3mod> bucketnames = new List<s3mod>();

            var credentials = new BasicAWSCredentials(access, secret);
            var config = new AmazonS3Config

            {
                RegionEndpoint = andys.function.S3Region.getAWSRegion(region)
            };

            try
            {
                using var client = new AmazonS3Client(credentials, config);
                ListBucketsResponse buckets = await client.ListBucketsAsync();
                foreach (var bucket in buckets.Buckets)
                {

                    bucketnames.Add(new s3mod() { name = "Bucket Name", value = bucket.BucketName });
                }
                
            }
            catch(AmazonS3Exception amazonS3Exception)
            {
                 if (amazonS3Exception.ErrorCode != null && (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                    {
                        Console.WriteLine("Incorrect AWS Credentials.");
                    bucketnames.Add(new s3mod() { name = "Error", value = "Incorrect AWS Credentials" });
                }
                    else
                    {
                        Console.WriteLine("Error: ", amazonS3Exception.ErrorCode, amazonS3Exception.Message);
                    bucketnames.Add(new s3mod() { name = amazonS3Exception.ErrorCode, value = amazonS3Exception.Message });
                }

                
            }
            return bucketnames;
        }
    }
}
