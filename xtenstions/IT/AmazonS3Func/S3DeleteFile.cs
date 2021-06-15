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
    public static class S3DeleteFile
    {
        [FunctionName("S3DeleteFile")]
        public static async Task<String> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            var bucketName = req.Query["bucketName"];
            var access = req.Headers["access"];
            var secret = req.Headers["secret"];
            var region = req.Query["region"];
            var filePath = req.Query["filePath"];

            var credentials = new BasicAWSCredentials(access, secret);
            var config = new AmazonS3Config

            {
                RegionEndpoint = andys.function.S3Region.getAWSRegion(region)
            };

            var deleteFileRequest = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = filePath
            };

            try
            {
                using var client = new AmazonS3Client(credentials, config);
                DeleteObjectResponse fileDeleteResponse = await client.DeleteObjectAsync(deleteFileRequest);
            }

            catch (AmazonS3Exception amazonS3Exception)
            {
                if (amazonS3Exception.ErrorCode != null && (amazonS3Exception.ErrorCode.Equals("InvalidAccessKeyId") || amazonS3Exception.ErrorCode.Equals("InvalidSecurity")))
                {
                    return("Incorrect AWS Credentials.");
                   
                }
                else
                {
                    return("Error: ", amazonS3Exception.ErrorCode, amazonS3Exception.Message).ToString();
                    
                }
                
            }

            return (filePath + "Deleted");
            
        }
    }
}