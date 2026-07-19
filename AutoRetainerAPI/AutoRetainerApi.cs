using AutoRetainerAPI.Configuration;
using ECommons.DalamudServices;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static AutoRetainerAPI.Delegates;

namespace AutoRetainerAPI;

public partial class AutoRetainerApi : IDisposable
{
    public readonly AutoRetainerConfig Config = new();
    /// <summary>
    /// 即将委托雇员执行探险委托时触发的事件。
    /// </summary>
    public event OnSendRetainerToVentureDelegate OnSendRetainerToVenture;

    /// <summary>
    /// 雇员处理完毕、准备接收额外后处理任务时触发的事件。
    /// </summary>
    public event OnRetainerPostprocessTaskDelegate OnRetainerPostprocessStep;

    /// <summary>
    /// 你的插件应执行额外后处理任务时触发的事件。必须将游戏界面恢复到接管前的状态。
    /// </summary>
    public event OnRetainerReadyToPostprocessDelegate OnRetainerReadyToPostprocess;

    /// <summary>
    /// 每次显示雇员设置时触发的事件。
    /// </summary>
    public event OnRetainerSettingsDrawDelegate OnRetainerSettingsDraw;

    /// <summary>
    /// 每次显示探险后任务时触发的事件。
    /// </summary>
    public event OnRetainerPostVentureTaskDrawDelegate OnRetainerPostVentureTaskDraw;

    /// <summary>
    /// 每次在雇员列表中显示任务按钮时触发的事件。
    /// </summary>
    public event OnRetainerListTaskButtonsDrawDelegate OnRetainerListTaskButtonsDraw;

    /// <summary>
    /// 角色处理完毕、准备接收额外后处理任务时触发的事件。
    /// </summary>
    public event OnCharacterPostprocessTaskDelegate OnCharacterPostprocessStep;

    /// <summary>
    /// 你的插件应执行额外后处理任务时触发的事件。必须将游戏界面恢复到接管前的状态。
    /// </summary>
    public event OnCharacterReadyToPostprocessDelegate OnCharacterReadyToPostProcess;

    /// <summary>
    /// 每次绘制主控件时触发的事件。
    /// </summary>
    public event OnMainControlsDrawDelegate OnMainControlsDraw;

    private string PluginName;

    public AutoRetainerApi(string additionalSuffix = null)
    {
        PluginName = Svc.PluginInterface.InternalName + (additionalSuffix == null ? "" : $"_{additionalSuffix}");
        Svc.PluginInterface.GetIpcSubscriber<string, object>(ApiConsts.OnSendRetainerToVenture).Subscribe(OnSendRetainerToVentureAction);
        Svc.PluginInterface.GetIpcSubscriber<string, object>(ApiConsts.OnRetainerAdditionalTask).Subscribe(OnRetainerAdditionalTask);
        Svc.PluginInterface.GetIpcSubscriber<string, string, object>(ApiConsts.OnRetainerReadyForPostprocess).Subscribe(OnRetainerReadyForPostprocessIntl);
        Svc.PluginInterface.GetIpcSubscriber<ulong, string, object>(ApiConsts.OnRetainerSettingsDraw).Subscribe(OnRetainerSettingsDrawAction);
        Svc.PluginInterface.GetIpcSubscriber<ulong, string, object>(ApiConsts.OnRetainerPostVentureTaskDraw).Subscribe(OnRetainerPostVentureTaskDrawAction);
        Svc.PluginInterface.GetIpcSubscriber<object>(ApiConsts.OnRetainerListTaskButtonsDraw).Subscribe(OnRetainerListTaskButtonsDrawAction);
        Svc.PluginInterface.GetIpcSubscriber<object>(ApiConsts.OnCharacterAdditionalTask).Subscribe(OnCharacterAdditionalTask);
        Svc.PluginInterface.GetIpcSubscriber<string, object>(ApiConsts.OnCharacterReadyForPostprocess).Subscribe(OnCharacterReadyForPostprocessIntl);
        Svc.PluginInterface.GetIpcSubscriber<object>(ApiConsts.OnMainControlsDraw).Subscribe(OnMainControlsDrawAction);
    }

    /// <summary>
    /// 请求 AutoRetainer 遍历列表中的所有雇员，并执行当前插件的 IPC 任务。不会执行其他插件的任务。此方法只能在 <see cref="OnRetainerListTaskButtonsDraw"/> 事件中调用。
    /// </summary>
    public void ProcessIPCTaskFromOverlay()
    {
        Svc.PluginInterface.GetIpcSubscriber<string, object>(ApiConsts.OnRetainerListCustomTask).InvokeAction(PluginName);
    }


    /// <summary>
    /// 在 <see cref="OnRetainerPostprocessStep"/> 事件中调用，以表示你希望对雇员执行后处理。
    /// </summary>
    public void RequestRetainerPostprocess()
    {
        Svc.PluginInterface.GetIpcSubscriber<string, object>(ApiConsts.RequestRetainerPostProcess).InvokeAction(PluginName);
    }

    /// <summary>
    /// 在 <see cref="OnRetainerReadyToPostprocess"/> 中调用，以表示你已完成后处理任务。
    /// </summary>
    public void FinishRetainerPostProcess() => Svc.PluginInterface.GetIpcSubscriber<object>(ApiConsts.FinishRetainerPostprocessRequest).InvokeAction();

    /// <summary>
    /// 在 <see cref="OnCharacterPostprocessStep"/> 事件中调用，以表示你希望对角色执行后处理。
    /// </summary>
    public void RequestCharacterPostprocess() => Svc.PluginInterface.GetIpcSubscriber<string, object>(ApiConsts.RequestCharacterPostProcess).InvokeAction(PluginName);


    /// <summary>
    /// 在 <see cref="OnCharacterReadyToPostProcess"/> 中调用，以表示你已完成后处理任务。
    /// </summary>
    public void FinishCharacterPostProcess() => Svc.PluginInterface.GetIpcSubscriber<object>(ApiConsts.FinishCharacterPostprocessRequest).InvokeAction();

    /// <summary>
    /// 指示 AutoRetainer API 是否已初始化并可供使用。
    /// </summary>
    public bool Ready
    {
        get
        {
            try
            {
                Svc.PluginInterface.GetIpcSubscriber<object>("AutoRetainer.Init").InvokeAction();
                return true;
            }
            catch(Exception)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// 允许设置抑制状态；在此状态下，无论配置如何，AutoRetainer 都不会执行任何操作。
    /// </summary>
    public bool Suppressed
    {
        get
        {
            return Svc.PluginInterface.GetIpcSubscriber<bool>("AutoRetainer.GetSuppressed").InvokeFunc();
        }
        set
        {
            Svc.PluginInterface.GetIpcSubscriber<bool, object>("AutoRetainer.SetSuppressed").InvokeAction(value);
        }
    }

    /// <summary>
    /// 释放 API。不要忘记调用此方法。
    /// </summary>
    public void Dispose()
    {
        Svc.PluginInterface.GetIpcSubscriber<string, object>(ApiConsts.OnSendRetainerToVenture).Unsubscribe(OnSendRetainerToVentureAction);
        Svc.PluginInterface.GetIpcSubscriber<string, object>(ApiConsts.OnRetainerAdditionalTask).Unsubscribe(OnRetainerAdditionalTask);
        Svc.PluginInterface.GetIpcSubscriber<string, string, object>(ApiConsts.OnRetainerReadyForPostprocess).Unsubscribe(OnRetainerReadyForPostprocessIntl);
        Svc.PluginInterface.GetIpcSubscriber<ulong, string, object>(ApiConsts.OnRetainerSettingsDraw).Unsubscribe(OnRetainerSettingsDrawAction);
        Svc.PluginInterface.GetIpcSubscriber<ulong, string, object>(ApiConsts.OnRetainerPostVentureTaskDraw).Unsubscribe(OnRetainerPostVentureTaskDrawAction);
        Svc.PluginInterface.GetIpcSubscriber<object>(ApiConsts.OnRetainerListTaskButtonsDraw).Unsubscribe(OnRetainerListTaskButtonsDrawAction);
        Svc.PluginInterface.GetIpcSubscriber<object>(ApiConsts.OnCharacterAdditionalTask).Unsubscribe(OnCharacterAdditionalTask);
        Svc.PluginInterface.GetIpcSubscriber<string, object>(ApiConsts.OnCharacterReadyForPostprocess).Unsubscribe(OnCharacterReadyForPostprocessIntl);
        Svc.PluginInterface.GetIpcSubscriber<object>(ApiConsts.OnMainControlsDraw).Unsubscribe(OnMainControlsDrawAction);
    }

    /// <summary>
    /// 在 <see cref="OnSendRetainerToVenture"/> 中调用，可设置雇员要执行的探险委托，覆盖用户配置。
    /// </summary>
    /// <param name="ventureId"></param>
    public void SetVenture(uint ventureId)
    {
        Svc.PluginInterface.GetIpcSubscriber<uint, object>("AutoRetainer.SetVenture").InvokeAction(ventureId);
    }

    /// <summary>
    /// 获取指定内容 ID 的 <see cref="OfflineCharacterData"/>。警告：不得保存此数据。每次需要读取最新的 <see cref="OfflineCharacterData"/> 时，都必须调用此函数。
    /// </summary>
    /// <param name="cid">角色的内容 ID</param>
    /// <returns></returns>
    public OfflineCharacterData GetOfflineCharacterData(ulong cid)
    {
        return Svc.PluginInterface.GetIpcSubscriber<ulong, OfflineCharacterData>("AutoRetainer.GetOfflineCharacterData").InvokeFunc(cid);
    }

    /// <summary>
    /// 将 <see cref="OfflineCharacterData"/> 写入 AutoRetainer。如果指定内容 ID 已存在另一个 <see cref="OfflineCharacterData"/> 实例，则将其替换。警告：必须读取数据、完成修改，并在同一帧更新内立即写回；禁止保存 <see cref="OfflineCharacterData"/>。
    /// </summary>
    /// <param name="data"></param>
    public void WriteOfflineCharacterData(OfflineCharacterData data)
    {
        if(data.CreationFrame != Svc.PluginInterface.UiBuilder.FrameCount) throw new Exception("必须在同一帧更新中读取数据、完成修改并立即写回；禁止保存 OfflineCharacterData。");
        Svc.PluginInterface.GetIpcSubscriber<OfflineCharacterData, object>("AutoRetainer.WriteOfflineCharacterData").InvokeAction(data);
    }

    /// <summary>
    /// 获取指定角色和雇员的 <see cref="AdditionalRetainerData"/>。警告：不得保存此数据。每次需要读取最新的 <see cref="AdditionalRetainerData"/> 时，都必须调用此函数。
    /// </summary>
    /// <param name="cid">目标角色的内容 ID</param>
    /// <param name="name">雇员名称</param>
    /// <returns></returns>
    public AdditionalRetainerData GetAdditionalRetainerData(ulong cid, string name)
    {
        return Svc.PluginInterface.GetIpcSubscriber<ulong, string, AdditionalRetainerData>("AutoRetainer.GetAdditionalRetainerData").InvokeFunc(cid, name);
    }

    /// <summary>
    /// 将 <see cref="AdditionalRetainerData"/> 写入 AutoRetainer。警告：必须读取数据、完成修改，并在同一帧更新内立即写回；禁止保存 <see cref="AdditionalRetainerData"/>。
    /// </summary>
    /// <param name="cid"></param>
    /// <param name="name"></param>
    /// <param name="data"></param>
    public void WriteAdditionalRetainerData(ulong cid, string name, AdditionalRetainerData data)
    {
        if(data.CreationFrame != Svc.PluginInterface.UiBuilder.FrameCount) throw new Exception("必须在同一帧更新中读取数据、完成修改并立即写回；禁止保存 AdditionalRetainerData。");
        Svc.PluginInterface.GetIpcSubscriber<ulong, string, AdditionalRetainerData, object>("AutoRetainer.WriteAdditionalRetainerData").InvokeAction(cid, name, data);
    }

    /// <summary>
    /// 返回所有已知角色的 CID，不包括已列入黑名单或尚未初始化的角色。
    /// </summary>
    /// <returns></returns>
    public List<ulong> GetRegisteredCharacters()
    {
        return Svc.PluginInterface.GetIpcSubscriber<List<ulong>>("AutoRetainer.GetRegisteredCIDs").InvokeFunc();
    }
}
