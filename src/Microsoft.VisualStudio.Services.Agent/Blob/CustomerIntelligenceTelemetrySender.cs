// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.Agent.Util;
using Microsoft.VisualStudio.Services.Content.Common.Telemetry;
using Microsoft.VisualStudio.Services.CustomerIntelligence.WebApi;
using Microsoft.VisualStudio.Services.WebPlatform;

namespace Microsoft.VisualStudio.Services.Agent.Blob
{
    public class CustomerIntelligenceTelemetrySender : ITelemetrySender
    {
        private CustomerIntelligenceHttpClient _ciClient;

        private long _chunksUploaded = 0;
        private long _compressionBytesSaved = 0;
        private long _dedupUploadBytesSaved = 0;
        private long _logicalContentBytesUploaded = 0;
        private long _physicalContentBytesUploaded = 0;
        private long _totalNumberOfChunks = 0;

        public CustomerIntelligenceTelemetrySender(VssConnection connection)
        {
            ArgUtil.NotNull(connection, nameof(connection));
            _ciClient = connection.GetClient<CustomerIntelligenceHttpClient>();
        }

        // Not used by the interface. We just want to capture successful telemetry for dedup analytics
        public void StartSender()
        {
        }
        public void StopSender()
        {
        }
        public void SendErrorTelemetry(ErrorTelemetryRecord errorTelemetry)
        {
        }
        public void SendRecord(TelemetryRecord record)
        {
        }

        public void SendActionTelemetry(ActionTelemetryRecord actionTelemetry)
        {
            if (actionTelemetry is IDedupRecord dedupRecord)
            {
                var uploadStats = dedupRecord.UploadStatistics;
                if (uploadStats != null)
                {
                    this._chunksUploaded += uploadStats.ChunksUploaded;
                    this._compressionBytesSaved += uploadStats.CompressionBytesSaved;
                    this._dedupUploadBytesSaved += uploadStats.DedupUploadBytesSaved;
                    this._logicalContentBytesUploaded += uploadStats.LogicalContentBytesUploaded;
                    this._physicalContentBytesUploaded += uploadStats.PhysicalContentBytesUploaded;
                    this._totalNumberOfChunks += uploadStats.TotalNumberOfChunks;
                }
            }
        }

        public async Task CommitTelemetry(Guid planId, Guid jobId)
        {
            var ciData = new Dictionary<string, object>();
            ciData.Add("PlanId", planId);
            ciData.Add("JobId", jobId);

            ciData.Add("ChunksUploaded", this._chunksUploaded);
            ciData.Add("CompressionBytesSaved", this._compressionBytesSaved);
            ciData.Add("DedupUploadBytesSaved", this._dedupUploadBytesSaved);
            ciData.Add("LogicalContentBytesUploaded", this._logicalContentBytesUploaded);
            ciData.Add("PhysicalContentBytesUploaded", this._physicalContentBytesUploaded);
            ciData.Add("TotalNumberOfChunks", this._totalNumberOfChunks);

            var ciEvent = new CustomerIntelligenceEvent
            {
                Area = "AzurePipelinesAgent",
                Feature = "BuildArtifacts",
                Properties = ciData
            };
            await _ciClient.PublishEventsAsync(new [] { ciEvent });
        }
    }
}