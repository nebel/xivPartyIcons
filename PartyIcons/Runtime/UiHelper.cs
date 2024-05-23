
using Dalamud.Game;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Dalamud.Logging;

#nullable enable
namespace SimpleTweaksPlugin.Utility
{
  public static class UiHelper
  {
    private static 
    #nullable disable
    UiHelper.AtkUnitBaseClose _atkUnitBaseClose;
    public static bool Ready;

    public static unsafe void SetSize(AtkResNode* node, int? width, int? height)
    {
      int? nullable;
      if (width.HasValue)
      {
        nullable = width;
        int num = 0;
        if (nullable.GetValueOrDefault() >= num & nullable.HasValue)
        {
          nullable = width;
          int maxValue = (int) ushort.MaxValue;
          if (nullable.GetValueOrDefault() <= maxValue & nullable.HasValue)
            node->Width = (ushort) width.Value;
        }
      }
      if (height.HasValue)
      {
        nullable = height;
        int num = 0;
        if (nullable.GetValueOrDefault() >= num & nullable.HasValue)
        {
          nullable = height;
          int maxValue = (int) ushort.MaxValue;
          if (nullable.GetValueOrDefault() <= maxValue & nullable.HasValue)
            node->Height = (ushort) height.Value;
        }
      }
      node->DrawFlags |= 1U;
    }

    public static unsafe void SetPosition(AtkResNode* node, float? x, float? y)
    {
      if (x.HasValue)
        node->X = x.Value;
      if (y.HasValue)
        node->Y = y.Value;
      node->DrawFlags |= 1U;
    }

    public static unsafe void SetPosition(AtkUnitBase* atkUnitBase, float? x, float? y)
    {
      float? nullable1 = x;
      float minValue1 = (float) short.MinValue;
      if ((double) nullable1.GetValueOrDefault() >= (double) minValue1 & nullable1.HasValue)
      {
        float? nullable2 = x;
        float maxValue = (float) short.MaxValue;
        if ((double) nullable2.GetValueOrDefault() <= (double) maxValue & nullable2.HasValue)
          atkUnitBase->X = (short) x.Value;
      }
      float? nullable3 = y;
      float minValue2 = (float) short.MinValue;
      if (!((double) nullable3.GetValueOrDefault() >= (double) minValue2 & nullable3.HasValue))
        return;
      float? nullable4 = x;
      float maxValue1 = (float) short.MaxValue;
      if (!((double) nullable4.GetValueOrDefault() <= (double) maxValue1 & nullable4.HasValue))
        return;
      atkUnitBase->Y = (short) y.Value;
    }

    public static unsafe void ExpandNodeList(AtkComponentNode* componentNode, ushort addSize)
    {
      AtkResNode** atkResNodePtr = UiHelper.ExpandNodeList(componentNode->Component->UldManager.NodeList, componentNode->Component->UldManager.NodeListCount, (ushort) ((uint) componentNode->Component->UldManager.NodeListCount + (uint) addSize));
      componentNode->Component->UldManager.NodeList = atkResNodePtr;
    }

    public static unsafe void ExpandNodeList(AtkUnitBase* atkUnitBase, ushort addSize)
    {
      AtkResNode** atkResNodePtr = UiHelper.ExpandNodeList(atkUnitBase->UldManager.NodeList, atkUnitBase->UldManager.NodeListCount, (ushort) ((uint) atkUnitBase->UldManager.NodeListCount + (uint) addSize));
      atkUnitBase->UldManager.NodeList = atkResNodePtr;
    }

    private static unsafe AtkResNode** ExpandNodeList(
      AtkResNode** originalList,
      ushort originalSize,
      ushort newSize = 0)
    {
      if ((int) newSize <= (int) originalSize)
        newSize = (ushort) ((uint) originalSize + 1U);
      IntPtr source = new IntPtr((void*) originalList);
      IntPtr destination = UiHelper.Alloc((ulong) (((int) newSize + 1) * 8));
      IntPtr[] numArray = new IntPtr[(int) originalSize];
      Marshal.Copy(source, numArray, 0, (int) originalSize);
      Marshal.Copy(numArray, 0, destination, (int) originalSize);
      return (AtkResNode**) destination;
    }

    public static unsafe AtkResNode* CloneNode(AtkResNode* original)
    {
      int num;
      switch (original->Type)
      {
        case NodeType.Res:
          num = sizeof (AtkResNode);
          break;
        case NodeType.Image:
          num = sizeof (AtkImageNode);
          break;
        case NodeType.Text:
          num = sizeof (AtkTextNode);
          break;
        case NodeType.NineGrid:
          num = sizeof (AtkNineGridNode);
          break;
        case NodeType.Counter:
          num = sizeof (AtkCounterNode);
          break;
        case NodeType.Collision:
          num = sizeof (AtkCollisionNode);
          break;
        default:
          DefaultInterpolatedStringHandler interpolatedStringHandler = new DefaultInterpolatedStringHandler(18, 1);
          interpolatedStringHandler.AppendLiteral("Unsupported Type: ");
          interpolatedStringHandler.AppendFormatted<NodeType>(original->Type);
          throw new Exception(interpolatedStringHandler.ToStringAndClear());
      }
      int size = num;
      IntPtr destination = UiHelper.Alloc((ulong) size);
      byte[] numArray = new byte[size];
      Marshal.Copy(new IntPtr((void*) original), numArray, 0, numArray.Length);
      Marshal.Copy(numArray, 0, destination, numArray.Length);
      AtkResNode* atkResNodePtr = (AtkResNode*) destination;
      atkResNodePtr->ParentNode = (AtkResNode*) null;
      atkResNodePtr->ChildNode = (AtkResNode*) null;
      atkResNodePtr->ChildCount = (ushort) 0;
      atkResNodePtr->PrevSiblingNode = (AtkResNode*) null;
      atkResNodePtr->NextSiblingNode = (AtkResNode*) null;
      return atkResNodePtr;
    }

    public static unsafe void Close(AtkUnitBase* atkUnitBase, bool unknownBool = false)
    {
      if (!UiHelper.Ready)
        return;
      int num = (int) UiHelper._atkUnitBaseClose(atkUnitBase, unknownBool ? (byte) 1 : (byte) 0);
    }

    public static void Setup(ISigScanner scanner)
    {
      UiHelper._atkUnitBaseClose = Marshal.GetDelegateForFunctionPointer<UiHelper.AtkUnitBaseClose>(scanner.ScanText("40 53 48 83 EC 50 81 A1"));
      UiHelper.Ready = true;
    }

    public static unsafe IntPtr Alloc(ulong size)
    {
      return new IntPtr(IMemorySpace.GetUISpace()->Malloc(size, 8UL));
    }

    public static IntPtr Alloc(int size)
    {
      return size > 0 ? UiHelper.Alloc((ulong) size) : throw new ArgumentException("Allocation size must be positive.");
    }

    public static unsafe AtkImageNode* MakeImageNode(uint id, UiHelper.PartInfo partInfo)
    {
      AtkImageNode* imageNode;
      if (!UiHelper.TryMakeImageNode(id, ~(NodeFlags.AnchorTop | NodeFlags.AnchorLeft | NodeFlags.AnchorBottom | NodeFlags.AnchorRight | NodeFlags.Visible | NodeFlags.Enabled | NodeFlags.Clip | NodeFlags.Fill | NodeFlags.HasCollision | NodeFlags.RespondToMouse | NodeFlags.Focusable | NodeFlags.Droppable | NodeFlags.IsTopNode | NodeFlags.EmitsEvents | NodeFlags.UseDepthBasedPriority | NodeFlags.UnkFlag2), 0U, (byte) 0, (byte) 0, out imageNode))
      {
        // PluginLog.Error((object) "Failed to alloc memory for AtkImageNode.", "/work/repo/Utility/UiHelper.Nodes.cs", nameof (MakeImageNode), 27);
        return (AtkImageNode*) null;
      }
      AtkUldPartsList* partsList;
      if (!UiHelper.TryMakePartsList(0U, out partsList))
      {
        // PluginLog.Error((object) "Failed to alloc memory for AtkUldPartsList.", "/work/repo/Utility/UiHelper.Nodes.cs", nameof (MakeImageNode), 33);
        UiHelper.FreeImageNode(imageNode);
        return (AtkImageNode*) null;
      }
      AtkUldPart* part;
      if (!UiHelper.TryMakePart(partInfo.U, partInfo.V, partInfo.Width, partInfo.Height, out part))
      {
        // PluginLog.Error((object) "Failed to alloc memory for AtkUldPart.", "/work/repo/Utility/UiHelper.Nodes.cs", nameof (MakeImageNode), 40);
        UiHelper.FreePartsList(partsList);
        UiHelper.FreeImageNode(imageNode);
        return (AtkImageNode*) null;
      }
      AtkUldAsset* asset;
      if (!UiHelper.TryMakeAsset(0U, out asset))
      {
        // PluginLog.Error((object) "Failed to alloc memory for AtkUldAsset.", "/work/repo/Utility/UiHelper.Nodes.cs", nameof (MakeImageNode), 48);
        UiHelper.FreePart(part);
        UiHelper.FreePartsList(partsList);
        UiHelper.FreeImageNode(imageNode);
      }
      UiHelper.AddAsset(part, asset);
      UiHelper.AddPart(partsList, part);
      UiHelper.AddPartsList(imageNode, partsList);
      return imageNode;
    }

    public static unsafe bool IsAddonReady(AtkUnitBase* addon)
    {
      return (IntPtr) addon != IntPtr.Zero && (IntPtr) addon->RootNode != IntPtr.Zero && (IntPtr) addon->RootNode->ChildNode != IntPtr.Zero;
    }

    public static unsafe AtkTextNode* MakeTextNode(uint id)
    {
      AtkTextNode* textNode;
      return !UiHelper.TryMakeTextNode(id, out textNode) ? (AtkTextNode*) null : textNode;
    }

    public static unsafe void LinkNodeAtEnd(AtkResNode* imageNode, AtkUnitBase* parent)
    {
      AtkResNode* atkResNodePtr = parent->RootNode->ChildNode;
      while ((IntPtr) atkResNodePtr->PrevSiblingNode != IntPtr.Zero)
        atkResNodePtr = atkResNodePtr->PrevSiblingNode;
      atkResNodePtr->PrevSiblingNode = imageNode;
      imageNode->NextSiblingNode = atkResNodePtr;
      imageNode->ParentNode = atkResNodePtr->ParentNode;
      parent->UldManager.UpdateDrawNodeList();
    }

    public static unsafe void LinkNodeAtEnd<T>(
      T* atkNode,
      AtkResNode* parentNode,
      AtkUnitBase* addon)
      where T : unmanaged
    {
      AtkResNode* atkResNodePtr1 = (AtkResNode*) atkNode;
      AtkResNode* atkResNodePtr2 = parentNode->ChildNode;
      if ((IntPtr) atkResNodePtr2 == IntPtr.Zero)
      {
        parentNode->ChildNode = atkResNodePtr1;
        atkResNodePtr1->ParentNode = parentNode;
        atkResNodePtr1->PrevSiblingNode = (AtkResNode*) null;
        atkResNodePtr1->NextSiblingNode = (AtkResNode*) null;
      }
      else
      {
        while ((IntPtr) atkResNodePtr2->PrevSiblingNode != IntPtr.Zero)
          atkResNodePtr2 = atkResNodePtr2->PrevSiblingNode;
        atkResNodePtr1->ParentNode = parentNode;
        atkResNodePtr1->NextSiblingNode = atkResNodePtr2;
        atkResNodePtr1->PrevSiblingNode = (AtkResNode*) null;
        atkResNodePtr2->PrevSiblingNode = atkResNodePtr1;
      }
      addon->UldManager.UpdateDrawNodeList();
    }

    public static unsafe void LinkNodeAfterTargetNode(
      AtkResNode* node,
      AtkComponentNode* parent,
      AtkResNode* targetNode)
    {
      AtkResNode* prevSiblingNode = targetNode->PrevSiblingNode;
      node->ParentNode = targetNode->ParentNode;
      targetNode->PrevSiblingNode = node;
      prevSiblingNode->NextSiblingNode = node;
      node->PrevSiblingNode = prevSiblingNode;
      node->NextSiblingNode = targetNode;
      parent->Component->UldManager.UpdateDrawNodeList();
    }

    public static unsafe void LinkNodeAfterTargetNode<T>(
      T* atkNode,
      AtkUnitBase* parent,
      AtkResNode* targetNode)
      where T : unmanaged
    {
      AtkResNode* atkResNodePtr = (AtkResNode*) atkNode;
      AtkResNode* prevSiblingNode = targetNode->PrevSiblingNode;
      atkResNodePtr->ParentNode = targetNode->ParentNode;
      targetNode->PrevSiblingNode = atkResNodePtr;
      prevSiblingNode->NextSiblingNode = atkResNodePtr;
      atkResNodePtr->PrevSiblingNode = prevSiblingNode;
      atkResNodePtr->NextSiblingNode = targetNode;
      parent->UldManager.UpdateDrawNodeList();
    }

    public static unsafe void UnlinkNode<T>(T* atkNode, AtkComponentNode* componentNode) where T : unmanaged
    {
      AtkResNode* atkResNodePtr = (AtkResNode*) atkNode;
      if ((IntPtr) atkResNodePtr == IntPtr.Zero)
        return;
      if (atkResNodePtr->ParentNode->ChildNode == atkResNodePtr)
        atkResNodePtr->ParentNode->ChildNode = atkResNodePtr->NextSiblingNode;
      if ((IntPtr) atkResNodePtr->NextSiblingNode != IntPtr.Zero && atkResNodePtr->NextSiblingNode->PrevSiblingNode == atkResNodePtr)
        atkResNodePtr->NextSiblingNode->PrevSiblingNode = atkResNodePtr->PrevSiblingNode;
      if ((IntPtr) atkResNodePtr->PrevSiblingNode != IntPtr.Zero && atkResNodePtr->PrevSiblingNode->NextSiblingNode == atkResNodePtr)
        atkResNodePtr->PrevSiblingNode->NextSiblingNode = atkResNodePtr->NextSiblingNode;
      componentNode->Component->UldManager.UpdateDrawNodeList();
    }

    public static unsafe void UnlinkNode<T>(T* atkNode, AtkUnitBase* unitBase) where T : unmanaged
    {
      AtkResNode* atkResNodePtr = (AtkResNode*) atkNode;
      if ((IntPtr) atkResNodePtr == IntPtr.Zero)
        return;
      if (atkResNodePtr->ParentNode->ChildNode == atkResNodePtr)
        atkResNodePtr->ParentNode->ChildNode = atkResNodePtr->NextSiblingNode;
      if ((IntPtr) atkResNodePtr->NextSiblingNode != IntPtr.Zero && atkResNodePtr->NextSiblingNode->PrevSiblingNode == atkResNodePtr)
        atkResNodePtr->NextSiblingNode->PrevSiblingNode = atkResNodePtr->PrevSiblingNode;
      if ((IntPtr) atkResNodePtr->PrevSiblingNode != IntPtr.Zero && atkResNodePtr->PrevSiblingNode->NextSiblingNode == atkResNodePtr)
        atkResNodePtr->PrevSiblingNode->NextSiblingNode = atkResNodePtr->NextSiblingNode;
      unitBase->UldManager.UpdateDrawNodeList();
    }

    public static unsafe void UnlinkAndFreeImageNodeIndirect(AtkImageNode* node, AtkUldManager uldManager)
    {
      if ((IntPtr) node->AtkResNode.PrevSiblingNode != IntPtr.Zero)
        node->AtkResNode.PrevSiblingNode->NextSiblingNode = node->AtkResNode.NextSiblingNode;
      if ((IntPtr) node->AtkResNode.NextSiblingNode != IntPtr.Zero)
        node->AtkResNode.NextSiblingNode->PrevSiblingNode = node->AtkResNode.PrevSiblingNode;
      uldManager.UpdateDrawNodeList();
      UiHelper.FreePartsList(node->PartsList);
      UiHelper.FreeImageNode(node);
    }

    public static unsafe void UnlinkAndFreeImageNode(AtkImageNode* node, AtkUnitBase* parent)
    {
      if ((IntPtr) node->AtkResNode.PrevSiblingNode != IntPtr.Zero)
        node->AtkResNode.PrevSiblingNode->NextSiblingNode = node->AtkResNode.NextSiblingNode;
      if ((IntPtr) node->AtkResNode.NextSiblingNode != IntPtr.Zero)
        node->AtkResNode.NextSiblingNode->PrevSiblingNode = node->AtkResNode.PrevSiblingNode;
      parent->UldManager.UpdateDrawNodeList();
      UiHelper.FreePartsList(node->PartsList);
      UiHelper.FreeImageNode(node);
    }

    public static unsafe void UnlinkAndFreeTextNode(AtkTextNode* node, AtkUnitBase* parent)
    {
      if ((IntPtr) node->AtkResNode.PrevSiblingNode != IntPtr.Zero)
        node->AtkResNode.PrevSiblingNode->NextSiblingNode = node->AtkResNode.NextSiblingNode;
      if ((IntPtr) node->AtkResNode.NextSiblingNode != IntPtr.Zero)
        node->AtkResNode.NextSiblingNode->PrevSiblingNode = node->AtkResNode.PrevSiblingNode;
      parent->UldManager.UpdateDrawNodeList();
      UiHelper.FreeTextNode(node);
    }

    public static unsafe bool TryMakeTextNode(uint id, [NotNullWhen(true)] out AtkTextNode* textNode)
    {
      textNode = IMemorySpace.GetUISpace()->Create<AtkTextNode>();
      if ((IntPtr) textNode == IntPtr.Zero)
        return false;
      textNode->AtkResNode.Type = NodeType.Text;
      textNode->AtkResNode.NodeID = id;
      return true;
    }

    public static unsafe bool TryMakeImageNode(
      uint id,
      NodeFlags resNodeFlags,
      uint resNodeDrawFlags,
      byte wrapMode,
      byte imageNodeFlags,
      [NotNullWhen(true)] out AtkImageNode* imageNode)
    {
      imageNode = IMemorySpace.GetUISpace()->Create<AtkImageNode>();
      if ((IntPtr) imageNode == IntPtr.Zero)
        return false;
      imageNode->AtkResNode.Type = NodeType.Image;
      imageNode->AtkResNode.NodeID = id;
      imageNode->AtkResNode.NodeFlags = resNodeFlags;
      imageNode->AtkResNode.DrawFlags = resNodeDrawFlags;
      imageNode->WrapMode = wrapMode;
      imageNode->Flags = imageNodeFlags;
      return true;
    }

    public static unsafe bool TryMakePartsList(uint id, [NotNullWhen(true)] out AtkUldPartsList* partsList)
    {
      partsList = (AtkUldPartsList*) IMemorySpace.GetUISpace()->Malloc((ulong) sizeof (AtkUldPartsList), 8UL);
      if ((IntPtr) partsList == IntPtr.Zero)
        return false;
      partsList->Id = id;
      partsList->PartCount = 0U;
      partsList->Parts = (AtkUldPart*) null;
      return true;
    }

    public static unsafe bool TryMakePart(
      ushort u,
      ushort v,
      ushort width,
      ushort height,
      [NotNullWhen(true)] out AtkUldPart* part)
    {
      part = (AtkUldPart*) IMemorySpace.GetUISpace()->Malloc((ulong) sizeof (AtkUldPart), 8UL);
      if ((IntPtr) part == IntPtr.Zero)
        return false;
      part->U = u;
      part->V = v;
      part->Width = width;
      part->Height = height;
      return true;
    }

    public static unsafe bool TryMakeAsset(uint id, [NotNullWhen(true)] out AtkUldAsset* asset)
    {
      asset = (AtkUldAsset*) IMemorySpace.GetUISpace()->Malloc((ulong) sizeof (AtkUldAsset), 8UL);
      if ((IntPtr) asset == IntPtr.Zero)
        return false;
      asset->Id = id;
      asset->AtkTexture.Ctor();
      return true;
    }

    public static unsafe void AddPartsList(AtkImageNode* imageNode, AtkUldPartsList* partsList)
    {
      imageNode->PartsList = partsList;
    }

    public static unsafe void AddPartsList(AtkCounterNode* counterNode, AtkUldPartsList* partsList)
    {
      counterNode->PartsList = partsList;
    }

    public static unsafe void AddPart(AtkUldPartsList* partsList, AtkUldPart* part)
    {
      AtkUldPart* parts = partsList->Parts;
      uint num1 = partsList->PartCount + 1U;
      AtkUldPart* atkUldPartPtr = (AtkUldPart*) IMemorySpace.GetUISpace()->Malloc((ulong) sizeof (AtkUldPart) * (ulong) num1, 8UL);
      if ((IntPtr) parts != IntPtr.Zero)
      {
        foreach (int num2 in Enumerable.Range(0, (int) partsList->PartCount))
          Buffer.MemoryCopy((void*) (parts + num2), (void*) (atkUldPartPtr + num2), (long) sizeof (AtkUldPart), (long) sizeof (AtkUldPart));
        IMemorySpace.Free((void*) parts, (ulong) sizeof (AtkUldPart) * (ulong) partsList->PartCount);
      }
      Buffer.MemoryCopy((void*) part, (void*) ((IntPtr) atkUldPartPtr + (IntPtr) ((long) (num1 - 1U) * (long) sizeof (AtkUldPart))), (long) sizeof (AtkUldPart), (long) sizeof (AtkUldPart));
      partsList->Parts = atkUldPartPtr;
      partsList->PartCount = num1;
    }

    public static unsafe void AddAsset(AtkUldPart* part, AtkUldAsset* asset)
    {
      part->UldAsset = asset;
    }

    public static unsafe void FreeImageNode(AtkImageNode* node)
    {
      node->AtkResNode.Destroy(false);
      IMemorySpace.Free((void*) node, (ulong) sizeof (AtkImageNode));
    }

    public static unsafe void FreeTextNode(AtkTextNode* node)
    {
      node->AtkResNode.Destroy(false);
      IMemorySpace.Free((void*) node, (ulong) sizeof (AtkTextNode));
    }

    public static unsafe void FreePartsList(AtkUldPartsList* partsList)
    {
      foreach (int num in Enumerable.Range(0, (int) partsList->PartCount))
      {
        AtkUldPart* part = partsList->Parts + num;
        UiHelper.FreeAsset(part->UldAsset);
        UiHelper.FreePart(part);
      }
      IMemorySpace.Free((void*) partsList, (ulong) sizeof (AtkUldPartsList));
    }

    public static unsafe void FreePart(AtkUldPart* part)
    {
      IMemorySpace.Free((void*) part, (ulong) sizeof (AtkUldPart));
    }

    public static unsafe void FreeAsset(AtkUldAsset* asset)
    {
      IMemorySpace.Free((void*) asset, (ulong) sizeof (AtkUldAsset));
    }

    public static unsafe void SetSize<T>(T* node, int? w, int? h) where T : unmanaged
    {
      UiHelper.SetSize((AtkResNode*) node, w, h);
    }

    public static unsafe void SetPosition<T>(T* node, float? x, float? y) where T : unmanaged
    {
      UiHelper.SetPosition((AtkResNode*) node, x, y);
    }

    public static unsafe T* CloneNode<T>(T* original) where T : unmanaged
    {
      return (T*) UiHelper.CloneNode((AtkResNode*) original);
    }

    private unsafe delegate byte AtkUnitBaseClose(AtkUnitBase* unitBase, byte a2);

    public record PartInfo(ushort U, ushort V, ushort Width, ushort Height);

    public static unsafe T* GetNodeByID<T>(AtkUldManager* uldManager, uint nodeId, NodeType? type = null)
      where T : unmanaged
    {
      if ((IntPtr)uldManager->NodeList == IntPtr.Zero)
        return (T*)null;
      for (int index = 0; index < (int)uldManager->NodeListCount; ++index) {
        AtkResNode* nodeById = uldManager->NodeList[index];
        if ((IntPtr)nodeById != IntPtr.Zero && (int)nodeById->NodeID == (int)nodeId &&
            (!type.HasValue || nodeById->Type == type.Value))
          return (T*)nodeById;
      }

      return (T*)null;
    }
  }
}
