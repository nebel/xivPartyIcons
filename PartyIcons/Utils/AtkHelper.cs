using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace PartyIcons.Utils;

// Blatantly copied from SimpleTweaks
public static class AtkHelper
{
    public static unsafe AtkImageNode* MakeImageNode(uint id, PartInfo partInfo)
    {
        if (!TryMakeImageNode(id,
                ~(NodeFlags.AnchorTop | NodeFlags.AnchorLeft | NodeFlags.AnchorBottom | NodeFlags.AnchorRight |
                  NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.Clip | NodeFlags.Fill | NodeFlags.HasCollision |
                  NodeFlags.RespondToMouse | NodeFlags.Focusable | NodeFlags.Droppable | NodeFlags.IsTopNode |
                  NodeFlags.EmitsEvents | NodeFlags.UseDepthBasedPriority | NodeFlags.UnkFlag2), 0U, 0, 0,
                out var imageNode)) {
            return null;
        }

        if (!TryMakePartsList(0U, out var partsList)) {
            FreeImageNode(imageNode);
            return null;
        }

        if (!TryMakePart(partInfo.U, partInfo.V, partInfo.Width, partInfo.Height, out var part)) {
            FreePartsList(partsList);
            FreeImageNode(imageNode);
            return null;
        }

        if (!TryMakeAsset(0U, out var asset)) {
            FreePart(part);
            FreePartsList(partsList);
            FreeImageNode(imageNode);
            return null;
        }

        AddAsset(part, asset);
        AddPart(partsList, part);
        AddPartsList(imageNode, partsList);
        return imageNode;
    }

    public static unsafe void LinkNodeAfterTargetNode(AtkResNode* node, AtkComponentNode* parentComponent, AtkResNode* targetNode)
    {
        if (targetNode->PrevSiblingNode == null) {
            throw new Exception(
                $"LinkNodeAfterTargetNode: Failed to link 0x{(nint)node:X} (parent 0x{(nint)parentComponent:X}, target 0x{(nint)targetNode:X} since PrevSiblingNode was null");
        }

        var prevSiblingNode = targetNode->PrevSiblingNode;
        node->ParentNode = targetNode->ParentNode;
        targetNode->PrevSiblingNode = node;
        prevSiblingNode->NextSiblingNode = node;
        node->PrevSiblingNode = prevSiblingNode;
        node->NextSiblingNode = targetNode;
        parentComponent->Component->UldManager.UpdateDrawNodeList();
    }

    public static unsafe void LinkNodeAtEnd(AtkResNode* intruder, AtkComponentNode* parentComponent, AtkResNode* parentNode)
    {
        var node = parentNode->ChildNode;
        if (node != null) {
            while (node->PrevSiblingNode != null) node = node->PrevSiblingNode;
        }

        node->PrevSiblingNode = intruder;
        intruder->NextSiblingNode = node;
        intruder->ParentNode = node->ParentNode;

        if (intruder->ParentNode->NextSiblingNode == intruder->NextSiblingNode) {
            intruder->ParentNode->NextSiblingNode = intruder;
        }

        parentComponent->Component->UldManager.UpdateDrawNodeList();
    }

    public static unsafe void UnlinkAndFreeImageNodeIndirect(AtkImageNode* node, AtkUldManager* uldManager)
    {
        if ((IntPtr)node->ParentNode->NextSiblingNode == (IntPtr)node)
            node->ParentNode->NextSiblingNode = node->NextSiblingNode;
        if ((IntPtr)node->PrevSiblingNode != IntPtr.Zero)
            node->PrevSiblingNode->NextSiblingNode = node->NextSiblingNode;
        if ((IntPtr)node->NextSiblingNode != IntPtr.Zero)
            node->NextSiblingNode->PrevSiblingNode = node->PrevSiblingNode;
        uldManager->UpdateDrawNodeList();
        FreePartsList(node->PartsList);
        FreeImageNode(node);
    }

    private static unsafe bool TryMakeImageNode(uint id, NodeFlags resNodeFlags, uint resNodeDrawFlags, byte wrapMode,
        byte imageNodeFlags, [NotNullWhen(true)] out AtkImageNode* imageNode)
    {
        imageNode = IMemorySpace.GetUISpace()->Create<AtkImageNode>();
        if ((IntPtr)imageNode == IntPtr.Zero)
            return false;
        imageNode->Type = NodeType.Image;
        imageNode->NodeId = id;
        imageNode->NodeFlags = resNodeFlags;
        imageNode->DrawFlags = resNodeDrawFlags;
        imageNode->WrapMode = wrapMode;
        imageNode->Flags = imageNodeFlags;
        return true;
    }

    private static unsafe bool TryMakePartsList(uint id, [NotNullWhen(true)] out AtkUldPartsList* partsList)
    {
        partsList = (AtkUldPartsList*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkUldPartsList), 8UL);
        if ((IntPtr)partsList == IntPtr.Zero)
            return false;
        partsList->Id = id;
        partsList->PartCount = 0U;
        partsList->Parts = null;
        return true;
    }

    private static unsafe bool TryMakePart(ushort u, ushort v, ushort width, ushort height,
        [NotNullWhen(true)] out AtkUldPart* part)
    {
        part = (AtkUldPart*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkUldPart), 8UL);
        if ((IntPtr)part == IntPtr.Zero)
            return false;
        part->U = u;
        part->V = v;
        part->Width = width;
        part->Height = height;
        return true;
    }

    private static unsafe bool TryMakeAsset(uint id, [NotNullWhen(true)] out AtkUldAsset* asset)
    {
        asset = (AtkUldAsset*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkUldAsset), 8UL);
        if ((IntPtr)asset == IntPtr.Zero)
            return false;
        asset->Id = id;
        asset->AtkTexture.Ctor();
        return true;
    }

    private static unsafe void AddPartsList(AtkImageNode* imageNode, AtkUldPartsList* partsList)
    {
        imageNode->PartsList = partsList;
    }

    private static unsafe void AddPart(AtkUldPartsList* partsList, AtkUldPart* part)
    {
        var parts = partsList->Parts;
        var num1 = partsList->PartCount + 1U;
        var atkUldPartPtr =
            (AtkUldPart*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkUldPart) * num1, 8UL);
        if ((IntPtr)parts != IntPtr.Zero) {
            foreach (var num2 in Enumerable.Range(0, (int)partsList->PartCount))
                Buffer.MemoryCopy(parts + num2, atkUldPartPtr + num2, sizeof(AtkUldPart), sizeof(AtkUldPart));
            IMemorySpace.Free(parts, (ulong)sizeof(AtkUldPart) * partsList->PartCount);
        }

        Buffer.MemoryCopy(part, (void*)((IntPtr)atkUldPartPtr + (IntPtr)((num1 - 1U) * sizeof(AtkUldPart))),
            sizeof(AtkUldPart), sizeof(AtkUldPart));
        partsList->Parts = atkUldPartPtr;
        partsList->PartCount = num1;
    }

    private static unsafe void AddAsset(AtkUldPart* part, AtkUldAsset* asset)
    {
        part->UldAsset = asset;
    }

    private static unsafe void FreeImageNode(AtkImageNode* node)
    {
        node->Destroy(false);
        IMemorySpace.Free(node, (ulong)sizeof(AtkImageNode));
    }

    private static unsafe void FreePartsList(AtkUldPartsList* partsList)
    {
        foreach (var num in Enumerable.Range(0, (int)partsList->PartCount)) {
            var part = partsList->Parts + num;
            FreeAsset(part->UldAsset);
            FreePart(part);
        }

        IMemorySpace.Free(partsList, (ulong)sizeof(AtkUldPartsList));
    }

    private static unsafe void FreePart(AtkUldPart* part)
    {
        IMemorySpace.Free(part, (ulong)sizeof(AtkUldPart));
    }

    private static unsafe void FreeAsset(AtkUldAsset* asset)
    {
        IMemorySpace.Free(asset, (ulong)sizeof(AtkUldAsset));
    }

    public record PartInfo(ushort U, ushort V, ushort Width, ushort Height);

    public static unsafe T* GetNodeByID<T>(AtkUldManager* uldManager, uint nodeId, NodeType? type = null)
        where T : unmanaged
    {
        // Service.Log.Warning($"GetNodeByID(0x{(nint)uldManager:X}, {nodeId}, {type})");
        if ((IntPtr)uldManager->NodeList == IntPtr.Zero)
            return null;
        for (var index = 0; index < uldManager->NodeListCount; ++index) {
            var nodeById = uldManager->NodeList[index];
            if ((IntPtr)nodeById != IntPtr.Zero && (int)nodeById->NodeId == (int)nodeId &&
                (!type.HasValue || nodeById->Type == type.Value))
                return (T*)nodeById;
        }

        return null;
    }
}