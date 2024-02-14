﻿using System.Reflection;

namespace Staple.Editor;

[CustomEditor(typeof(IStapleAsset))]
public class StapleAssetEditor : Editor
{
    public bool ApplyChanges() => StapleEditor.SaveAsset(path, (IStapleAsset)target);

    public void RenderApplyRevertFields()
    {
        var asset = (IStapleAsset)target;
        var originalAsset = (IStapleAsset)original;

        var hasChanges = asset != originalAsset;

        if (hasChanges)
        {
            if (EditorGUI.Button("Apply"))
            {
                if (ApplyChanges())
                {
                    var fields = asset.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

                    foreach (var field in fields)
                    {
                        field.SetValue(originalAsset, field.GetValue(asset));
                    }

                    EditorUtils.RefreshAssets(false, null);
                }
            }

            EditorGUI.SameLine();

            if (EditorGUI.Button("Revert"))
            {
                var fields = asset.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

                foreach (var field in fields)
                {
                    field.SetValue(asset, field.GetValue(originalAsset));
                }
            }
        }
        else
        {
            EditorGUI.ButtonDisabled("Apply");

            EditorGUI.SameLine();

            EditorGUI.ButtonDisabled("Revert");
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        RenderApplyRevertFields();
    }
}
