using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiningMinigameScript : MonoBehaviour
{
    public MiningGameHandler miningGame;

    IEnumerator interact()
    {
        if (PlayerMovement.player.setCheckBusyWith(this.gameObject))
        {
            miningGame.gameObject.SetActive(true);
            yield return StartCoroutine(miningGame.mainLoop());
            while (miningGame.gameObject.activeSelf)
            {
                yield return null;
            }
            PlayerMovement.player.unsetCheckBusyWith(this.gameObject);
        }

    }
}
