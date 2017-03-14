﻿#region using directives

using System;
using System.Threading;
using System.Threading.Tasks;
using PokeMaster.Logic.Utils;
using PokemonGo.RocketAPI;
using PokemonGo.RocketAPI.Enums;
using PokemonGo.RocketAPI.Exceptions;
using PokemonGo.RocketAPI.Extensions;
using PokemonGo.RocketAPI.Rpc;
using POGOProtos.Networking.Envelopes;
using POGOProtos.Networking.Responses;
using System.Windows.Forms;
using PokemonGo.RocketAPI.Helpers;
using PokeMaster.Logic.Shared;
#endregion


namespace PokeMaster.Logic
{
    public class ApiFailureStrat 
    {
        private readonly Client _session;
        private int _retryCount;
        public static Player _player;
        public static PokeMaster.Logic.Shared.ISettings _settings;
        

        public ApiFailureStrat(Client session)
        {
            _session = session;
        }
        
        public void HandleCaptcha(string challengeUrl, ICaptchaResponseHandler captchaResponseHandler)
        {
           
            /* We recieve the token after the user has completed the captcha
             * The site will want to redirect the user, to the app again
             * So the redirect url would look like this: "unity:some-long-ass-code"
             * This "long-ass-code" is the responseToken
             * */
            Logger.ColoredConsoleWrite(ConsoleColor.Green, "Are you an human?");
            Task.Run(() => {
                var chelper = new Utils.CaptchaHelper(challengeUrl);
                if (chelper.ShowDialog() == DialogResult.OK) {
                    var token = chelper.TOKEN;
                    captchaResponseHandler.SetCaptchaToken(token);
                             
                    // We will send a request, passing the long-ass-token and wait for a response.
                    VerifyChallengeResponse r = _player.VerifyChallenge(token);
                    if (r.Success) {
                        Logger.ColoredConsoleWrite(ConsoleColor.Green, "TOKEN OK!");
                    } else {
                        Logger.ColoredConsoleWrite(ConsoleColor.Green, "Failure.");
                        HandleCaptcha(challengeUrl, captchaResponseHandler);
                    }
                    RandomHelper.RandomSleep(2000, 2200);
                } else {
                    Logger.ColoredConsoleWrite(ConsoleColor.Green, "Canceled.");
                    System.Console.ReadKey();
                    Environment.Exit(0);
                }
            }).Wait();

        }
                

        public ApiOperation HandleApiFailure()
        {
            if (_retryCount == 11)
                return ApiOperation.Abort;

            Task.Delay(500).Wait();
            _retryCount++;

            if (_retryCount % 5 == 0)
            {
                DoLogin();
            }

            return ApiOperation.Retry;
        }

        public void HandleApiSuccess()
        {
            _retryCount = 0;
        }

        private async void DoLogin()
        {
            try
            {
                if (_session.AuthType == AuthType.Google || _session.AuthType == AuthType.Ptc)
                {
                    _session.Login.DoLogin();
                }
                else
                {
                    Logger.Error("Wrong AuthType?");
                }
            }
            catch (AggregateException ae)
            {
                throw ae.Flatten().InnerException;
            }
            catch (LoginFailedException)
            {
                Logger.Error("Wrong Username or Password?");
            }
            catch (AccessTokenExpiredException)
            {
                Logger.Error("Access Token expired? Retrying in 1 second.");

                await Task.Delay(1000).ConfigureAwait(false);
            }
            catch (PtcOfflineException)
            {
                Logger.Error("PTC probably offline? Retrying in 15 seconds.");

                await Task.Delay(15000).ConfigureAwait(false);
            }
            catch (InvalidResponseException)
            {
                Logger.Error("Invalid Response, retrying in 5 seconds.");
                await Task.Delay(5000).ConfigureAwait(false);
            } catch (NullReferenceException e)
            {
                Logger.Error("Method which calls that: " + e.TargetSite + " Source: " + e.Source + " Data: " + e.Data);
            }
            catch (GoogleException)
            {

            }
            catch (Exception ex)
            {
                throw ex.InnerException;
            }
        }
        public void HandleApiSuccess(RequestEnvelope request, ResponseEnvelope response)
        {
            
            _retryCount = 0;

                /*Logger.ColoredConsoleWrite(ConsoleColor.Cyan, $"Accuracy: {request.Accuracy}");
                //Logger.ColoredConsoleWrite(ConsoleColor.Cyan, $"--------------[AUTH INFO]-------------");
                //Logger.ColoredConsoleWrite(ConsoleColor.Cyan, $"Auth Provider: {request.AuthInfo.Provider}");
                //Logger.ColoredConsoleWrite(ConsoleColor.Cyan, $"Auth Token: {request.AuthInfo.Token}");
                Logger.ColoredConsoleWrite(ConsoleColor.Cyan, $"------------[AUTHTICKET INFO]------------");
                Logger.ColoredConsoleWrite(ConsoleColor.Cyan, $"AuthTicket End: {request.AuthTicket.End}");
                Logger.ColoredConsoleWrite(ConsoleColor.Cyan, $"AuthTicket Expire (MS): {request.AuthTicket.ExpireTimestampMs}");
                Logger.ColoredConsoleWrite(ConsoleColor.Cyan, $"AuthTicket Start: {request.AuthTicket.Start}");
                Logger.ColoredConsoleWrite(ConsoleColor.Cyan, $"-----------------------------------------");
                Logger.ColoredConsoleWrite(ConsoleColor.Cyan, $"Latitude: {request.Latitude}");
                Logger.ColoredConsoleWrite(ConsoleColor.Cyan, $"Longitude: {request.Longitude}");
                Logger.ColoredConsoleWrite(ConsoleColor.Cyan, $"Milliseconds SinceLastLocationFix: {request.MsSinceLastLocationfix}");
                Logger.ColoredConsoleWrite(ConsoleColor.Cyan, $"PlatformRequests: {request.PlatformRequests}");
                Logger.ColoredConsoleWrite(ConsoleColor.Cyan, $"RequestId: {request.RequestId}");
                Logger.ColoredConsoleWrite(ConsoleColor.Cyan, $"Requests: {request.Requests}");
                Logger.ColoredConsoleWrite(ConsoleColor.Cyan, $"Status Code: {request.StatusCode}");*/
            
        }

        public ApiOperation HandleApiFailure(RequestEnvelope request, ResponseEnvelope response)
        {
            if (_retryCount == 11)
                return ApiOperation.Abort;

            Task.Delay(500).Wait();
            _retryCount++;

            if (_retryCount % 5 == 0)
            {
                try
                {
                    DoLogin();
                }
                catch (PtcOfflineException)
                {
                    Task.Delay(20000).Wait();
                }
                catch (AccessTokenExpiredException)
                {
                    Task.Delay(2000).Wait();
                }
                catch (Exception ex) when (ex is InvalidResponseException || ex is TaskCanceledException)
                {
                     Task.Delay(1000).Wait();
                }
            }

            return ApiOperation.Retry;
        }
    }
}