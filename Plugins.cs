using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace MasaickerToolbox
{
    [BepInPlugin("Mhz.masaickertoolbox", "MasaickerToolbox", "1.0.7")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        public static ConfigEntry<bool> NoInertiaEnabled;
        public static ConfigEntry<bool> JumpBufferEnabled;
        public static ConfigEntry<float> JumpBufferWindow;
        public static ConfigEntry<bool> CoyoteTimeEnabled;
        public static ConfigEntry<float> CoyoteTimeWindow;
        public static ConfigEntry<bool> DebugLog;
        public static ConfigEntry<bool> AirTurnEnabled;
        public static ConfigEntry<float> StaminaCooldownMult;
        public static ConfigEntry<float> AerialAtkSkipFrames;
        public static ConfigEntry<bool> SprintHoldEnabled;
        public static ConfigEntry<bool> DropThroughHeldEnabled;
        public static ConfigEntry<bool> HoverGrabEnabled;
        public static ConfigEntry<bool> LeapBreakEnabled;

        private void Awake()
        {
            Log = Logger;

            NoInertiaEnabled = Config.Bind(
                "General",
                "NoInertia",
                true,
                "No Inertia - Stop immediately when sprint key released - 取消奔跑惯性（松开按键立即停止）");

            JumpBufferEnabled = Config.Bind(
                "Jump",
                "JumpBuffer",
                true,
                "Jump Buffer - Queue jump input before landing, auto-jump on touchdown - 跳跃输入缓冲（着地前按跳跃，着地瞬间自动跳）");

            JumpBufferWindow = Config.Bind(
                "Jump",
                "JumpBufferWindow",
                0.1f,
                "Jump Buffer Window (seconds) - 跳跃缓冲窗口（秒）");

            HoverGrabEnabled = Config.Bind(
                "Jump",
                "HoverGrab",
                true,
                "Hover Grab - Use Grab key instead of Jump to activate hover (rocket boots) mid-air, preventing conflict with Jump Buffer - 抓取键悬浮（空中用抓取键代替跳跃键激活火箭靴悬浮，避免与跳跃缓冲冲突）");

            CoyoteTimeEnabled = Config.Bind(
                "Jump",
                "CoyoteTime",
                true,
                "Coyote Time - Allow jumping briefly after walking off a ledge - 土狼时间（离开平台后短暂仍可跳跃）");

            CoyoteTimeWindow = Config.Bind(
                "Jump",
                "CoyoteTimeWindow",
                0.08f,
                "Coyote Time Window (seconds) - 土狼时间窗口（秒）");

            DebugLog = Config.Bind(
                "Debug",
                "DebugLog",
                false,
                "Enable Debug Log - 启用调试日志输出");

            AirTurnEnabled = Config.Bind(
                "General",
                "AirTurn",
                true,
                "Air Turn - Allow flipping direction mid-air with directional input - 空中转身（按反方向键时角色翻转朝向）");

            StaminaCooldownMult = Config.Bind(
                "Stamina",
                "StaminaCooldownMultiplier",
                0.5f,
                "Stamina recovery cooldown multiplier (0.0 = no cooldown, 1.0 = vanilla) - 耐力回复冷却倍率（0.0=无冷却，1.0=原版）");

            AerialAtkSkipFrames = Config.Bind(
                "Combat",
                "AerialAttackSkipRatio",
                0.3f,
                "Skip aerial attack startup animation (0.0 = vanilla, 0.3 = skip 30% startup) - 跳过空中攻击前摇动画比例（0.0=原版，0.3=跳过30%前摇）");

            SprintHoldEnabled = Config.Bind(
                "General",
                "SprintHold",
                true,
                "Sprint Hold - Auto re-activate sprint while holding sprint key - 按住冲刺键自动重激活冲刺（无需松开再按）");

            DropThroughHeldEnabled = Config.Bind(
                "General",
                "DropThroughHeld",
                false,
                "Drop Through Held - Hold down+jump to continuously fall through drop-through platforms - 长按穿透平台（按住下+跳连续穿过可下跳的平台）");

            LeapBreakEnabled = Config.Bind(
                "General",
                "LeapBreak",
                false,
                "Leap Break - Press opposite direction mid-air to exit sprint jump - 冲刺跳脱离（空中按反方向脱离冲刺跳状态）");

            var harmony = new Harmony("Mhz.masaickertoolbox");
            harmony.PatchAll();
            Logger.LogInfo("MasaickerToolbox loaded");
        }
    }

    // 跳跃增强共享状态
    public static class JumpState
    {
        public static float lastJumpPressTime = -1f;
        public static float lastGroundedTime = -1f;
        public static bool leftGroundByJump = false;
        public static bool leftGroundFromSprint = false;

        // 冲刺跳公共逻辑：体力消耗、粒子、音效、状态切换
        public static void DoSprintJump(GaleLogicOne g)
        {
            g._UseUpStamina(10.5f);
            g._EmitDustOrWaterParticle(0f, -0.75f, DIRECTION.DOWN, 1);
            g._EmitDustOrWaterParticle(0f, -0.75f, DIRECTION.LEFT, 0);
            g._EmitDustOrWaterParticle(0f, -0.75f, DIRECTION.RIGHT, 0);
            PT2.sound_g.PlayGlobalCommonSfx(18, 1f, GL.M_RandomPitch(), 1);
            PT2.sound_g.PlayCommonSfx(166, g._transform.position, 1f, 0f, GL.M_RandomPitch(1f, 0.05f));
            g._GoToState(GaleLogicOne.GALE_STATE.IN_AIR_LEAPING_STATE);
        }
    }

    // 记录跳跃按键时间（ControlAdapter.Update 没有 new Action，patch 安全）
    [HarmonyPatch(typeof(ControlAdapter), nameof(ControlAdapter.Update))]
    public class JumpInputTracker
    {
        static void Postfix(ControlAdapter __instance)
        {
            if (Plugin.JumpBufferEnabled.Value && __instance.JUMP_PRESSED)
            {
                JumpState.lastJumpPressTime = Time.time;
                if (Plugin.DebugLog.Value)
                    Plugin.Log.LogInfo("[JumpBuffer] Jump pressed recorded at " + Time.time);
            }
        }
    }

    // 在地面状态中记录最后在地面的时间，并追踪离地方式
    [HarmonyPatch(typeof(GaleLogicOne), nameof(GaleLogicOne._STATE_Default))]
    public class GroundTracker_Default
    {
        static void Prefix(GaleLogicOne __instance)
        {
            JumpState.lastGroundedTime = Time.time;
        }

        static void Postfix(GaleLogicOne __instance)
        {
            // _STATE_Default 末尾检查离地：如果之前在地面，现在不在了，说明是走下平台
            if (!__instance._mover2.collision_info.below && __instance._mover2.DistanceToGround() > 0.5f)
            {
                JumpState.leftGroundByJump = false;
                JumpState.leftGroundFromSprint = false;
                if (Plugin.DebugLog.Value)
                    Plugin.Log.LogInfo("[Coyote] Left ground passively (Default)");
            }
        }
    }

    [HarmonyPatch(typeof(GaleLogicOne), nameof(GaleLogicOne._STATE_Sprinting))]
    public class GroundTracker_Sprinting
    {
        static void Prefix(GaleLogicOne __instance)
        {
            JumpState.lastGroundedTime = Time.time;
        }

        static void Postfix(GaleLogicOne __instance)
        {
            // 无惯性
            if (Plugin.NoInertiaEnabled.Value && !__instance._control.SPRINT_HELD)
            {
                if (Mathf.Abs(__instance.velocity.x) > 0.01f)
                {
                    __instance.velocity.x = 0f;
                    __instance.momentum.x = 0f;
                }
            }

            // 土狼时间：追踪被动离地（冲刺状态）
            if (!__instance._mover2.collision_info.below && __instance._mover2.DistanceToGround() > 0.5f)
            {
                JumpState.leftGroundByJump = false;
                JumpState.leftGroundFromSprint = true;
                if (Plugin.DebugLog.Value)
                    Plugin.Log.LogInfo("[Coyote] Left ground passively (Sprinting)");
            }
        }
    }

    // 土狼时间 + 跳跃缓冲：都在 _STATE_InAir 中处理（0 个 new Action，patch 安全）
    [HarmonyPatch(typeof(GaleLogicOne), nameof(GaleLogicOne._STATE_InAir))]
    public class JumpEnhance_InAir
    {
        static bool Prefix(GaleLogicOne __instance)
        {
            // 土狼时间：刚离地且非主动跳跃，窗口内按跳跃则执行跳跃
            if (Plugin.CoyoteTimeEnabled.Value
                && !JumpState.leftGroundByJump
                && __instance._control.JUMP_PRESSED)
            {
                float timeSinceGrounded = Time.time - JumpState.lastGroundedTime;
                if (timeSinceGrounded <= Plugin.CoyoteTimeWindow.Value)
                {
                    __instance.velocity.y = __instance.jump_velocity;
                    JumpState.leftGroundByJump = true;
                    JumpState.lastJumpPressTime = -1f;

                    if (JumpState.leftGroundFromSprint)
                    {
                        JumpState.DoSprintJump(__instance);
                        JumpState.leftGroundFromSprint = false;
                        if (Plugin.DebugLog.Value)
                            Plugin.Log.LogInfo("[Coyote] Sprint jump triggered! timeSinceGround=" + timeSinceGrounded.ToString("F3"));
                        return false;
                    }
                    else
                    {
                        // 普通土狼跳
                        PT2.sound_g.PlayGlobalCommonSfx(18, 1f, GL.M_RandomPitch(1.4f, 0.05f), 1);
                        if (Plugin.DebugLog.Value)
                            Plugin.Log.LogInfo("[Coyote] Normal jump triggered! timeSinceGround=" + timeSinceGrounded.ToString("F3"));
                        return true;
                    }
                }
            }

            // 跳跃缓冲：着地瞬间检查缓冲窗口
            if (Plugin.JumpBufferEnabled.Value
                && __instance._wait_time > 0.05f
                && __instance._mover2.collision_info.below)
            {
                float timeSinceJump = Time.time - JumpState.lastJumpPressTime;
                if (timeSinceJump <= Plugin.JumpBufferWindow.Value)
                {
                    JumpState.lastJumpPressTime = -1f;
                    __instance.velocity.y = __instance.jump_velocity;
                    JumpState.leftGroundByJump = true;

                    if (Plugin.SprintHoldEnabled.Value && __instance._control.SPRINT_HELD)
                    {
                        JumpState.DoSprintJump(__instance);
                        if (Plugin.DebugLog.Value)
                            Plugin.Log.LogInfo("[JumpBuffer] Sprint jump triggered in InAir! timeSinceJump=" + timeSinceJump.ToString("F3"));
                    }
                    else
                    {
                        PT2.sound_g.PlayGlobalCommonSfx(18, 1f, GL.M_RandomPitch(1.4f, 0.05f), 1);
                        __instance._GoToState(GaleLogicOne.GALE_STATE.IN_AIR);
                        if (Plugin.DebugLog.Value)
                            Plugin.Log.LogInfo("[JumpBuffer] Triggered in InAir! timeSinceJump=" + timeSinceJump.ToString("F3"));
                    }
                    return false;
                }
            }

            // 着地时重置标记
            if (__instance._wait_time > 0.05f && __instance._mover2.collision_info.below)
                JumpState.leftGroundByJump = false;

            return true;
        }

        static void Postfix(GaleLogicOne __instance)
        {
            // 空中转身：按方向键时翻转朝向
            if (Plugin.AirTurnEnabled.Value && Mathf.Abs(__instance._control.LEFT_RIGHT_AXIS) > 0.5f)
            {
                float facing = __instance._control.LEFT_RIGHT_AXIS > 0f ? 1f : -1f;
                if (__instance._transform.localScale.x != facing)
                    __instance._transform.localScale = new Vector3(facing, 1f, 1f);
            }

            // 持续穿透单向板：按住下+跳时持续刷新ignore计时器
            if (Plugin.DropThroughHeldEnabled.Value
                && __instance.velocity.y <= 0f
                && (__instance._control.UP_DOWN_AXIS <= -0.95f || __instance._control.CROUCH_HELD)
                && __instance._control.JUMP_HELD)
            {
                __instance._mover2._ignore_owp_time = 0.1f;
            }
        }
    }

    [HarmonyPatch(typeof(GaleLogicOne), nameof(GaleLogicOne._STATE_Leap))]
    public class JumpBufferLeap
    {
        static bool Prefix(GaleLogicOne __instance)
        {
            if (!Plugin.JumpBufferEnabled.Value)
                return true;

            bool landed = (__instance._wait_time > 0.05f && __instance._mover2.collision_info.below)
                || (__instance.velocity.y == 0f && __instance._wait_time_alt > 0.25f);

            if (landed)
            {
                float timeSinceJump = Time.time - JumpState.lastJumpPressTime;
                if (timeSinceJump <= Plugin.JumpBufferWindow.Value)
                {
                    JumpState.lastJumpPressTime = -1f;
                    __instance.velocity.y = __instance.jump_velocity;
                    JumpState.leftGroundByJump = true;

                    if (Plugin.SprintHoldEnabled.Value && __instance._control.SPRINT_HELD)
                    {
                        JumpState.DoSprintJump(__instance);
                        if (Plugin.DebugLog.Value)
                            Plugin.Log.LogInfo("[JumpBuffer] Sprint jump triggered in Leap! timeSinceJump=" + timeSinceJump.ToString("F3"));
                    }
                    else
                    {
                        PT2.sound_g.PlayGlobalCommonSfx(18, 1f, GL.M_RandomPitch(1.4f, 0.05f), 1);
                        __instance._GoToState(GaleLogicOne.GALE_STATE.IN_AIR);
                        if (Plugin.DebugLog.Value)
                            Plugin.Log.LogInfo("[JumpBuffer] Triggered in Leap! timeSinceJump=" + timeSinceJump.ToString("F3"));
                    }
                    return false;
                }
                JumpState.leftGroundByJump = false;
            }
            return true;
        }

        static void Postfix(GaleLogicOne __instance)
        {
            // 冲刺跳脱离：反方向输入立即切换为普通空中状态
            if (Plugin.LeapBreakEnabled.Value)
            {
                float facing = __instance._transform.localScale.x;
                float input = __instance._control.LEFT_RIGHT_AXIS;

                if (facing * input < -0.5f)
                {
                    __instance._GoToState(GaleLogicOne.GALE_STATE.IN_AIR);
                }
            }

            // 长按穿透平台
            if (Plugin.DropThroughHeldEnabled.Value
                && __instance.velocity.y <= 0f
                && (__instance._control.UP_DOWN_AXIS <= -0.95f || __instance._control.CROUCH_HELD)
                && __instance._control.JUMP_HELD)
            {
                __instance._mover2._ignore_owp_time = 0.1f;
            }
        }
    }

    [HarmonyPatch(typeof(GaleLogicOne), nameof(GaleLogicOne._STATE_InAirCarry))]
    public class JumpBufferInAirCarry
    {
        static bool Prefix(GaleLogicOne __instance)
        {
            if (!Plugin.JumpBufferEnabled.Value)
                return true;

            if (__instance._wait_time > 0.05f && __instance._mover2.collision_info.below)
            {
                float timeSinceJump = Time.time - JumpState.lastJumpPressTime;
                if (timeSinceJump <= Plugin.JumpBufferWindow.Value)
                {
                    JumpState.lastJumpPressTime = -1f;
                    __instance.velocity.y = __instance.jump_velocity;
                    PT2.sound_g.PlayGlobalCommonSfx(18, 1f, GL.M_RandomPitch(1.4f, 0.05f), 1);
                    __instance._GoToState(GaleLogicOne.GALE_STATE.IN_AIR_CARRY);
                    JumpState.leftGroundByJump = true;
                    if (Plugin.DebugLog.Value)
                        Plugin.Log.LogInfo("[JumpBuffer] Triggered in InAirCarry! timeSinceJump=" + timeSinceJump.ToString("F3"));
                    return false;
                }
                JumpState.leftGroundByJump = false;
            }
            return true;
        }

        static void Postfix(GaleLogicOne __instance)
        {
            if (Plugin.AirTurnEnabled.Value && Mathf.Abs(__instance._control.LEFT_RIGHT_AXIS) > 0.5f)
            {
                float facing = __instance._control.LEFT_RIGHT_AXIS > 0f ? 1f : -1f;
                if (__instance._transform.localScale.x != facing)
                    __instance._transform.localScale = new Vector3(facing, 1f, 1f);
            }

            if (Plugin.DropThroughHeldEnabled.Value
                && __instance.velocity.y <= 0f
                && (__instance._control.UP_DOWN_AXIS <= -0.95f || __instance._control.CROUCH_HELD)
                && __instance._control.JUMP_HELD)
            {
                __instance._mover2._ignore_owp_time = 0.1f;
            }
        }
    }

    [HarmonyPatch(typeof(GaleLogicOne), nameof(GaleLogicOne._STATE_Hovering))]
    public class HoveringPatch
    {
        static bool _savedJumpHeld;

        static void Prefix(GaleLogicOne __instance)
        {
            // 用 Grab 维持悬浮：按住 Grab 时临时伪装 JUMP_HELD
            _savedJumpHeld = __instance._control.JUMP_HELD;
            if (Plugin.HoverGrabEnabled.Value && __instance._control.GRAB_HELD)
                __instance._control.JUMP_HELD = true;
        }

        static void Postfix(GaleLogicOne __instance)
        {
            if (Plugin.HoverGrabEnabled.Value)
                __instance._control.JUMP_HELD = _savedJumpHeld;

            if (Plugin.AirTurnEnabled.Value && Mathf.Abs(__instance._control.LEFT_RIGHT_AXIS) > 0.5f)
            {
                float facing = __instance._control.LEFT_RIGHT_AXIS > 0f ? 1f : -1f;
                if (__instance._transform.localScale.x != facing)
                    __instance._transform.localScale = new Vector3(facing, 1f, 1f);
            }
        }
    }

    [HarmonyPatch(typeof(GaleLogicOne), nameof(GaleLogicOne._UseUpStamina))]
    public class StaminaCooldownPatch
    {
        static void Postfix(GaleLogicOne __instance, bool skip_cost)
        {
            if (!skip_cost)
                __instance.stamina_stun *= Plugin.StaminaCooldownMult.Value;
        }
    }

    // 空中攻击跳过前摇：用高速播放模拟跳帧，到达指定比例后恢复原速
    [HarmonyPatch(typeof(GaleLogicOne), nameof(GaleLogicOne._STATE_AerialAttack))]
    public class AerialAttackSkip
    {
        static void Postfix(GaleLogicOne __instance)
        {
            float skip = Plugin.AerialAtkSkipFrames.Value;
            if (skip <= 0f || __instance._anim.GetInteger(__instance.anim_state) != 19) return;

            var stateInfo = __instance._anim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.normalizedTime < skip)
                __instance._anim.speed = 10f;
            else if (__instance._anim.speed > 5f)
                __instance._anim.speed = 1.35f;
        }
    }

    [HarmonyPatch(typeof(GaleLogicOne), nameof(GaleLogicOne._STATE_LeapAerialAttack))]
    public class LeapAerialAttackSkip
    {
        static void Postfix(GaleLogicOne __instance)
        {
            float skip = Plugin.AerialAtkSkipFrames.Value;
            if (skip <= 0f || __instance._anim.GetInteger(__instance.anim_state) != 19) return;

            var stateInfo = __instance._anim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.normalizedTime < skip)
                __instance._anim.speed = 10f;
            else if (__instance._anim.speed > 5f)
                __instance._anim.speed = 1.35f;
        }
    }

    // Skid 转向：skid 结束时翻转朝向，配合 SprintHold 实现冲刺转向
    [HarmonyPatch(typeof(GaleLogicOne), nameof(GaleLogicOne._STATE_Skidding))]
    class SkidTurnPatch
    {
        static void Postfix(GaleLogicOne __instance)
        {
            if (!Plugin.SprintHoldEnabled.Value || !__instance._control.SPRINT_HELD) return;

            // skid 即将结束（速度衰减到 < 1 时原版会进 DEFAULT），提前翻转朝向
            if (Mathf.Abs(__instance.velocity.x) < 1f)
                __instance._transform.localScale = new Vector3(-__instance._transform.localScale.x, 1f, 1f);
        }
    }

    // Sprint Hold: 按住冲刺键时自动重激活冲刺
    [HarmonyPatch(typeof(GaleLogicOne), "Update")]
    class SprintHoldPatch
    {
        static void Prefix(GaleLogicOne __instance)
        {
            if (!Plugin.SprintHoldEnabled.Value) return;
            if (!__instance._control.SPRINT_HELD || __instance._control.SPRINT_PRESSED) return;

            if (__instance.StateFn == __instance._STATE_Default)
            {
                // 同帧跳跃+冲刺：直接执行冲刺跳，避免被 Update 中普通跳抢先（JUMP_PRESSED 1678行 先于 SPRINT_PRESSED 1955行）
                if (__instance._control.JUMP_PRESSED && __instance._EnoughStamina())
                {
                    __instance.velocity.y = __instance.jump_velocity;
                    JumpState.DoSprintJump(__instance);
                    JumpState.leftGroundByJump = true;
                    __instance._control.JUMP_PRESSED = false;
                    return;
                }

                __instance._control.SPRINT_PRESSED = true;
                __instance._control.num_frames_since_last_SPRINT_PRESSED = 0;
            }
        }
    }

    // 悬浮改用 Grab 键触发，解决与跳跃缓冲的冲突
    static class HoverGrabState
    {
        public static bool grabRocketPress;
        public static float grabRocketBufferTime;
        public static bool savedHoverFlag;
    }

    [HarmonyPatch(typeof(GaleLogicOne), "Update")]
    class HoverGrabPatch
    {
        static void Prefix(GaleLogicOne __instance)
        {
            if (!Plugin.HoverGrabEnabled.Value)
                return;

            // Grab 键触发悬浮准备
            if (__instance._control.GRAB_PRESSED
                && __instance.DEBUG_CAN_HOVER
                && __instance._AttemptFlyOutOfAir())
            {
                HoverGrabState.grabRocketPress = true;
                HoverGrabState.grabRocketBufferTime = 0f;
            }

            // 搬运空中按 Grab 直接进入悬浮搬运（原版1850行的替代）
            if (__instance._control.GRAB_PRESSED
                && __instance.DEBUG_CAN_HOVER
                && __instance.StateFn == __instance._STATE_InAirCarry
                && __instance._EnoughStamina())
            {
                __instance._GoToState(GaleLogicOne.GALE_STATE.HOVERING_CARRY);
            }

            // Grab 持续按住：累积计时器，满足条件进入悬浮
            if (HoverGrabState.grabRocketPress && __instance._control.GRAB_HELD)
            {
                HoverGrabState.grabRocketBufferTime += Time.deltaTime;
                if (HoverGrabState.grabRocketBufferTime > 0.05f
                    && __instance.DEBUG_CAN_HOVER
                    && __instance._AttemptFlyOutOfAir()
                    && __instance._EnoughStamina())
                {
                    __instance._GoToState(GaleLogicOne.GALE_STATE.HOVERING);
                    HoverGrabState.grabRocketPress = false;
                }
            }
            else
            {
                HoverGrabState.grabRocketPress = false;
            }

            // 临时禁用原版跳跃触发悬浮 + 清除原版残留状态
            HoverGrabState.savedHoverFlag = __instance.DEBUG_CAN_HOVER;
            __instance.DEBUG_CAN_HOVER = false;
            __instance._discrete_rocket_press = false;
            if (Plugin.DebugLog.Value && __instance._control.JUMP_PRESSED)
                Plugin.Log.LogInfo("[HoverGrab] Prefix executed: savedFlag=" + HoverGrabState.savedHoverFlag + " state=" + __instance.StateFn.Method.Name);
        }

        static void Postfix(GaleLogicOne __instance)
        {
            if (!Plugin.HoverGrabEnabled.Value)
                return;

            __instance.DEBUG_CAN_HOVER = HoverGrabState.savedHoverFlag;
        }
    }

    // TODO: 测试用，测完删除 - 强制解锁火箭靴
    // [HarmonyPatch(typeof(GaleLogicOne), nameof(GaleLogicOne.EnableAbilitiesBasedOnInv))]
    // class DebugUnlockHover
    // {
    //     static void Prefix(ref int[] status_inv)
    //     {
    //         status_inv[32] = 34;
    //     }
    // }
}
