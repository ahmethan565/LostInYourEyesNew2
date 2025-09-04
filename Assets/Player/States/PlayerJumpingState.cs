// PlayerJumpingState.cs
using UnityEngine;

public class PlayerJumpingState : PlayerState
{
    public PlayerJumpingState(FPSPlayerController player, PlayerFSM fsm) : base(player, fsm) { }

    public override void Enter()
    {
        //Debug.Log("Jumping durumuna girildi.");
        player.HandleJumpInput(); // Zıplama gücünü uygula ve takeoff phase'ini başlat
    }

    public override void Execute()
    {
        // Jump phase'ini güncelle
        player.UpdateJumpPhase();
        
        // Yer çekimi her zaman uygulanmalı
        player.ApplyGravity();
        player.MoveCharacter();

        // Landing phase'i tamamlandı mı?
        if (player.currentJumpPhase == FPSPlayerController.JumpPhase.None)
        {
            // Yere indikten sonra hangi duruma geçeceğine karar ver
            Vector3 horizontalMove = player.GetInputMoveVector();
            
            // 1. Çömelerek İniş
            if (Input.GetKey(KeyCode.LeftControl)) // Tuşa basılı tutuyorsa
            {
                fsm.ChangeState(typeof(PlayerCrouchingState));
                return;
            }
            // 2. Koşarak İniş
            else if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                if (horizontalMove.magnitude > 0.01f)
                {
                    fsm.ChangeState(typeof(PlayerRunningState));
                    return;
                }
            }
            // 3. Yürüyerek İniş
            else if (horizontalMove.magnitude > 0.01f)
            {
                fsm.ChangeState(typeof(PlayerWalkingState));
                return;
            }
            // 4. Idle İniş (Hareket yoksa)
            else
            {
                fsm.ChangeState(typeof(PlayerIdleState));
                return;
            }
            player.isSprinting = false;
        }
    }

    public override void Exit()
    {
        //Debug.Log("Jumping durumundan çıkıldı.");
        player.EndJumpPhase(); // Jump phase'ini sıfırla
    }
}