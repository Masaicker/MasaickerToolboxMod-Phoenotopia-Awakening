我[size=6][b]MasaickerToolbox - Jump Enhancement & QoL MOD[/b][/size]


[quote]A BepInEx plugin that improves jump responsiveness and movement feel.
一个 BepInEx 插件，改善跳跃手感和移动体验。[/quote]

[size=5][b]Features / 功能[/b][/size]

[list]
[*][b]Jump Buffer / 跳跃输入缓冲[/b]
Press jump slightly before landing, and the game will automatically jump the moment you touch the ground. No more dropped inputs.  在落地前稍早按下跳跃键，着地瞬间自动执行跳跃，不再吞输入。  Default window: 0.1s / 默认窗口：0.1秒[*][b]Coyote Time / 土狼时间[/b]
Walk off a ledge and you still have a brief moment to jump. Only triggers when passively walking off — not after an intentional jump.  走下平台边缘后仍有极短时间可以跳跃。仅在被动离地时生效，主动跳跃不会触发。  Default window: 0.08s / 默认窗口：0.08秒[*][b]No Sprint Inertia / 取消奔跑惯性[/b]
Release the run button and stop immediately. No more sliding.  松开奔跑键立即停止，不再滑行。  Default: ON / 默认：开启[*][b]Air Turn / 空中转身[/b]
Press the opposite direction while airborne to turn around. Works in normal air, carrying, and hovering states.  空中按反方向键即可转身。适用于普通空中、搬物空中和火箭靴状态。  Default: ON / 默认：开启[*][b]Stamina Cooldown Multiplier / 耐力冷却倍率[/b]
Reduces the delay before stamina starts regenerating after use. Default 0.5x (halved). Preserves the ratio between unlocked and locked cooldown.  缩短耐力消耗后的回复冷却时间。默认0.5倍（减半）。解锁前后的冷却比例保持不变。[*][b]Aerial Attack Startup Skip / 空中攻击前摇跳过[/b]
Skip the startup animation of aerial attacks for faster hit confirmation. Default 0.3 (skips 30% startup). Set to 0 for vanilla behavior.  跳过空中攻击的起手动画，让攻击更快命中。默认0.3（跳过30%前摇）。设为0恢复原版行为。[*][b]Sprint Hold / 按住自动冲刺[/b]
Hold the sprint key to auto re-activate sprint after interruptions (attacks, rolls, landing, etc.). No need to release and re-press. Includes skid-to-turn: pressing the opposite direction during sprint plays the skid animation, then automatically sprints in the new direction.  按住冲刺键即可在被打断后自动重新冲刺（攻击、翻滚、落地等）。无需松开再按。附带滑停转向：冲刺中按反方向播放滑停动画后自动转向继续冲刺。  Default: ON / 默认：开启[*][b]Drop Through Held / 长按穿透平台[/b]
Hold down+jump to continuously fall through drop-through platforms instead of landing on each one. Release to stop.  按住下+跳可连续穿过可下跳的平台，无需每层重新操作。松手即停。  Default: OFF / 默认：关闭[*][b]Hover Grab / 悬浮改键[/b]
Use Grab key instead of Jump to activate and maintain hover (rocket boots) mid-air. Prevents conflict with Jump Buffer — no more accidental hover when buffering a jump near landing.  空中使用抓取键替代跳跃键触发和维持悬浮（火箭靴）。避免与跳跃缓冲冲突——落地前缓冲跳跃不再误触悬浮。  Default: ON / 默认：开启[/list]

[quote]All features are enabled by default and can be toggled or tuned individually in the BepInEx config file.
所有功能默认开启，可在 BepInEx 配置文件中单独开关或调整参数。[/quote]

[size=5][b]Installation / 安装[/b][/size]

[list]
[*]Install [url=https://gh-proxy.net/https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.5/BepInEx_win_x86_5.4.23.5.zip]BepInEx 5.x[/url] for Unity 5.6 / 安装 [url=https://gh-proxy.net/https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.5/BepInEx_win_x86_5.4.23.5.zip]BepInEx 5.x[/url]（适用于 Unity 5.6）
[*]Place [b]MasaickerToolbox.dll[/b] in BepInEx/plugins/ / 将 [b]MasaickerToolbox.dll[/b] 放入 BepInEx/plugins/ 目录
[*]Launch the game. Config file will be generated at BepInEx/config/Mhz.masaickertoolbox.cfg / 启动游戏，配置文件将自动生成于 BepInEx/config/Mhz.masaickertoolbox.cfg
[/list]

[size=5][b]Requirements / 前置[/b][/size]

[list]
[*]BepInEx 5.x
[/list]