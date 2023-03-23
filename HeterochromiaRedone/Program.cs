using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Mutagen.Bethesda.Skyrim.Assets;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using System.ComponentModel;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using System.Text;
using System.Reflection;
using static HeterochromiaRedone.Structs;

namespace HeterochromiaRedone
{
    public class Program
    {
        public const string DefaultFileName = "HeterochromiaRedone.esp";

        public static string? FileName { get; set; }

        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                                  .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                                  .SetTypicalOpen(GameRelease.SkyrimSE, DefaultFileName)
                                  .Run(args);
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            foreach (var headPart in state.LoadOrder.PriorityOrder.HeadPart().WinningOverrides())
            {
                // Only want the eye parts, skip other stuff
                if (headPart.Type != HeadPart.TypeEnum.Eyes)
                    continue;

                if (headPart.ValidRaces == null)
                    continue;

                var leftHeadPart = headPart.DeepCopy();
                var rightHeadPart = headPart.DeepCopy();

                // Set all eyes as non-playable
                if (headPart.Flags.HasFlag(HeadPart.Flag.Playable))
                {
                    var unplayableHeadPart = headPart.DeepCopy();
                    unplayableHeadPart.Flags = unplayableHeadPart.Flags.SetFlag(HeadPart.Flag.Playable, false);
                    unplayableHeadPart = state.PatchMod.HeadParts.GetOrAddAsOverride(unplayableHeadPart);
                }

                // Add the texture sets to our own patch mod to prevent lots of masters
                var originalTextureSet = headPart.TextureSet.TryResolve(state.LinkCache);
                TextureSet? newTextureSet = null;
                if (originalTextureSet != null)
                {
                    newTextureSet = originalTextureSet.DeepCopy();
                    newTextureSet.EditorID = "_HR_" + originalTextureSet.EditorID;
                    newTextureSet = state.PatchMod.TextureSets.DuplicateInAsNewRecord(newTextureSet);
                }

                if (newTextureSet != null)
                    leftHeadPart.TextureSet = rightHeadPart.TextureSet = newTextureSet.ToNullableLink();

                string gender = headPart.Flags.HasFlag(HeadPart.Flag.Female) ? "Fem" : "Male";
                var raceType = DetermineRaceType(state, headPart);

                leftHeadPart.Name = leftHeadPart.EditorID = $"_HR_L_{headPart.EditorID}";
                rightHeadPart.Name = rightHeadPart.EditorID = $"_HR_R_{headPart.EditorID}";

                leftHeadPart.Model ??= new ();
                rightHeadPart.Model ??= new ();

                leftHeadPart.Model.Data = rightHeadPart.Model.Data = null;
                leftHeadPart.Model.File = new ()
                {
                    RawPath = (raceType, gender) switch
                    {
                        (RaceType.Argonian, _) => Constants.ArgonianEyes.Left.NifPath,
                        (RaceType.Khajiit, "Fem") => Constants.KhajiitFemaleEyes.Left.NifPath,
                        (RaceType.Khajiit, "Male") => Constants.KhajiitMaleEyes.Left.NifPath,
                        (_, "Fem") => Constants.NonBeastFemaleEyes.Left.NifPath,
                        (_, "Male") => Constants.NonBeastMaleEyes.Left.NifPath
                    }
                };
                rightHeadPart.Model.File = new ()
                {
                    RawPath = (raceType, gender) switch
                    {
                        (RaceType.Argonian, _) => Constants.ArgonianEyes.Right.NifPath,
                        (RaceType.Khajiit, "Fem") => Constants.KhajiitFemaleEyes.Right.NifPath,
                        (RaceType.Khajiit, "Male") => Constants.KhajiitMaleEyes.Right.NifPath,
                        (_, "Fem") => Constants.NonBeastFemaleEyes.Right.NifPath,
                        (_, "Male") => Constants.NonBeastMaleEyes.Right.NifPath
                    }
                };

                leftHeadPart.Flags |= HeadPart.Flag.Playable;
                rightHeadPart.Flags |= HeadPart.Flag.Playable;

                // Set head part type to 30 (left eye) or 31 (right eye)
                leftHeadPart.Type = (HeadPart.TypeEnum)30;
                rightHeadPart.Type = (HeadPart.TypeEnum)31;

                leftHeadPart.ExtraParts?.Clear();
                rightHeadPart.ExtraParts?.Clear();

                foreach (var part in leftHeadPart.Parts)
                {
                    if (part.PartType != Part.PartTypeEnum.Tri && part.PartType != Part.PartTypeEnum.ChargenMorph)
                        continue;

                    part.FileName = (raceType, gender, part.PartType) switch
                    {
                        (RaceType.Argonian, _, Part.PartTypeEnum.ChargenMorph) => Constants.ArgonianEyes.Left.ChargenPath,
                        (RaceType.Argonian, _, Part.PartTypeEnum.Tri) => Constants.ArgonianEyes.Left.TriPath,
                        (RaceType.Khajiit, "Fem", Part.PartTypeEnum.ChargenMorph) => Constants.KhajiitFemaleEyes.Left.ChargenPath,
                        (RaceType.Khajiit, "Fem", Part.PartTypeEnum.Tri) => Constants.KhajiitFemaleEyes.Left.TriPath,
                        (RaceType.Khajiit, "Male", Part.PartTypeEnum.ChargenMorph) => Constants.KhajiitMaleEyes.Left.ChargenPath,
                        (RaceType.Khajiit, "Male", Part.PartTypeEnum.Tri) => Constants.KhajiitMaleEyes.Left.TriPath,
                        (_, "Fem", Part.PartTypeEnum.ChargenMorph) => Constants.NonBeastFemaleEyes.Left.ChargenPath,
                        (_, "Fem", Part.PartTypeEnum.Tri) => Constants.NonBeastFemaleEyes.Left.TriPath,
                        (_, "Male", Part.PartTypeEnum.ChargenMorph) => Constants.NonBeastMaleEyes.Left.ChargenPath,
                        (_, "Male", Part.PartTypeEnum.Tri) => Constants.NonBeastMaleEyes.Left.TriPath,
                    };
                }

                foreach (var part in rightHeadPart.Parts)
                {
                    if (part.PartType != Part.PartTypeEnum.Tri && part.PartType != Part.PartTypeEnum.ChargenMorph)
                        continue;

                    part.FileName = (raceType, gender, part.PartType) switch
                    {
                        (RaceType.Argonian, _, Part.PartTypeEnum.ChargenMorph) => Constants.ArgonianEyes.Right.ChargenPath,
                        (RaceType.Argonian, _, Part.PartTypeEnum.Tri) => Constants.ArgonianEyes.Right.TriPath,
                        (RaceType.Khajiit, "Fem", Part.PartTypeEnum.ChargenMorph) => Constants.KhajiitFemaleEyes.Right.ChargenPath,
                        (RaceType.Khajiit, "Fem", Part.PartTypeEnum.Tri) => Constants.KhajiitFemaleEyes.Right.TriPath,
                        (RaceType.Khajiit, "Male", Part.PartTypeEnum.ChargenMorph) => Constants.KhajiitMaleEyes.Right.ChargenPath,
                        (RaceType.Khajiit, "Male", Part.PartTypeEnum.Tri) => Constants.KhajiitMaleEyes.Right.TriPath,
                        (_, "Fem", Part.PartTypeEnum.ChargenMorph) => Constants.NonBeastFemaleEyes.Right.ChargenPath,
                        (_, "Fem", Part.PartTypeEnum.Tri) => Constants.NonBeastFemaleEyes.Right.TriPath,
                        (_, "Male", Part.PartTypeEnum.ChargenMorph) => Constants.NonBeastMaleEyes.Right.ChargenPath,
                        (_, "Male", Part.PartTypeEnum.Tri) => Constants.NonBeastMaleEyes.Right.TriPath,
                    };
                }

                state.PatchMod.HeadParts.DuplicateInAsNewRecord(leftHeadPart);
                state.PatchMod.HeadParts.DuplicateInAsNewRecord(rightHeadPart);
            }

            var blankEyesSpecialLeftARMB = state.PatchMod.HeadParts.AddNew();
            blankEyesSpecialLeftARMB.EditorID = blankEyesSpecialLeftARMB.Name = "_HR_BlankEyesSpecial_L_AllRacesMinusBeast";
            blankEyesSpecialLeftARMB.Flags = HeadPart.Flag.Playable | HeadPart.Flag.Male | HeadPart.Flag.Female;
            blankEyesSpecialLeftARMB.Type = (HeadPart.TypeEnum)30;
            blankEyesSpecialLeftARMB.ValidRaces = Skyrim.FormList.HeadPartsAllRacesMinusBeast.AsNullable();
            state.PatchMod.HeadParts.Set(blankEyesSpecialLeftARMB);

            var blankEyesSpecialRightARMB = state.PatchMod.HeadParts.AddNew();
            blankEyesSpecialRightARMB.EditorID = blankEyesSpecialRightARMB.Name = "_HR_BlankEyesSpecial_R_AllRacesMinusBeast";
            blankEyesSpecialRightARMB.Flags = HeadPart.Flag.Playable | HeadPart.Flag.Male | HeadPart.Flag.Female;
            blankEyesSpecialRightARMB.Type = (HeadPart.TypeEnum)31;
            blankEyesSpecialRightARMB.ValidRaces = Skyrim.FormList.HeadPartsAllRacesMinusBeast.AsNullable();
            state.PatchMod.HeadParts.Set(blankEyesSpecialRightARMB);

            var blankEyesSpecialLeftArgonian = state.PatchMod.HeadParts.AddNew();
            blankEyesSpecialLeftArgonian.EditorID = blankEyesSpecialLeftArgonian.Name = "_HR_BlankEyesSpecial_L_Argonian";
            blankEyesSpecialLeftArgonian.Flags = HeadPart.Flag.Playable | HeadPart.Flag.Male | HeadPart.Flag.Female;
            blankEyesSpecialLeftArgonian.Type = (HeadPart.TypeEnum)30;
            blankEyesSpecialLeftArgonian.ValidRaces = Skyrim.FormList.HeadPartsArgonianandVampire.AsNullable();
            state.PatchMod.HeadParts.Set(blankEyesSpecialLeftArgonian);

            var blankEyesSpecialRightArgonian = state.PatchMod.HeadParts.AddNew();
            blankEyesSpecialRightArgonian.EditorID = blankEyesSpecialRightArgonian.Name = "_HR_BlankEyesSpecial_R_Argonian";
            blankEyesSpecialRightArgonian.Flags = HeadPart.Flag.Playable | HeadPart.Flag.Male | HeadPart.Flag.Female;
            blankEyesSpecialRightArgonian.Type = (HeadPart.TypeEnum)31;
            blankEyesSpecialRightArgonian.ValidRaces = Skyrim.FormList.HeadPartsArgonianandVampire.AsNullable();
            state.PatchMod.HeadParts.Set(blankEyesSpecialRightArgonian);

            var blankEyesSpecialLeftKhajiit = state.PatchMod.HeadParts.AddNew();
            blankEyesSpecialLeftKhajiit.EditorID = blankEyesSpecialLeftKhajiit.Name = "_HR_BlankEyesSpecial_L_Khajiit";
            blankEyesSpecialLeftKhajiit.Flags = HeadPart.Flag.Playable | HeadPart.Flag.Male | HeadPart.Flag.Female;
            blankEyesSpecialLeftKhajiit.Type = (HeadPart.TypeEnum)30;
            blankEyesSpecialLeftKhajiit.ValidRaces = Skyrim.FormList.HeadPartsKhajiitandVampire.AsNullable();
            state.PatchMod.HeadParts.Set(blankEyesSpecialLeftKhajiit);

            var blankEyesSpecialRightKhajiit = state.PatchMod.HeadParts.AddNew();
            blankEyesSpecialRightKhajiit.EditorID = blankEyesSpecialRightKhajiit.Name = "_HR_BlankEyesSpecial_R_Khajiit";
            blankEyesSpecialRightKhajiit.Flags = HeadPart.Flag.Playable | HeadPart.Flag.Male | HeadPart.Flag.Female;
            blankEyesSpecialRightKhajiit.Type = (HeadPart.TypeEnum)31;
            blankEyesSpecialRightKhajiit.ValidRaces = Skyrim.FormList.HeadPartsKhajiitandVampire.AsNullable();
            state.PatchMod.HeadParts.Set(blankEyesSpecialRightKhajiit);

            var blankEyesARMB = state.PatchMod.HeadParts.AddNew();
            blankEyesARMB.EditorID = blankEyesARMB.Name = "_HR_BlankEyes_AllRacesMinusBeast";
            blankEyesARMB.Flags = HeadPart.Flag.Playable;
            blankEyesARMB.Type = HeadPart.TypeEnum.Eyes;
            blankEyesARMB.ValidRaces = Skyrim.FormList.HeadPartsAllRacesMinusBeast.AsNullable();
            blankEyesARMB.Parts.Add(new ()
            {
                FileName = Constants.NonBeastFemaleEyes.Right.ChargenPath,
                PartType = Part.PartTypeEnum.ChargenMorph
            });
            state.PatchMod.HeadParts.Set(blankEyesARMB);

            var blankEyesArgonian = state.PatchMod.HeadParts.AddNew();
            blankEyesArgonian.EditorID = blankEyesArgonian.Name = "_HR_BlankEyes_Argonian";
            blankEyesArgonian.Flags = HeadPart.Flag.Playable;
            blankEyesArgonian.Type = HeadPart.TypeEnum.Eyes;
            blankEyesArgonian.ValidRaces = Skyrim.FormList.HeadPartsArgonianandVampire.AsNullable();
            blankEyesArgonian.Parts.Add(new ()
            {
                FileName = Constants.ArgonianEyes.Right.ChargenPath,
                PartType = Part.PartTypeEnum.ChargenMorph
            });
            state.PatchMod.HeadParts.Set(blankEyesArgonian);

            var blankEyesKhajiit = state.PatchMod.HeadParts.AddNew();
            blankEyesKhajiit.EditorID = blankEyesKhajiit.Name = "_HR_BlankEyes_Khajiit";
            blankEyesKhajiit.Flags = HeadPart.Flag.Playable;
            blankEyesKhajiit.Type = HeadPart.TypeEnum.Eyes;
            blankEyesKhajiit.ValidRaces = Skyrim.FormList.HeadPartsKhajiitandVampire.AsNullable();
            blankEyesKhajiit.Parts.Add(new ()
            {
                FileName = Constants.KhajiitFemaleEyes.Right.ChargenPath,
                PartType = Part.PartTypeEnum.ChargenMorph
            });
            state.PatchMod.HeadParts.Set(blankEyesKhajiit);

            FileName = state.PatchMod.ModKey.FileName;

            // Move the meshes and RaceMenu interface configuration to the data folder
            var parentDirectory = Directory.GetParent(state.OutputPath)!.FullName;
            Helpers.CopyDirectory(Path.Combine(state.InternalDataPath ?? "", "meshes"), Path.Combine(parentDirectory, "meshes"), true);
            Helpers.CopyDirectory(Path.Combine(state.InternalDataPath ?? "", "interface"), Path.Combine(parentDirectory, "interface"), true);

            // Rename folder to current ESP name so it works with Synthesis
            if(FileName != DefaultFileName)
                Directory.Move($"{parentDirectory}\\meshes\\actors\\character\\FaceGenMorphs\\{DefaultFileName}", $"{parentDirectory}\\meshes\\actors\\character\\FaceGenMorphs\\{FileName}");

            // Also rename translation file
            File.Move($"{parentDirectory}\\interface\\translations\\HeterochromiaRedone_english.txt", $"{parentDirectory}\\interface\\translations\\{state.PatchMod.ModKey.Name}_english.txt");

            // Write all races into the human ini
            StringBuilder iniBuilder = new ();
            foreach(var item in Skyrim.FormList.HeadPartsAllRacesMinusBeast.Resolve(state.LinkCache).Items)
            {
                var humanRace = item.TryResolve<IRaceGetter>(state.LinkCache);
                if (humanRace != null)
                    iniBuilder.AppendLine($"{humanRace.EditorID} = sliders/human.ini");
            }
            foreach(var item in Skyrim.FormList.HeadPartsArgonianandVampire.Resolve(state.LinkCache).Items)
            {
                var argonianRace = item.TryResolve<IRaceGetter>(state.LinkCache);
                if (argonianRace != null)
                    iniBuilder.AppendLine($"{argonianRace.EditorID} = sliders/human.ini");
            }
            foreach(var item in Skyrim.FormList.HeadPartsKhajiitandVampire.Resolve(state.LinkCache).Items)
            {
                var khajiitRace = item.TryResolve<IRaceGetter>(state.LinkCache);
                if (khajiitRace != null)
                    iniBuilder.AppendLine($"{khajiitRace.EditorID} = sliders/human.ini");
            }
            File.WriteAllText($"{parentDirectory}\\meshes\\actors\\character\\FaceGenMorphs\\{FileName}\\races.ini", iniBuilder.ToString());
        }

        private static RaceType DetermineRaceType(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, IHeadPartGetter headPart)
        {
            var raceList = headPart.ValidRaces.TryResolve(state.LinkCache);
            if (raceList == null)
                throw new Exception($"Head part {headPart.EditorID}|{headPart.FormKey} contains invalid ValidRaces record!");

            if (raceList.Items.Contains(Skyrim.Race.KhajiitRace) || raceList.Items.Contains(Skyrim.Race.KhajiitRaceVampire))
                return RaceType.Khajiit;
            else if (raceList.Items.Contains(Skyrim.Race.ArgonianRace) || raceList.Items.Contains(Skyrim.Race.ArgonianRaceVampire))
                return RaceType.Argonian;
            else
                return RaceType.AllRacesMinusBeasts;
        }
    }
}
