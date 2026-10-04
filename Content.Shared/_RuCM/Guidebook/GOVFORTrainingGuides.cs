namespace Content.Shared._RuCM.Guidebook;

/// <summary>Public reading references only; this mapping does not change training requirements or saved progress.</summary>
public static class GOVFORTrainingGuides
{
    public const string Root = "RuCMGOVFOR";
    public const string Basic = "RuCMGOVFORBasicDocuments";
    public const string Service = "RuCMGOVFORService";
    public const string Training = "RuCMGOVFORTraining";
    public const string Drill = "RuCMGOVFORDrillRegulations";
    public const string Communications = "RuCMGOVFORCommunications";
    public const string Command = "RuCMGOVFORUnitCommand";
    public const string Qualifications = "RuCMGOVFORTrainingQualifications";
    public const string Sop = "AU14SOP";
    public const string MilitaryCode = "AU14UCMJ";
    public const string RecruitCourse = "RuCMGOVFORRecruitCourse";
    public const string RecruitIntroduction = RecruitCourse + "Introduction";
    public const string RecruitMilitaryRegulations = RecruitCourse + "MilitaryRegulations";
    public const string RecruitDrill = RecruitCourse + "Drill";
    public const string RecruitCommunications = RecruitCourse + "Communications";
    public const string RecruitFirearms = RecruitCourse + "Firearms";
    public const string RecruitTactics = RecruitCourse + "Tactics";
    public const string RecruitFirstAid = RecruitCourse + "FirstAid";
    public const string RecruitTopography = RecruitCourse + "Topography";
    public const string RecruitEngineering = RecruitCourse + "EngineeringBasics";
    public const string RecruitFieldExercise = RecruitCourse + "FieldExercise";
    public const string RecruitReference = RecruitCourse + "Reference";

    public static bool IsReference(string id) => id is Root or Basic or Service or Training or Drill
        or Communications or Command or Qualifications or Sop or MilitaryCode or RecruitCourse or RecruitIntroduction
        or RecruitMilitaryRegulations or RecruitDrill or RecruitCommunications or RecruitFirearms or RecruitTactics
        or RecruitFirstAid or RecruitTopography or RecruitEngineering or RecruitFieldExercise or RecruitReference;

    /// <summary>Uses existing stable IDs, while also supporting the equivalent IDs in custom training programmes.</summary>
    public static string[] ForChecklistItem(string qualification, string item) => (qualification, item) switch
    {
        ("enlisted", "drill") => [Drill, RecruitDrill],
        ("enlisted", "communications") => [Communications, RecruitCommunications],
        ("enlisted", "military_regulations") => [Sop, MilitaryCode, Drill, RecruitMilitaryRegulations],
        ("enlisted", "firearms") => [RecruitFirearms],
        ("enlisted", "tactics") => [RecruitTactics],
        ("enlisted", "first_aid") => [RecruitFirstAid],
        ("enlisted", "topography") => [RecruitTopography],
        ("enlisted", "engineering_basics") => [RecruitEngineering],
        ("enlisted", "field_exercise") => [RecruitFieldExercise],
        ("sergeant", "drill") or ("officer", "drill") => [Drill],
        ("sergeant", "military_regulations")
            or ("officer", "military_regulations") => [Sop, MilitaryCode, Drill],
        ("sergeant", "tactics") or ("sergeant", "unit_command") or ("sergeant", "unit_management")
            or ("officer", "operations") or ("officer", "operational_management") => [Command],
        ("sergeant", "teaching") or ("sergeant", "methodical_training") => [Qualifications, Training],
        ("officer", "staff_work") => [Command, Sop],
        ("communications", "theory") or ("communications", "safety") or ("communications", "practice") => [Communications],
        _ => [],
    };
}
