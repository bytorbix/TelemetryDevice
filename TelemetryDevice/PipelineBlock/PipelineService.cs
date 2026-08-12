using System.Threading.Tasks.Dataflow;
using TelemetryDevice.BuilderBlock;
using TelemetryDevice.ListenerBlock;
using TelemetryDevice.ParserBlock;
using TelemetryDevice.KafkaBlock;

namespace TelemetryDevice.PipelineBlock
{
    public class PipelineService
    {
        public PipelineService(Listener listener, Builder builder, Parser parser, Kafka kafka ,ILogger<PipelineService> logger)
        {

            DataflowLinkOptions linkOptions = new() { PropagateCompletion = true };
            ActionBlock<string> sink = new(json => logger.LogInformation("Parsed telemetry: {Json}", json));
            BroadcastBlock<string> broadcast = new(x => x);

            listener.Block.LinkTo(builder.Block, linkOptions);
            builder.Block.LinkTo(parser.Block, linkOptions);

            parser.Block.LinkTo(broadcast, linkOptions);
            broadcast.LinkTo(sink, linkOptions);
            broadcast.LinkTo(kafka.Block, linkOptions);



            ObserveFaults(listener.Block, logger, "Listener");
            ObserveFaults(builder.Block, logger, "Builder");
            ObserveFaults(parser.Block, logger, "Parser");
            ObserveFaults(sink, logger, "Sink");
        }

        private static void ObserveFaults(IDataflowBlock block, ILogger logger, string blockName)
        {
            block.Completion.ContinueWith(
                task => logger.LogError(task.Exception, "{Block} pipeline stage faulted.", blockName),
                TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
