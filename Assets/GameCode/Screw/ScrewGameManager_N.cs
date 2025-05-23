using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Android;
using Unity.VisualScripting;
namespace GameSystem    
{
    public class ScrewGameManager_N:GameManager_N
    {
        Screw_N screw;
        int actionnumber=-1;
        bool click = false;
        bool screwdeclared,nextturnflag = false;
        int endgamecounter,framecount = 0;
        InputHandler inputHandler;
        List<KeyCode> keyCodes = new List<KeyCode>
        {
            KeyCode.Alpha1,
            KeyCode.Alpha2,
            KeyCode.Alpha3,
            KeyCode.Alpha4,
            KeyCode.Q,
            KeyCode.W,
            KeyCode.Return
        };
        GameObject card1, card2;
        public void Start()
        {
            if(!IsServer)return;
            inputHandler = GetComponent<InputHandler>();
            //inputHandler.setupinput(keyCodes);
            screw = new Screw_N(inputHandler);
            currentTurn = firstplayer();
            screw.gamestate = "start";
            StartCoroutine(screw.memorizecard());
        }
        
        public void Update()
        {
            if(!IsServer)return;

            if(screw.gamestate=="end")
            {
                Debug.Log("game ended");
                return;
            }
            if(screwdeclared)
            {
                if(endgamecounter==4)
                {
                    screw.gamestate = "end";
                    screw.scoresheet();
                }
            }
            currentTurn = NextTurn(screw.numberOfPlayers);
            framecount++;
            if(framecount%120==0)
            {
                framecount =0;
                Debug.Log(screw.gamestate); 
            }
            StartCoroutine(screw.navigatedCards(currentTurn));

            if(screw.gamestate=="seeothercard"||screw.gamestate=="choosing1"||screw.gamestate=="choosing2"||screw.gamestate=="lookaround")
            {
                actioncall(currentTurn,actionnumber);
            }
            else if(screw.gamestate=="action"||screw.gamestate == "Swapping1"||screw.gamestate == "picking"||inputHandler.GetKeyDown(KeyCode.Alpha1,currentTurn))
            {
                if(screw.gamestate=="normal")
                {
                    screw.gamestate = "picking";
                }
                if(screw.pickedcard==null)
                {

                    screw.pickedcard = screw.PickCard(currentTurn);
                }
                else
                {
                    if(framecount%120==0)
                    Debug.Log("you already picked a card");
                }
                if(!click)
                StartCoroutine(WaitAndAllowClickAgain(0.5f));
                if(click)
                {
                    pickingcard();
                    if(screw.pickedcard==null)
                    click = false;
                }
            }
            else if(screw.gamestate=="Swapping2"||inputHandler.GetKeyDown(KeyCode.Alpha2,currentTurn))
            {
                screw.gamestate = "Swapping2";
                swapping();
            }
            else if(screw.gamestate=="basra"||screw.gamestate=="matching"||inputHandler.GetKeyDown(KeyCode.Alpha3,currentTurn))
            {
                if(screw.gamestate!="basra")
                {
                    screw.gamestate="matching";
                }
                matching();
            }
            else if(inputHandler.GetKeyDown(KeyCode.Alpha4,currentTurn))
            {
                screwdeclared = true;
                endgamecounter++;
            }
     
        }
        public void pickingcard()
        {
            if(screw.gamestate == "Swapping1"||inputHandler.GetKeyDown(KeyCode.Alpha1,currentTurn))
            {
                screw.gamestate = "Swapping1";
                if(inputHandler.GetKeyDown(KeyCode.Return,currentTurn))
                {
                    Debug.Log("swaped");
                    screw.swapwpickedcard(currentTurn);
                }
            }
            if(screw.gamestate=="action"||inputHandler.GetKeyDown(KeyCode.Alpha2,currentTurn))
            {
                if(screw.gamestate!="action")
                {
                    screw.throwcard(screw.pickedcard);
                    actionnumber = isaction();
                }
                if(screw.gamestate=="action"||actionnumber!=-1)
                {
                    screw.gamestate = "action";
                    actioncall(currentTurn,actionnumber);
                }
                else
                {
                screw.pickedcard = null;
                screw.gamestate = "normal";
                }
            }
        }
        public int isaction()
        {
            for(int i =0;i<screw.specialcardsnames.Length;i++)
            {
                if(screw.centralPile.Last.Value.name.Split(" ")[0]==screw.specialcardsnames[i])
                {
                    Debug.Log("action card name: " + screw.centralPile.Last.Value.name);
                    return i;
                }
            }
            Debug.Log("not a action card : "+screw.centralPile.Last.Value.name);
            return -1;
        }
        private IEnumerator WaitAndAllowClickAgain(float waitTime)
        {
            Debug.Log("Waiting for " + waitTime + " seconds...");
            yield return new WaitForSeconds(waitTime);
            click = true;
            Debug.Log("Ready for another click!");
        }
        public void actioncall(int player, int number)
        {
            if(number==0 || number ==1)
            {
                
                StartCoroutine(screw.seeurcard(player));
            }
            else if (number ==2|| number==3)
            {
                screw.gamestate = "seeothercard";
                StartCoroutine(screw.seeotherscard(player));
                StartCoroutine(screw.navigateplayers(player));
            }
            else if (number ==4)
            {
                screw.gamestate = "basra";
            }
            else if (number ==5)
            {
                screw.gamestate = "lookaround";
                StartCoroutine(screw.lookaround(player));
            }
            else 
            {
                swapwplayer();
            }
        }
        public void swapping()
        {
            if(inputHandler.GetKeyDown(KeyCode.Return,currentTurn))
            {
                screw.swapwdiscardpile(currentTurn);

            }
        }
        public void matching()
        {
            if(screw.gamestate=="basra")
            {
                Debug.Log("basra");
            }
            if(inputHandler.GetKeyDown(KeyCode.Return,currentTurn))
            {
                screw.match(currentTurn);
                UpdatecardPositions();
            }
        }
        public override int NextTurn(int noOfPlayers)
        {
            if(nextturnflag||screw.gamestate!="normal")
            {
                nextturnflag = true;
                if(screw.gamestate=="normal")
                {
                    screw.hands[currentTurn][screw.navigatedCardindex].transform.localScale = screw.oldscale;
                    screw.hands[currentTurn][screw.navigatedCardindex].GetComponent<Renderer>().material.color = Color.white;
                    Debug.Log("current turn: " + currentTurn);
                    nextturnflag=false;
                    if(screwdeclared)
                    {
                        endgamecounter++;
                    }
                    return (currentTurn + 1) % noOfPlayers;
                }
            }
            return currentTurn;
        }
        public void swapwplayer()
        {
            if(screw.gamestate=="choosing2")
            {
                StartCoroutine(screw.navigateplayers(currentTurn));
            }
            if(screw.gamestate!="choosing2")
            {
                screw.gamestate = "choosing1";
            }
            if(screw.gamestate=="choosing1"&&inputHandler.GetKeyDown(KeyCode.Return,currentTurn))
            {
                card1 = screw.hands[currentTurn][screw.navigatedCardindex];
                
                screw.gamestate = "choosing2";
            }
            else if(screw.gamestate=="choosing2"&&inputHandler.GetKeyDown(KeyCode.Return,currentTurn))
            {
                card2 = screw.hands[currentTurn][screw.navigatedCardindex];
                StopCoroutine(screw.navigateplayers(currentTurn));
                screw.hands[currentTurn][screw.navigatedCardindex]=screw.hands[screw.naviagedplayerindex][screw.navigatedplayercard];
                screw.hands[screw.naviagedplayerindex][screw.navigatedplayercard] = card1;
                screw.hands[currentTurn][screw.navigatedCardindex].transform.localPosition =screw.handspostions[currentTurn][screw.navigatedCardindex];
                screw.hands[screw.naviagedplayerindex][screw.navigatedplayercard].transform.localPosition =screw.handspostions[screw.naviagedplayerindex][screw.navigatedplayercard];
                screw.hands[currentTurn][screw.navigatedCardindex].transform.localRotation = Quaternion.Euler(screw.playerrotations[currentTurn]);  
                screw.hands[screw.naviagedplayerindex][screw.navigatedplayercard].transform.localRotation = Quaternion.Euler(screw.playerrotations[screw.naviagedplayerindex]);
                screw.gamestate = "normal";
                screw.pickedcard = null;   
            }
        }
        public void UpdatecardPositions()
        {
            for(int i = 0; i < screw.hands.Count; i++)
            {
                for(int j = 0; j < screw.hands[i].Count; j++)
                {
                    screw.hands[i][j].transform.localPosition = screw.handspostions[i][j];
                }
            }
        }
        
    }
}
