using BLAM.Enums;
using BLAM.Models.Tasks;
using SDK;
using SDK.ExtensionMethods;
using SDK.TemplateInterpolation;
using SDK.WorkflowIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlamTaskStatus = BLAM.Enums.TaskStatus;

namespace YellaSubtitleTask
{
    class YellaSubtitleTaskBlidget : Blidget<FoldersIO, FoldersIO>
    {
        private BlamClient blamClient;
        private YellaSubtitleTaskRunState runState;

        protected override void Init(FoldersIO inputData)
        {
            runState = GetRunState<YellaSubtitleTaskRunState>();

            blamClient = new BlamClient();
        }

        protected override async Task<BlidgetOutput<FoldersIO>> RunAsync(FoldersIO inputData)
        {
            var name = GetArgument<string>("name");
            var description = GetArgument<string>("description");
            var type = GetArgument<string>("type");
            var userIds = GetArgument<IEnumerable<int>>("userIds");
            var groupIds = GetArgument<IEnumerable<int>>("groupIds");

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("The 'Name' input argument is required for creating a task and was not supplied.");
            }

            if (string.IsNullOrWhiteSpace(type))
            {
                throw new ArgumentException("The 'Type' input argument is required for creating a task and was not supplied.");
            }

            if (!userIds.Any() && !groupIds.Any())
            {
                throw new ArgumentException("No users or groups have been specified for the task to be assigned to.");
            }

            if (userIds.Any() && groupIds.Any())
            {
                throw new ArgumentException("Either the 'Assigned Users' input argument or the 'Assigned Groups' input argument must be supplied but not both.");
            }

            if (inputData.FolderIds.Count() != 1)
            {
                throw new ArgumentException($"Input must be for a single folder.  For multiple folders split workflows when submitting to be processed.");
            }
            
            var project = await blamClient.GetFolderById(inputData.FolderIds.First(), FolderStructureDepth.Full);
            if (project.Assets.Count() != 1)
            {
                throw new ArgumentException($"Requires a single video asset to be provided in the top level folder.");
            }

            var projectAsset = await blamClient.GetAssetById(project.Assets.First().Id);

            var templateData = new TemplateData
            {
                AssetId = projectAsset.Id,
                Asset = projectAsset,
                FolderId = project.Id,
                Folder = project,
                WorkflowRunId = WorkflowRunId
            };

            name = await name.Interpolate(blamClient, templateData);
            if (!string.IsNullOrWhiteSpace(description))
            {
                description = await description.Interpolate(blamClient, templateData);
            }

            var yellaTask = await blamClient.CreateActionTask(new ActionTaskInputModel
            {
                Name = name,
                Description = description ?? "",
                AssetId = templateData?.AssetId,
                ProjectId = templateData?.FolderId,
                WorkOrderId = templateData?.WorkOrderId,
                Type = type,
                UserIds = userIds,
                GroupIds = groupIds
            });

            return Idle(new YellaSubtitleTaskRunState { TaskId = yellaTask.Id });
        }

        protected override async Task<BlidgetOutput<FoldersIO>> CompleteAsync(FoldersIO inputData)
        {
            var task = await blamClient.GetActionTaskById(runState.TaskId);

            if (task.Status == $"{BlamTaskStatus.Completed}")
            {
                return Complete(inputData);
            }
            return Idle(runState);
        }
    }
}
