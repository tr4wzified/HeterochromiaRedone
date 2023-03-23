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

namespace HeterochromiaRedone
{
    public class Program
    {
        public const string DefaultFileName = "HeterochromiaRedone.esp";

        public static string? HumanIni { get; set; }
        public static string? FileName { get; set; }

        public static async Task<int> Main(string[] args)
        {
            var generateEspTask = await SynthesisPipeline.Instance
                                  .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                                  .SetTypicalOpen(GameRelease.SkyrimSE, DefaultFileName)
                                  .Run(args);

            return generateEspTask;
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
                string race = DetermineRace(state, headPart);

                leftHeadPart.Name = leftHeadPart.EditorID = $"_HR_{gender}_{race}_Left_{headPart.EditorID}";
                rightHeadPart.Name = rightHeadPart.EditorID = $"_HR_{gender}_{race}_Right_{headPart.EditorID}";

                leftHeadPart.Model ??= new ();
                rightHeadPart.Model ??= new ();

                leftHeadPart.Model.Data = rightHeadPart.Model.Data = null;
                leftHeadPart.Model.File = new ()
                {
                    RawPath = (race, gender) switch
                    {
                        ("Arg", _) => Constants.ArgonianEyes.Left.NifPath,
                        ("Khaj", "Fem") => Constants.KhajiitFemaleEyes.Left.NifPath,
                        ("Khaj", "Male") => Constants.KhajiitMaleEyes.Left.NifPath,
                        (_, "Fem") => Constants.NonBeastFemaleEyes.Left.NifPath,
                        (_, "Male") => Constants.NonBeastMaleEyes.Left.NifPath
                    }
                };
                rightHeadPart.Model.File = new ()
                {
                    RawPath = (race, gender) switch
                    {
                        ("Arg", _) => Constants.ArgonianEyes.Right.NifPath,
                        ("Khaj", "Fem") => Constants.KhajiitFemaleEyes.Right.NifPath,
                        ("Khaj", "Male") => Constants.KhajiitMaleEyes.Right.NifPath,
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

                    part.FileName = (race, gender, part.PartType) switch
                    {
                        ("Arg", _, Part.PartTypeEnum.ChargenMorph) => Constants.ArgonianEyes.Left.ChargenPath,
                        ("Arg", _, Part.PartTypeEnum.Tri) => Constants.ArgonianEyes.Left.TriPath,
                        ("Khaj", "Fem", Part.PartTypeEnum.ChargenMorph) => Constants.KhajiitFemaleEyes.Left.ChargenPath,
                        ("Khaj", "Fem", Part.PartTypeEnum.Tri) => Constants.KhajiitFemaleEyes.Left.TriPath,
                        ("Khaj", "Male", Part.PartTypeEnum.ChargenMorph) => Constants.KhajiitMaleEyes.Left.ChargenPath,
                        ("Khaj", "Male", Part.PartTypeEnum.Tri) => Constants.KhajiitMaleEyes.Left.TriPath,
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

                    part.FileName = (race, gender, part.PartType) switch
                    {
                        ("Arg", _, Part.PartTypeEnum.ChargenMorph) => Constants.ArgonianEyes.Right.ChargenPath,
                        ("Arg", _, Part.PartTypeEnum.Tri) => Constants.ArgonianEyes.Right.TriPath,
                        ("Khaj", "Fem", Part.PartTypeEnum.ChargenMorph) => Constants.KhajiitFemaleEyes.Right.ChargenPath,
                        ("Khaj", "Fem", Part.PartTypeEnum.Tri) => Constants.KhajiitFemaleEyes.Right.TriPath,
                        ("Khaj", "Male", Part.PartTypeEnum.ChargenMorph) => Constants.KhajiitMaleEyes.Right.ChargenPath,
                        ("Khaj", "Male", Part.PartTypeEnum.Tri) => Constants.KhajiitMaleEyes.Right.TriPath,
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
            blankEyesSpecialLeftARMB.EditorID = blankEyesSpecialLeftARMB.Name = "_HR_BlankEyesSpecial_Left_AllRacesMinusBeast";
            blankEyesSpecialLeftARMB.Flags = HeadPart.Flag.Playable | HeadPart.Flag.Male | HeadPart.Flag.Female;
            blankEyesSpecialLeftARMB.Type = (HeadPart.TypeEnum)30;
            blankEyesSpecialLeftARMB.ValidRaces = Skyrim.FormList.HeadPartsAllRacesMinusBeast.AsNullable();
            state.PatchMod.HeadParts.Set(blankEyesSpecialLeftARMB);

            var blankEyesSpecialRightARMB = state.PatchMod.HeadParts.AddNew();
            blankEyesSpecialRightARMB.EditorID = blankEyesSpecialRightARMB.Name = "_HR_BlankEyesSpecial_Right_AllRacesMinusBeast";
            blankEyesSpecialRightARMB.Flags = HeadPart.Flag.Playable | HeadPart.Flag.Male | HeadPart.Flag.Female;
            blankEyesSpecialRightARMB.Type = (HeadPart.TypeEnum)31;
            blankEyesSpecialRightARMB.ValidRaces = Skyrim.FormList.HeadPartsAllRacesMinusBeast.AsNullable();
            state.PatchMod.HeadParts.Set(blankEyesSpecialRightARMB);

            var blankEyesSpecialLeftArgonian = state.PatchMod.HeadParts.AddNew();
            blankEyesSpecialLeftArgonian.EditorID = blankEyesSpecialLeftArgonian.Name = "_HR_BlankEyesSpecial_Left_Argonian";
            blankEyesSpecialLeftArgonian.Flags = HeadPart.Flag.Playable | HeadPart.Flag.Male | HeadPart.Flag.Female;
            blankEyesSpecialLeftArgonian.Type = (HeadPart.TypeEnum)30;
            blankEyesSpecialLeftArgonian.ValidRaces = Skyrim.FormList.HeadPartsArgonianandVampire.AsNullable();
            state.PatchMod.HeadParts.Set(blankEyesSpecialLeftArgonian);

            var blankEyesSpecialRightArgonian = state.PatchMod.HeadParts.AddNew();
            blankEyesSpecialRightArgonian.EditorID = blankEyesSpecialRightArgonian.Name = "_HR_BlankEyesSpecial_Right_Argonian";
            blankEyesSpecialRightArgonian.Flags = HeadPart.Flag.Playable | HeadPart.Flag.Male | HeadPart.Flag.Female;
            blankEyesSpecialRightArgonian.Type = (HeadPart.TypeEnum)31;
            blankEyesSpecialRightArgonian.ValidRaces = Skyrim.FormList.HeadPartsArgonianandVampire.AsNullable();
            state.PatchMod.HeadParts.Set(blankEyesSpecialRightArgonian);

            var blankEyesSpecialLeftKhajiit = state.PatchMod.HeadParts.AddNew();
            blankEyesSpecialLeftKhajiit.EditorID = blankEyesSpecialLeftKhajiit.Name = "_HR_BlankEyesSpecial_Left_Khajiit";
            blankEyesSpecialLeftKhajiit.Flags = HeadPart.Flag.Playable | HeadPart.Flag.Male | HeadPart.Flag.Female;
            blankEyesSpecialLeftKhajiit.Type = (HeadPart.TypeEnum)30;
            blankEyesSpecialLeftKhajiit.ValidRaces = Skyrim.FormList.HeadPartsKhajiitandVampire.AsNullable();
            state.PatchMod.HeadParts.Set(blankEyesSpecialLeftKhajiit);

            var blankEyesSpecialRightKhajiit = state.PatchMod.HeadParts.AddNew();
            blankEyesSpecialRightKhajiit.EditorID = blankEyesSpecialRightKhajiit.Name = "_HR_BlankEyesSpecial_Right_Khajiit";
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

            // Add compatibility for all races
            StringBuilder humanIniBuilder = new ();
            foreach(var race in state.LoadOrder.PriorityOrder.Race().WinningOverrides())
            {
                if (!race.Flags.HasFlag(Race.Flag.Playable))
                    continue;

                humanIniBuilder.AppendLine($"{race.EditorID} = sliders/human.ini");
            }
            HumanIni = humanIniBuilder.ToString();

            FileName = state.PatchMod.ModKey.FileName;

            // Rename folder to current ESP name so it works with Synthesis
            var currentDirectory = Directory.GetCurrentDirectory();
            if(FileName != DefaultFileName)
                Directory.Move($"{currentDirectory}\\meshes\\actors\\character\\FaceGenMorphs\\{DefaultFileName}", $"{currentDirectory}\\meshes\\actors\\character\\FaceGenMorphs\\{FileName}");

            // Write all human races into the human ini
            File.WriteAllText($"{currentDirectory}\\meshes\\actors\\character\\FaceGenMorphs\\{FileName}\\sliders\\human.ini", HumanIni);

            // Move the meshes and RaceMenu interface configuration to the data folder
            Helpers.CopyDirectory($"{currentDirectory}\\meshes", $"{state.DataFolderPath}\\meshes", true);
            Helpers.CopyDirectory($"{currentDirectory}\\interface", $"{state.DataFolderPath}\\interface", true);
        }

        private static string DetermineRace(IPatcherState<ISkyrimMod, ISkyrimModGetter> state, IHeadPartGetter headPart)
        {
            var raceList = headPart.ValidRaces.TryResolve(state.LinkCache);
            if (raceList == null)
                throw new Exception($"Head part {headPart.EditorID}|{headPart.FormKey} contains invalid ValidRaces record!");

            if (raceList.Items.Contains(Skyrim.Race.KhajiitRace) || raceList.Items.Contains(Skyrim.Race.KhajiitRaceVampire))
                return "Khaj";
            else if (raceList.Items.Contains(Skyrim.Race.ArgonianRace) || raceList.Items.Contains(Skyrim.Race.ArgonianRaceVampire))
                return "Arg";
            else
                return "ARMB";
        }
    }
}
