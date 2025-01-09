using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Starfield;
using Noggog;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.FormKeys.Starfield;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Noggog.StructuredStrings;

namespace dsm_testing
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<IStarfieldMod, IStarfieldModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.Starfield, "YourPatcher.esp")
                .Run(args);
        }

        async public static void RunPatch(IPatcherState<IStarfieldMod, IStarfieldModGetter> state)
        {
            if (state.LoadOrder.TryGetValue("DarkStar_Manufacturing.esm", out var DSM))
            {
                System.Console.WriteLine("found DarkStar Manufacturing, can continue patching");
            }
            else
            {
                System.Console.WriteLine("Could not find the DarkStar Manufacturing mod. Check your load order.");
                return;
            }

            if (state.LoadOrder.TryGetValue("cosplay_workbench.esm", out var cosplay))
            {
                System.Console.WriteLine("found DarkStar Cosplay, can continue patching");
            }
            else
            {
                System.Console.WriteLine("Could not find the DarkStar Cosplay Workbenches mod. Check your load order.");
                return;
            }

            await state.PatchMod.BeginWrite.ToPath(state.OutputPath).WithDefaultLoadOrder().WriteAsync() ;

            ILinkCache linkCache = state.LinkCache;

            // -------------------------------------------------------------------------------------------------
            // Patch headware / hats
            // -------------------------------------------------------------------------------------------------

            if (!Starfield.Keyword.ArmorTypeApparelHead.TryResolve(linkCache, out var head))
            {
                return;
            }
            if (!linkCache.TryResolve<IKeywordGetter>("ma_DSM_Headwear", out var dsmHeadwear))
            {
                return;
            }

            // -------------------------------------------------------------------------------------------------
            // Patch apparel / clothing
            // -------------------------------------------------------------------------------------------------

            if (!Starfield.Keyword.ArmorTypeApparelOrNakedBody.TryResolve(linkCache, out var apparel))
            {
                return;
            }
            if (!linkCache.TryResolve<IKeywordGetter>("ma_DSM_Apparel", out var dsmApparel))
            {
                return;
            }

            // -------------------------------------------------------------------------------------------------
            // Patch Neuroamps / Cyberware
            // -------------------------------------------------------------------------------------------------

            if (!Starfield.Keyword.ArmorTypeNeuroamp.TryResolve(linkCache, out var neuroamp))
            {
                return;
            }
            if (!linkCache.TryResolve<IKeywordGetter>("ma_DSM_Cyberware", out var dsmCyberware))
            {
                return;
            }

            // -------------------------------------------------------------------------------------------------
            // Patch Packs
            // -------------------------------------------------------------------------------------------------

            if (!Starfield.Keyword.ArmorTypeSpacesuitBackpack.TryResolve(linkCache, out var pack))
            {
                return;
            }
            if (!linkCache.TryResolve<IKeywordGetter>("ma_DSM_Backpack", out var dsmBackpack))
            {
                return;
            }

            // -------------------------------------------------------------------------------------------------
            // Patch Spacesuits
            // -------------------------------------------------------------------------------------------------

            if (!Starfield.Keyword.ArmorTypeSpacesuitBody.TryResolve(linkCache, out var spacesuit))
            {
                return;
            }
            if (!linkCache.TryResolve<IKeywordGetter>("ma_DSM_Spacesuit", out var dsmSpacesuit))
            {
                return;
            }

            // -------------------------------------------------------------------------------------------------
            // Patch Helmets
            // -------------------------------------------------------------------------------------------------

            if (!Starfield.Keyword.ArmorTypeSpacesuitHelmet.TryResolve(linkCache, out var helmet))
            {
                return;
            }
            if (!linkCache.TryResolve<IKeywordGetter>("ma_DSM_Helmet", out var dsmHelmet))
            {
                return;
            }

            //if (!Starfield.Keyword.ArmorTypeSpacesuitShowAlways.TryResolve(linkCache, out var always))
            //{
            //    return;
            //}


            // -------------------------------------------------------------------------------------------------
            // done with initial prep, let's find some stuff to patch...
            // -------------------------------------------------------------------------------------------------

            if (!linkCache.TryResolve<IKeywordGetter>("if_Crafting_ForceQuality01", out var dsmIfCraftingForceQuality01))
            {
                return;
            }

            Dictionary<FormKey,IResourceGetter> resourceMap = new Dictionary<FormKey, IResourceGetter> ();

            foreach (var resource in state.LoadOrder.PriorityOrder.WinningOverrides<IResourceGetter>())
                resourceMap[resource.FormKey] = resource;

            Dictionary<FormKey, IMiscItemGetter> miscItemMap = new Dictionary<FormKey, IMiscItemGetter>();

            foreach (var miscItem in state.LoadOrder.PriorityOrder.WinningOverrides<IMiscItemGetter>())
                miscItemMap[miscItem.FormKey] = miscItem;

            Dictionary<FormKey,IArmorGetter> armorMap = new Dictionary<FormKey, IArmorGetter> ();

            foreach (var armor in state.LoadOrder.PriorityOrder.WinningOverrides<IArmorGetter>())
                armorMap[armor.FormKey] = armor;

            var raceMap = new Dictionary<FormKey, IRaceGetter> ();
            foreach (var race in state.LoadOrder.PriorityOrder.WinningOverrides<IRaceGetter>())
                raceMap[race.FormKey] = race;

            if (!linkCache.TryResolve<IRaceGetter>("HumanRace", out var humanRace))
            {
                return;
            }

            Dictionary<FormKey,FormKey> dsmFromVanillaIoMkII = new Dictionary<FormKey, FormKey> ();
            Dictionary<FormKey, IConstructibleObjectGetter> dsmRecipes = new Dictionary<FormKey, IConstructibleObjectGetter>();
            Dictionary<FormKey,List<FormKey>> vanillaRecipes = new Dictionary<FormKey, List<FormKey>> ();

            foreach (var constructRecipe in state.LoadOrder.PriorityOrder.WinningOverrides<IConstructibleObjectGetter>()
                .Select(r => new {
                    recipe = r,
                    eid = r.EditorID,
                    createdObject = r.CreatedObject,
                    outputCount = r.AmountProduced,
                    inputObjects = r.ConstructableComponents,
                    workbenchKeyword = r.WorkbenchKeyword.TryResolve(linkCache),
                    formKey = r.FormKey,
                })
                .Where(r => r.workbenchKeyword != null)
                .Where(r => r.workbenchKeyword?.FormKey == Starfield.Keyword.WorkbenchIndustrialKeyword?.FormKey)
                .Where(r => !r.eid.IsNullOrEmpty())
                )
            {
                if (constructRecipe.inputObjects == null) {
                    System.Console.WriteLine($"found recipe {constructRecipe.eid} with no input components");
                    continue; 
                }

                if (
                    constructRecipe.inputObjects.Count == 1 &&
                    armorMap.ContainsKey(constructRecipe.createdObject.FormKey) && armorMap.ContainsKey(constructRecipe.inputObjects[0].Component.FormKey)
                    )
                {
                    dsmFromVanillaIoMkII.Add(constructRecipe.inputObjects[0].Component.FormKey, constructRecipe.createdObject.FormKey);
                    dsmRecipes.Add(constructRecipe.createdObject.FormKey, constructRecipe.recipe);
                    //System.Console.WriteLine($"enhancer? Goes from armor from {armorMap[constructRecipe.inputObjects[0].Component.FormKey].EditorID} to {armorMap[constructRecipe.createdObject.FormKey].EditorID}");
                    //System.Console.WriteLine($"recipe CIFK: {constructRecipe.recipe.InstantiationFilterKeyword}");
                }
                else
                {
                    foreach (var input in constructRecipe.inputObjects)
                    {
                        if (vanillaRecipes.ContainsKey(constructRecipe.createdObject.FormKey))
                        {
                            vanillaRecipes[constructRecipe.createdObject.FormKey].Add(input.Component.FormKey);
                        }
                        else
                        {
                            vanillaRecipes.Add(constructRecipe.createdObject.FormKey, new List<FormKey>());
                            vanillaRecipes[constructRecipe.createdObject.FormKey].Add(input.Component.FormKey);
                        }
                    }
                }
            }

            var armorsWithRecipes = dsmFromVanillaIoMkII.Values.Union(vanillaRecipes.Keys);
            var armorsWithoutRecipes = from armor in armorMap.Keys.Except(armorsWithRecipes) select armor;
            if (armorsWithoutRecipes.Any() )
            {
                //System.Console.WriteLine("found ARMO records without construction recipes");
                foreach (var armor in armorsWithoutRecipes)
                {
                    var a = armorMap[armor];
                    //System.Console.WriteLine($"{armor} ({a.EditorID}) cannot be constructed at a workbench");
                    if (
                        (a.Race?.FormKey.Equals(humanRace.FormKey) ?? false) &&
                        (a.AttachParentSlots?.Any() ?? false) &&
                        (!a.MajorFlags.HasFlag(Armor.MajorFlag.NonPlayable)) &&
                        (!a.FormKey.ModKey.FileName.String.Equals("starfield.esm", StringComparison.OrdinalIgnoreCase)) &&
                        (!a.FormKey.ModKey.FileName.String.Equals("DarkStar_Manufacturing.esm", StringComparison.OrdinalIgnoreCase))
                        )
                    {
                        System.Console.WriteLine($"{a.EditorID} (at {armor}) cannot be constructed at a workbench but but (a) it's human (b) has attach parent slots (c) is marked playable");
                    }
                }
            }

            foreach (var pair in dsmFromVanillaIoMkII)
            {
                var vanillaArmor = armorMap[pair.Key];
                var mk2Armor = armorMap[pair.Value];

                if (!dsmRecipes[mk2Armor.FormKey].InstantiationFilterKeyword.Equals(dsmIfCraftingForceQuality01))
                {
                    System.Console.WriteLine($"** WARNING ** found DSM MkII recipe {dsmRecipes[mk2Armor.FormKey].EditorID} with unexpected instantiation filter keyword {dsmRecipes[mk2Armor.FormKey].InstantiationFilterKeyword}");
                }
                else
                {
                    if (mk2Armor.Keywords?.Contains(dsmHeadwear) ?? false)
                    {
                        System.Console.WriteLine($"found DSM MkII headware recipe converting {vanillaArmor.EditorID} into {mk2Armor.EditorID}");
                    }
                    else if (mk2Armor.Keywords?.Contains(apparel) ?? false)
                    {
                        System.Console.WriteLine($"found DSM MkII apparel recipe converting {vanillaArmor.EditorID} into {mk2Armor.EditorID}");
                    }
                    else if (mk2Armor.Keywords?.Contains(neuroamp) ?? false)
                    {
                        System.Console.WriteLine($"found DSM MkII neuroamp recipe converting {vanillaArmor.EditorID} into {mk2Armor.EditorID}");
                    }
                    else if (mk2Armor.Keywords?.Contains(pack) ?? false)
                    {
                        System.Console.WriteLine($"found DSM MkII pack recipe converting {vanillaArmor.EditorID} into {mk2Armor.EditorID}");
                    }
                    else if (mk2Armor.Keywords?.Contains(spacesuit) ?? false)
                    {
                        System.Console.WriteLine($"found DSM MkII spacesuit recipe converting {vanillaArmor.EditorID} into {mk2Armor.EditorID}");
                    }
                    else if (mk2Armor.Keywords?.Contains(helmet) ?? false)
                    {
                        System.Console.WriteLine($"found DSM MkII helmet recipe converting {vanillaArmor.EditorID} into {mk2Armor.EditorID}");
                    }
                    else
                    {
                        System.Console.WriteLine($"found unknown one-to-one ARMO recipe converting {vanillaArmor.EditorID} into {mk2Armor.EditorID}");
                    }
                }
            }
        }
    }
}
