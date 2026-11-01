using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using DetectiveDesk.Gameplay;

namespace DetectiveDesk.Editor
{
    public static class SkillTreeIntegration
    {
        static readonly Color Gold = new Color(.59f,.44f,.22f), Ivory = new Color(.91f,.85f,.72f), Dark = new Color(.055f,.039f,.031f,.99f);
        static Font font;
        static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent,false);
            var r = (RectTransform)go.transform; r.anchorMin=r.anchorMax=new Vector2(.5f,1); r.pivot=new Vector2(.5f,1); r.anchoredPosition=new Vector2(x,y); r.sizeDelta=new Vector2(w,h); return r;
        }
        static Image Box(string name, Transform parent, float x,float y,float w,float h,Color color)
        { var r=Rect(name,parent,x,y,w,h); var i=r.gameObject.AddComponent<Image>();i.color=color;return i; }
        static Text Label(string name,Transform parent,float x,float y,float w,float h,string text,int size=18,TextAnchor align=TextAnchor.MiddleLeft)
        {var r=Rect(name,parent,x,y,w,h);var t=r.gameObject.AddComponent<Text>(); t.font=font;t.text=text;t.fontSize=size;t.color=Ivory;t.alignment=align;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;}
        static Button Button(string name,Transform parent,float x,float y,float w,float h,string text,UnityAction action)
        {var i=Box(name,parent,x,y,w,h,new Color(.18f,.125f,.065f));var b=i.gameObject.AddComponent<Button>();b.targetGraphic=i;UnityEventTools.AddPersistentListener(b.onClick,action); Label("Caption",i.transform,0,0,w-12,h,text,18,TextAnchor.MiddleCenter);return b;}
        public static void Finish()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
            var game=UnityEngine.Object.FindFirstObjectByType<GameManager>();var panel=game.skillTreePanel;font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            panel.wallet.rectTransform.anchoredPosition=new Vector2(284,-62);panel.summary.rectTransform.anchoredPosition=new Vector2(-284,-65);panel.summary.rectTransform.sizeDelta=new Vector2(520,28);
            foreach(var graphic in panel.connections)
            {
                if(!graphic.GetComponent<CanvasRenderer>())graphic.gameObject.AddComponent<CanvasRenderer>();
                var links=graphic.links.ToList();
                foreach(var view in panel.nodeViews.Where(v=>v.transform.parent==graphic.transform.parent))
                {
                    var node=panel.definition.Find(view.nodeId);
                    if(node.tab==1)view.title.fontSize=16;
                    if(node.tab==1&&node.rank==1&&!links.Any(l=>string.IsNullOrEmpty(l.sourceId)&&l.targetId==node.id))links.Add(new SkillTreeLinkGraphic.Link{source=(RectTransform)graphic.transform.parent.Find("Workshop foundation"),target=(RectTransform)view.transform,sourceId="",targetId=node.id});
                    foreach(var id in node.prerequisites)
                    {
                        var source=panel.definition.Find(id);if(source.tab==node.tab||links.Any(l=>l.sourceId==id&&l.targetId==node.id))continue;
                        var label=Label("External "+node.id,view.transform.parent,node.position.x,node.position.y+72,220,20,source.title.ToUpperInvariant()+" REQUIRED",12,TextAnchor.MiddleCenter);
                        links.Add(new SkillTreeLinkGraphic.Link{source=label.rectTransform,target=(RectTransform)view.transform,sourceId=id,targetId=node.id});
                    }
                }
                graphic.links=links.ToArray();EditorUtility.SetDirty(graphic);
            }
            panel.game=null;PrefabUtility.SaveAsPrefabAsset(panel.gameObject,"Assets/DetectiveDesk/Gameplay/SkillTree/SkillTreePanel.prefab");panel.game=game;EditorUtility.SetDirty(panel);
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(game.gameObject.scene);EditorSceneManager.SaveScene(game.gameObject.scene);
        }
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
            var game=UnityEngine.Object.FindFirstObjectByType<GameManager>();if(!game||!game.legacy||!game.legacy.upgradesPanel)throw new Exception("Existing bindings missing");
            var modal=game.legacy.upgradesPanel; if(modal.transform.Find("SkillTree content"))throw new Exception("Skill tree already exists; inspect before rebuilding");
            Directory.CreateDirectory("Assets/DetectiveDesk/Gameplay/SkillTree");
            var path="Assets/DetectiveDesk/Gameplay/SkillTree/DefaultSkillTree.asset";
            var tree=AssetDatabase.LoadAssetAtPath<SkillTreeDefinition>(path);
            if(!tree){tree=ScriptableObject.CreateInstance<SkillTreeDefinition>();tree.nodes=SkillTreeDefinition.DefaultNodes(game.config.maximumUpgradeLevel);AssetDatabase.CreateAsset(tree,path);}
            string error;if(!tree.Validate(out error)||tree.nodes.Length!=39)throw new Exception(error+" Expected 39 nodes");
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Undo.RegisterFullObjectHierarchyUndo(modal,"Install skill tree in existing modal");Undo.RecordObject(game,"Bind skill tree");
            var mr=(RectTransform)modal.transform;mr.sizeDelta=new Vector2(1160,760);
            var mi=modal.GetComponent<Image>();if(mi){mi.color=Gold;mi.raycastTarget=true;}
            game.panel.gameObject.SetActive(false);
            var content=Box("SkillTree content",modal.transform,0,-3,1154,754,Dark);var panel=content.gameObject.AddComponent<SkillTreePanel>();panel.game=game;panel.definition=tree;
            Label("Heading",content.transform,-320,-18,450,44,"DETECTIVE WORKSHOP",29);
            panel.wallet=Label("Available cash",content.transform,284,-62,410,36,"",23,TextAnchor.MiddleRight);
            panel.summary=Label("Global multiplier",content.transform,-284,-65,520,28,"",16);
            panel.engineeringTab=Button("Engineering tab",content.transform,-384,-106,310,42,"ENGINEERING",panel.ShowEngineering).GetComponent<Image>();
            panel.restorationTab=Button("Restorations tab",content.transform,-58,-106,310,42,"RESTORATIONS",panel.ShowRestorations).GetComponent<Image>();
            Label("Scroll hint",content.transform,-176,-704,780,28,"SCROLL TO EXPLORE ALL RANKS  /  SELECT ANY NODE TO INSPECT",15,TextAnchor.MiddleCenter);
            var detail=Box("Selected skill",content.transform,420,-166,278,523,new Color(.085f,.062f,.044f));
            panel.detailTitle=Label("Title",detail.transform,0,-14,246,70,"",23);
            Label("Effect heading",detail.transform,0,-88,246,24,"UPGRADE EFFECT",14);
            panel.detailEffect=Label("Effect",detail.transform,0,-115,246,138,"",17,TextAnchor.UpperLeft);
            Label("Requirements heading",detail.transform,0,-258,246,24,"PREREQUISITES",14);
            panel.detailRequirements=Label("Requirements",detail.transform,0,-288,246,84,"",16,TextAnchor.UpperLeft);
            panel.detailPrice=Label("Price",detail.transform,0,-377,246,32,"",21);
            panel.detailState=Label("Blocked reason",detail.transform,0,-412,246,54,"",16,TextAnchor.UpperLeft);
            panel.buyButton=Button("Unlock selected",detail.transform,0,-470,246,42,"UNLOCK SKILL",panel.PurchaseSelected);panel.buyCaption=panel.buyButton.GetComponentInChildren<Text>();
            var views=new List<SkillTreeNodeView>();var graphics=new List<SkillTreeLinkGraphic>();
            for(int tab=0;tab<2;tab++)
            {
                var viewport=Box(tab==0?"Engineering viewport":"Restorations viewport",content.transform,-142,-166,790,523,new Color(.035f,.029f,.025f));
                viewport.gameObject.AddComponent<RectMask2D>();var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=(RectTransform)viewport.transform;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=38;
                var graph=Rect("Graph",viewport.transform,0,0,790,tab==0?1650:660);scroll.content=graph;
                if(tab==0){panel.engineering=viewport.gameObject;panel.engineeringScroll=scroll;}else{panel.restorations=viewport.gameObject;panel.restorationScroll=scroll;}
                var root=Box("Workshop foundation",graph,0,-12,208,45,new Color(.18f,.125f,.065f));Label("Foundation title",root.transform,0,0,208,45,"WORKSHOP / FREE",17,TextAnchor.MiddleCenter);
                string[] headers=tab==0?new[]{"MOMENTUM","AUTO-MOTOR","PINCRAFT"}:new[]{"DETECTIVE","ARCHIVIST","COMMISSIONER"};
                for(int col=0;col<3;col++)Label("Branch "+col,graph,(col-1)*260,-91,232,28,headers[col],18,TextAnchor.MiddleCenter);
                var lines=Rect("Prerequisite connections",graph,0,0,790,tab==0?1650:660).gameObject.AddComponent<SkillTreeLinkGraphic>();lines.owner=panel;lines.raycastTarget=false;graphics.Add(lines);
                var map=new Dictionary<string,RectTransform>();
                foreach(var node in tree.nodes.Where(n=>n.tab==tab))
                {
                    var border=Box(node.id,graph,node.position.x,node.position.y+48,232,96,Gold);
                    var bg=Box("Face",border.transform,0,-2,228,92,Dark);bg.raycastTarget=false;
                    var view=border.gameObject.AddComponent<SkillTreeNodeView>();view.nodeId=node.id;view.owner=panel;view.border=border;view.background=bg;
                    var selection=Box("Selected",border.transform,0,0,232,3,Ivory);selection.raycastTarget=false;view.selection=selection;
                    view.title=Label("Node title",border.transform,0,-7,212,38,"",18,TextAnchor.MiddleCenter);
                    view.state=Label("State",border.transform,-43,-50,124,24,"",14,TextAnchor.MiddleLeft);
                    view.cost=Label("Cost",border.transform,60,-50,96,24,"",15,TextAnchor.MiddleRight);
                    view.button=border.gameObject.AddComponent<Button>();view.button.targetGraphic=border;view.button.transition=Selectable.Transition.None;UnityEventTools.AddPersistentListener(view.button.onClick,view.Select);
                    views.Add(view);map.Add(node.id,(RectTransform)border.transform);
                }
                var edges=new List<SkillTreeLinkGraphic.Link>();
                foreach(var node in tree.nodes.Where(n=>n.tab==tab))
                {
                    if(node.prerequisites.Length==0)edges.Add(new SkillTreeLinkGraphic.Link{source=(RectTransform)root.transform,target=map[node.id],sourceId="",targetId=node.id});
                    foreach(var id in node.prerequisites){RectTransform source;if(map.TryGetValue(id,out source))edges.Add(new SkillTreeLinkGraphic.Link{source=source,target=map[node.id],sourceId=id,targetId=node.id});}
                }
                lines.links=edges.ToArray();
            }
            panel.nodeViews=views.ToArray();panel.connections=graphics.ToArray();game.skillTree=tree;game.skillTreePanel=panel;panel.ShowEngineering();
            var close=(RectTransform)modal.transform.Find("Button_CLOSE");close.anchorMin=close.anchorMax=new Vector2(1,1);close.pivot=new Vector2(1,1);close.anchoredPosition=new Vector2(-17,-13);close.sizeDelta=new Vector2(100,36);close.SetAsLastSibling();
            var ct=close.GetComponentInChildren<Text>();var ctr=ct.rectTransform;ctr.anchorMin=Vector2.zero;ctr.anchorMax=Vector2.one;ctr.pivot=new Vector2(.5f,.5f);ctr.anchoredPosition=Vector2.zero;ctr.sizeDelta=Vector2.zero;ct.alignment=TextAnchor.MiddleCenter;ct.fontSize=17;
            // Prefab cannot serialize an external scene manager. Keep its internal references and restore the scene override.
            panel.game=null;PrefabUtility.SaveAsPrefabAsset(content.gameObject,"Assets/DetectiveDesk/Gameplay/SkillTree/SkillTreePanel.prefab");panel.game=game;
            EditorUtility.SetDirty(game);EditorUtility.SetDirty(panel);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(modal.scene);EditorSceneManager.SaveScene(modal.scene);
            File.WriteAllText("ProductionLogs/SkillTree/integration-result.json","{\"status\":\"PASS\",\"nodes\":39,\"legacyListRetainedInactive\":true,\"cash\":"+game.Money+",\"prefabSceneReference\":false}");
        }
    }
}
