using System;
using System.Collections;
using GoogleMobileAds.Api;
using UnityEngine;

public class Rewarded : Debuggable
{
    RewardedAd rewardedAd;

    [HideInInspector]
    public ADLoadingStatus status = ADLoadingStatus.NotInitialized;

    Action RewardAction = null;

    string GET_ID()
    {
        if (GameManager.instance.IS_DEVELOPMENT_MODE)
            return Constants.ADMOB.Test.REWARDED;
        else
            return Constants.ADMOB.REWARDED;
    }

    public void Initialize(Action rewardAction)
    {
        RewardAction = rewardAction;
        StartCoroutine(HandleInitialize());
    }

    IEnumerator HandleInitialize()
    {
        while (status == ADLoadingStatus.OnProgress)
            yield return null;

        if (status == ADLoadingStatus.Success)
        {
            Show();
            yield break;
        }

        status = ADLoadingStatus.NotInitialized;
        Load();
    }

    public void Load()
    {
        status = ADLoadingStatus.OnProgress;
        // Clean up the old ad before loading a new one.
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        Log("Loading the rewarded ad.");

        // create our request used to load the ad.
        var adRequest = new AdRequest();

        // send the request to load the ad.
        RewardedAd.Load(GET_ID(), adRequest,
            (RewardedAd ad, LoadAdError error) =>
            {
                // if error is not null, the load request failed.
                if (error != null || ad == null)
                {
                    status = ADLoadingStatus.Fail;
                    LogError("Rewarded ad failed to load an ad " +
                                   "with error : " + error);
                    return;
                }

                status = ADLoadingStatus.Success;
                Log("Rewarded ad loaded with response : "
                          + ad.GetResponseInfo());

                rewardedAd = ad;

                RegisterEventHandlers(rewardedAd);
            });
    }

    void RegisterEventHandlers(RewardedAd ad)
    {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Log(String.Format("Rewarded ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        // Raised when an impression is recorded for an ad.
        ad.OnAdImpressionRecorded += () =>
        {
            Log("Rewarded ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {
            Log("Rewarded ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
            Log("Rewarded ad full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            Log("Rewarded ad full screen content closed.");

            status = ADLoadingStatus.NotInitialized;
            rewardedAd.Destroy();
            Load();
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            LogError("Rewarded ad failed to open full screen content " +
                           "with error : " + error);

            status = ADLoadingStatus.NotInitialized;
            rewardedAd.Destroy();
            Load();
        };
    }

    void Show()
    {
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            rewardedAd.Show((Reward reward) =>
            {
                RewardAction.Invoke();
            });
        }
    }
}
