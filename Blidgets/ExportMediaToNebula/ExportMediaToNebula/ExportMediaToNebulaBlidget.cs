using BLAM.Constants;
using BLAM.Enums;
using BLAM.Models.AccessTokens;
using BLAM.Models.Assets;
using SDK;
using SDK.ExtensionMethods;
using SDK.TemplateInterpolation;
using SDK.WorkflowIO;
using System;
using System.Linq;
using System.Threading.Tasks;
using YellaAPI;
using YellaAPI.Models.Nebula;

namespace ExportMediaToNebula
{
    class ExportMediaToNebulaBlidget : Blidget<AssetFilesIO, AssetFilesIO>
    {
        private BlamClient blamClient;
        private YellaClient yellaClient;
        private NebulaConversionRunState runState;

        protected override async Task InitAsync(AssetFilesIO inputData)
        {
            runState = GetRunState<NebulaConversionRunState>();

            var yellaCredentialsId = GetArgument<int>("credentials_id");

            blamClient = new BlamClient();

            var yellaCredentials = await blamClient.GetCredentialsById(yellaCredentialsId);
            yellaClient = new YellaClient(new YellaClientConfig
            {
                Host = yellaCredentials.Fields["Host"],
                Port = int.TryParse(yellaCredentials.Fields["Port"], out var value) ? value : null,
                Token = yellaCredentials.Fields["API Key"]
            });
        }

        protected override async Task<BlidgetOutput<AssetFilesIO>> RunAsync(AssetFilesIO inputData)
        {
            var projectNameTemplate = GetArgument<string>("project_name");
            if (string.IsNullOrWhiteSpace(projectNameTemplate))
            {
                projectNameTemplate = "{{ASSET_TITLE}}";
            }

            foreach (var fileId in inputData.AssetFileIds)
            {
                var assetFile = await blamClient.GetAssetFileById(fileId);
                var asset = await blamClient.GetAssetById(assetFile.AssetId);

                var url = await GetStreamingUrl(assetFile);

                var yellaJob = await yellaClient.StartConversion(new NebulaConversionStartRequest
                {
                    Pluginname = "videoconversion2",
                    Projectname = await projectNameTemplate.Interpolate(blamClient, new TemplateData
                    { 
                        Asset = asset,
                        AssetFile = assetFile,
                        WorkflowRunId = WorkflowRunId
                    }),
                    Srcfile = url,
                    Medianame = asset.Title,
                    Mediafolder = asset.Title,
                    Addtext = new { }
                });

                runState.Jobs.Add(new NebulaConversionJob
                {
                    AssetFileId = assetFile.Id,
                    JobId = yellaJob.Id,
                    OriginalFilename = assetFile.OriginalFilename
                });
            }

            return Waiting(runState, PollingInterval.OneMinute / 2);
        }

        protected override async Task<BlidgetOutput<AssetFilesIO>> CompleteAsync(AssetFilesIO inputData)
        {
            var yellaJobs = await yellaClient.GetConversionStatus(new NebulaConversionGetStatusRequest
            {
                Pluginname = "videoconversion2"
            });

            var incompleteJobs = runState.Jobs.Where(p => !p.Complete);
            foreach (var job in incompleteJobs)
            {
                if (yellaJobs.Ids.TryGetValue(job.JobId, out var yellaJob))
                {
                    job.Percent = yellaJob.Percent;

                    if (yellaJob.Status == "complete")
                    {
                        job.Complete = true;
                        job.Success = true;
                    }
                    else if (yellaJob.Status == "error")
                    {
                        job.Complete = true;
                        job.Success = false;
                        job.Message = yellaJob.Error;
                        job.Percent = 100;
                    }
                }
                else
                {
                    throw new Exception($"Unable to find Nebula Conversion Job ID '{job.JobId}'");
                }
            }


            if (runState.Jobs.Any(p => !p.Complete))
            {
                UpdateProgress((runState.Jobs.Sum(p => p.Percent) / runState.Jobs.Count), $"Completed {runState.Jobs.Count(p => p.Complete)} of {runState.Jobs.Count} conversions.");
                return Waiting(runState, PollingInterval.OneMinute / 2);
            }
            else if (runState.Jobs.Any(p => !p.Success))
            {
                throw new Exception($"Conversion job(s) failed: {string.Join("\n", runState.Jobs.Where(p => !p.Success).Select(p => $"Error converting '{p.OriginalFilename}': {p.Message}"))}");
            }

            return Complete(inputData);
        }

        private async Task<string> GetStreamingUrl(AssetFileViewModel assetFile)
        {
            try
            {
                var url = string.Empty;
                var streamingUrlViewModel = await blamClient.GetStreamingUrl(assetFile.Id, new FileTokenRequestInputModel
                {
                    TokenType = AccessTokenType.WorkflowOperation,
                    MaxAllowedClaims = MaxAllowedClaims.Unlimited,
                    TokenExpiry = DateTimeOffset.Now + TimeSpan.FromDays(3)
                });
                url = streamingUrlViewModel.StreamingUrl;

                // Perhaps get the streaming url to be the full URL - then we wouldn't need to do this
                if (url.StartsWith("api/streams", StringComparison.InvariantCultureIgnoreCase))
                {
                    url = $"{BlamClient.BlamUrl}/{url}";
                }

                return url;
            }
            catch
            {
                throw new Exception($"Cannot get streaming URL for asset file ID {assetFile.Id}");
            }
        }
    }
}
