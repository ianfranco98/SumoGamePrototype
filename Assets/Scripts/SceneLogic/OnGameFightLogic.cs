﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Encargado de la lógica "InGame". Desde aca se controla el estado de los jugadores según
// lo que ocurra/hagan.

public class OnGameFightLogic : MonoBehaviour
{
    public delegate void Signal();
    Signal sendStateChanged;
    PlayersEventHandler eventHandler;

    public enum Player
    {
        NONE,
        ONE,
        TWO,
        BOTH
    }

    public enum State
    {
        INVALID,
        STARTING,
        ON_GAME,
        GRAB_BATTLE,
        ROUND_ENDED,
        FIGHT_ENDED
    }

    State currentState = State.INVALID;
    State initialState = State.STARTING;
    Player currentWinner = Player.NONE;
    int initialScore = 1;
    int playerOneScore, playerTwoScore;

    void Awake() => enabled = false;

    public void Setup(Transform playerOne, Transform playerTwo, Signal sendStateChanged, bool isMultiplayer = false)
    {

        this.sendStateChanged = sendStateChanged;

        playerOneScore = initialScore;
        playerTwoScore = initialScore;

        eventHandler = isMultiplayer ? gameObject.AddComponent(typeof(NetworkedPlayersEventHandler)) as NetworkedPlayersEventHandler : gameObject.AddComponent(typeof(PlayersEventHandler)) as PlayersEventHandler;
        eventHandler.Setup(playerOne, playerTwo);

        enabled = true;
        ChangeState(initialState);
    }

    void ChangeState(State newState)
    {
        if (newState != currentState)
        {
            switch (newState)
            {
                case State.STARTING:
                    StartCoroutine(SetPlayersAndGo());
                    break;
                case State.ON_GAME:
                    currentWinner = Player.NONE;
                    break;
                case State.ROUND_ENDED:
                    eventHandler.TellPlayersWhoWin(currentWinner);
                    StartCoroutine(SetPlayersAndGo());
                    break;
                case State.FIGHT_ENDED:
                    break;
                case State.GRAB_BATTLE:
                    StartCoroutine(FinalizeGrabBattle());
                    break;

            }
            currentState = newState;
            sendStateChanged();
        }
        else
        {
            Debug.Log(gameObject.name + ": Can't change game state when the new state is equal to the current state");
        }

    }

    void Update()
    {
        switch (currentState)
        {
            case State.ON_GAME:
                if (eventHandler.PlayersAreColliding())
                {
                    if (eventHandler.IsAttackingP1() && !eventHandler.IsAttackingP2()) eventHandler.PushPlayer2();
                    if (eventHandler.IsAttackingP2() && !eventHandler.IsAttackingP1()) eventHandler.PushPlayer1();
                }


                if (eventHandler.BothPlayersAttackAndCollide())
                {

                    switch (eventHandler.WhoHasGotStrongestPush())
                    {
                        case 0:// Si hubo empate
                            ChangeState(State.GRAB_BATTLE);
                            break;
                        case 1:
                            eventHandler.PushPlayer2();
                            break;
                        case 2:
                            eventHandler.PushPlayer1();
                            break;
                    }
                }

                bool playerOneLose = eventHandler.OutOfArenaP1();
                bool playerTwoLose = eventHandler.OutOfArenaP2();

                bool outOfArena = playerOneLose || playerTwoLose;

                if (outOfArena)
                {

                    //Checkeamos quien ganó.

                    if (playerOneLose && playerTwoLose)
                    {
                        currentWinner = Player.BOTH;
                    }
                    else
                    {
                        currentWinner = playerOneLose ? Player.TWO : Player.ONE;

                        switch (currentWinner)
                        {
                            case Player.ONE:
                                playerOneScore--;
                                break;
                            case Player.TWO:
                                playerTwoScore--;
                                break;
                        }
                    }

                    //

                    if (playerOneScore == 0 || playerTwoScore == 0) ChangeState(State.FIGHT_ENDED);
                    else ChangeState(State.ROUND_ENDED);

                }
                else if (eventHandler.BothPlayersAttackAndCollide())
                {
                    ChangeState(State.GRAB_BATTLE);
                }

                break;

            case State.GRAB_BATTLE:

                break;

            case State.ROUND_ENDED:

                break;

        }
    }

    public void Restart()
    {
        playerOneScore = initialScore;
        playerTwoScore = initialScore;

        ChangeState(State.STARTING);
    }

    int DecideWhoWinGrabBattle()
    {
        int pushCountP1 = eventHandler.GetPushCountP1();
        int pushCountP2 = eventHandler.GetPushCountP2();
        int whoWin = pushCountP1 - pushCountP2;
        int result = 0;
        if (whoWin > 0)
        {
            result = 1;
        }
        else if (whoWin < 0)
        {
            result = 2;
        }

        return result;
    }

    IEnumerator FinalizeGrabBattle()
    {
        int winner = 0;
        yield return new WaitForSeconds(3);
        winner = DecideWhoWinGrabBattle();

        

        yield return new WaitForSeconds(1);

        if(winner == 1)
        {
            eventHandler.PushPlayer2();
        } else if (winner == 2)
        {
            eventHandler.PushPlayer1();
        } else {
            eventHandler.PushPlayer1();
            eventHandler.PushPlayer2();
        }

        ChangeState(State.ON_GAME);
    }

    IEnumerator SetPlayersAndGo()
    {
        yield return new WaitForSeconds(1.5f);
        eventHandler.PositionPlayersToOrigin();
        yield return new WaitForSeconds(1.5f);
        ChangeState(State.ON_GAME);
    }

    public Transform GetWinnerTransform()
    {
        Transform t = null;

        switch (currentWinner)
        {
            case Player.ONE:
                t = eventHandler.GetP1Transform();
                break;
            case Player.TWO:
                t = eventHandler.GetP2Transform();
                break;
        }
        return t;
    }

    public State GetCurrentState() => currentState;

}
