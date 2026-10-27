using System;
using Luxodd.Game.Scripts.Game.Leaderboard;
#if NEWTONSOFT_JSON
using Newtonsoft.Json;
#endif

namespace Luxodd.Game.Scripts.Network.CommandHandler
{
    public class LeaderboardRequestCommandHandler : BaseCommandHandler
    {
        public LeaderboardRequestCommandHandler(WebSocketService webSocketService) : base(webSocketService)
        {
        }

        public override void SendCommand(Action onCommandComplete, params object[] parameters)
        {
            _onCommandCompletedCallback = onCommandComplete;
#if NEWTONSOFT_JSON
            var commandRequest = new CommandRequestJson()
            {
                Type = "leaderboard_request",
                Version = "1.0",
            };

            var commandRequestJson = JsonConvert.SerializeObject(commandRequest);
            commandRequestJson = commandRequestJson.Replace("null", "{}");
            
            WebSocketService.SendCommand(CommandRequestType.LeaderboardRequest, commandRequestJson,
                OnCommandResponseSuccessHandler);
            #endif
            SendStatus = CommandSendStatus.Pending;
        }
        
        protected override void OnCommandResponseSuccessHandler(CommandRequestHandler responseHandler)
        {
            base.OnCommandResponseSuccessHandler(responseHandler);
            ErrorMessage = null;
            if (ResponseStatus != CommandResponseStatus.Ok)
            {
                OnCommandResponseFailureHandler($"Leaderboard request failed ({StatusCode}).");
                return;
            }
#if NEWTONSOFT_JSON
            try
            {
                var payloadJson = JsonConvert.SerializeObject(ResponseHandler.Payload);
                var payloadObject = JsonConvert.DeserializeObject<LeaderboardDataResponse>(payloadJson);
                if (payloadObject == null)
                {
                    OnCommandResponseFailureHandler("Leaderboard response contained no payload.");
                    return;
                }

                // An unranked player may have no current_user. The game handles that
                // normally; logging their score must not prevent the completion callback.
                ResponseHandler.Payload = payloadObject;
            }
            catch (JsonException)
            {
                OnCommandResponseFailureHandler("Leaderboard response contained invalid data.");
                return;
            }
#endif
            _onCommandCompletedCallback?.Invoke();
        }
    }
}
