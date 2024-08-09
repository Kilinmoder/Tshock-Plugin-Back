using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace BP
{
    // 标记此插件使用的 API 版本
    [ApiVersion(2, 1)]
    public class BackPlugin : TerrariaPlugin
    {
        // 插件的名称
        public override string Name => "BackPlugin";

        // 插件的作者
        public override string Author => "LanF";

        // 插件的描述
        public override string Description => "允许你回到死亡地点";

        // 插件的版本号
        public override Version Version => new Version(1, 0, 0);

        // 构造函数，用于初始化插件
        // 继承自 TerrariaPlugin 的构造函数，接收 Main 类的实例
        public BackPlugin(Main game) : base(game)
        {
        }

        // 初始化方法，用于注册插件的各种事件和命令
        public override void Initialize()
        {
            // 注册游戏初始化事件
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            // 注册玩家离开服务器事件
            ServerApi.Hooks.ServerLeave.Register(this, ResetPos);
            // 注册玩家加入服务器事件
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnPlayerJoin);
            // 注册玩家死亡事件
            GetDataHandlers.KillMe += OnDead;
        }

        // 游戏初始化时调用的方法，用于添加插件的命令
        private void OnInitialize(EventArgs args)
        {
            // 添加一个名为 "back" 的命令，用于返回死亡地点
            Commands.ChatCommands.Add(new Command("back", Back, "back")
            {
                HelpText = "返回最后一次死亡的位置"
            });
        }

        // 当玩家离开服务器时调用，重置玩家的死亡位置数据
        private void ResetPos(LeaveEventArgs args)
        {
            // 根据玩家的名称或ID查找玩家
            List<TSPlayer> list = TSPlayer.FindByNameOrID(Main.player[args.Who].name);
            if (list.Count > 0)
            {
                // 移除保存的死亡地点数据
                list[0].RemoveData("DeadPoint");
            }
        }

        // 处理玩家输入的 "back" 命令，传送玩家到上次死亡的位置
        private void Back(CommandArgs args)
        {
            // 获取玩家保存的死亡地点数据
            Point data = args.Player.GetData<Point>("DeadPoint");

            // 如果玩家当前仍然处于死亡状态，则不能使用此命令
            if (args.Player.TPlayer.dead)
            {
                args.Player.SendErrorMessage("你还没复活呢，消停会.");
                return;
            }

            // 如果有保存的死亡地点数据，则传送玩家
            if (data != default(Point))
            {
                args.Player.Teleport((float)data.X, (float)data.Y, 1);
                args.Player.SendSuccessMessage($"已传送至死亡地点 [c/ff033e:<{data.X / 16} - {data.Y / 16}>].");
            }
            else
            {
                args.Player.SendErrorMessage("你还未死亡过");
            }
        }

        // 当插件被销毁时调用，用于清理注册的事件，防止内存泄漏
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // 取消注册所有的事件
                ServerApi.Hooks.ServerLeave.Deregister(this, ResetPos);
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnPlayerJoin);
                GetDataHandlers.KillMe -= OnDead;
            }
            base.Dispose(disposing);
        }

        // 当玩家死亡时调用，记录玩家的死亡位置
        private void OnDead(object o, GetDataHandlers.KillMeEventArgs args)
        {
            // 保存玩家死亡时的位置
            args.Player.SetData("DeadPoint", new Point((int)args.Player.X, (int)args.Player.Y));
        }

        // 当玩家加入服务器时调用，初始化玩家的死亡地点数据
        private void OnPlayerJoin(GreetPlayerEventArgs args)
        {
            // 根据玩家的名称或ID查找玩家
            List<TSPlayer> list = TSPlayer.FindByNameOrID(Main.player[args.Who].name);
            if (list.Count > 0)
            {
                // 初始化死亡地点数据为空
                list[0].SetData("DeadPoint", default(Point));
            }
        }
    }
}
