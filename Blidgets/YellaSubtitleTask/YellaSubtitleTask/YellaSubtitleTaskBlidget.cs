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
            var priorityTemplate = GetArgument<string>("priority");
            var dueDateTemplate = GetArgument<string>("due_date");
            var tags = GetArgument<IEnumerable<string>>("tags") ?? Array.Empty<string>();
            var userIds = GetArgument<IEnumerable<int>>("userIds") ?? Array.Empty<int>();
            var groupIds = GetArgument<IEnumerable<int>>("groupIds") ?? Array.Empty<int>();

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("The 'Name' input argument is required for creating a task and was not supplied.");
            }

            if (string.IsNullOrWhiteSpace(type))
            {
                throw new ArgumentException("The 'Type' input argument is required for creating a task and was not supplied.");
            }

            if (!tags.Any() && !userIds.Any() && !groupIds.Any())
            {
                throw new ArgumentException("No users or groups have been specified for the task to be assigned to.");
            }

            if ((tags.Any() || userIds.Any()) && groupIds.Any())
            {
                throw new ArgumentException("Either the 'User Tags | Assigned Users' input argument or the 'Assigned Groups' input argument must be supplied but not both.");
            }

            if (inputData.FolderIds.Count() != 1)
            {
                throw new ArgumentException($"Input must be for a single folder.  For multiple folders split workflows when submitting to be processed.");
            }

            // Append Tagged UserIds
            if (tags.Any())
            {
                userIds = await GetDistinctUserIds(userIds, tags);
            }

            var project = await blamClient.GetFolderById(inputData.FolderIds.First(), FolderStructureDepth.Full);
            var templateData = new TemplateData
            {
                FolderId = project.Id,
                Folder = project,
                WorkflowRunId = WorkflowRunId
            };

            name = await name.Interpolate(blamClient, templateData);
            if (!string.IsNullOrWhiteSpace(description))
            {
                description = await description.Interpolate(blamClient, templateData);
            }

            var priority = 0;
            if (!string.IsNullOrWhiteSpace(priorityTemplate))
            {
                priority = int.TryParse(await priorityTemplate.Interpolate(blamClient, templateData), out var value) ? value : 0;
            }

            DateTimeOffset? dueDate = null;
            if (!string.IsNullOrWhiteSpace(dueDateTemplate))
            {
                dueDate = DateTimeOffset.TryParse(await dueDateTemplate.Interpolate(blamClient, templateData), out var value) ? value : null;
            }

            var yellaTask = await blamClient.CreateActionTask(new ActionTaskInputModel
            {
                Name = name,
                Description = description ?? "",
                AssetId = templateData?.AssetId,
                ProjectId = templateData?.FolderId,
                WorkOrderId = templateData?.WorkOrderId,
                Type = type,
                Priority = priority,
                DueDate = dueDate,
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

        private async Task<IEnumerable<int>> GetDistinctUserIds(IEnumerable<int> userIds, IEnumerable<string> tags)
        {
            var output = new HashSet<int>(userIds);

            var users = await blamClient.GetUsers();
            var matches = users.Where(p => p.Tags.Intersect(tags).Any()).Select(p => p.Id);

            return output.Union(matches);
        }
    }
}
