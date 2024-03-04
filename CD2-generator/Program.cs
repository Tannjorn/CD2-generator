using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using CsvHelper;
using CsvHelper.Configuration;


// Classes defined for reading VsoPost 602 reference set
public class VsoPost
{
    [JsonProperty("items")]
    public List<VsoPostMap> VsoPostMaps { get; set; }
}

public class VsoPostMap
{

    [JsonProperty("referencedComponentId")]
    public string VsoSctId { get; set; }
    [JsonProperty("additionalFields")]
    public AdditionalFields additionalField { get; set; }
    public string LmmId { get; internal set; }
    public class AdditionalFields
    {
        [JsonProperty("mapTarget")]
        public string LmmId { get; set; }
    }
}

// Classes for reading relationships for a Clinical Drug
// First to define where and what to look for in the JSON response from Snowstorm
public class Relationship
{
    [JsonProperty("typeId")]
    public string TypeId { get; set; }

    [JsonProperty("destinationId")]
    public string DestinationId { get; set; }

    [JsonProperty("concreteValue")]
    public ConcreteValueProperties? ConcreteValue { get; set; }

    public class ConcreteValueProperties
    {
        [JsonProperty("value")]
        public string? Value { get; set; }
    }

    // groupId is used to set the correct combinations of PAI, BOSS and strength values and units. Necessary when searching drugs with multiple substances.
    [JsonProperty("groupId")]
    public int GroupId { get; set; }

    public void AssignCdProperties(CdProperties.GroupProperties groupProperties)
    {
        switch (TypeId)
        {
            case "411116001":
                groupProperties.ManDoseForm = DestinationId;
                break;

            case "762949000":
                groupProperties.PAI = DestinationId;
                break;

            case "763032000":
                groupProperties.UnitPres = DestinationId;
                break;

            case "1142139005":
                groupProperties.CountIng = ConcreteValue.Value;
                break;

            case "732943007":
                groupProperties.BOSS = DestinationId;
                break;

            case "1142135004":
                groupProperties.PresStrengthNumVal = ConcreteValue.Value;
                break;

            case "732945000":
                groupProperties.PresStrengthNumUnit = DestinationId;
                break;

            case "1142136003":
                groupProperties.PresStrengthDenVal = ConcreteValue.Value;
                break;

            case "732947008":
                groupProperties.PresStrengthDenUnit = DestinationId;
                break;

            case "1142138002":
                groupProperties.ConcStrengthNumVal = ConcreteValue.Value;
                break;

            case "733725009":
                groupProperties.ConcStrengthNumUnit = DestinationId;
                break;

            case "1142137007":
                groupProperties.ConcStrengthDenVal = ConcreteValue.Value;
                break;

            case "733722007":
                groupProperties.ConcStrengthDenUnit = DestinationId;
                break;

            default:
                break;
        }
    }

}
// Second define the different 
public class CdProperties
{
    [JsonProperty("items")]
    public List<Relationship> Relationships { get; set; }
    public Dictionary<int, GroupProperties> GroupedCdProperties { get; } = new Dictionary<int, GroupProperties>();

    public class GroupProperties
    {

        public string ManDoseForm { get; set; }

        public string UnitPres { get; set; }

        public string? CountIng { get; set; }

        public string PAI { get; set; }

        public string BOSS { get; set; }

        public string? PresStrengthNumVal { get; set; }

        public string? PresStrengthNumUnit { get; set; }

        public string? PresStrengthDenVal { get; set; }

        public string? PresStrengthDenUnit { get; set; }

        public string? ConcStrengthNumVal { get; set; }

        public string? ConcStrengthNumUnit { get; set; }

        public string? ConcStrengthDenVal { get; set; }

        public string? ConcStrengthDenUnit { get; set; }
    }
}

//Classes for looking up siblings. Siblings are LmmIds with similar clinical features that for this use case can be combined under a single Clinical Drug, but in map-automation is mapped to different Clinical Drugs.
public class Siblings
{
    [JsonProperty("total")]
    public string SibCount { get; set; }

}

public class SiblingMap
{
    public List<SiblingResult> SiblingResults { get; set; }
}

public class SiblingResult
{
    public string SibSctId { get; set; }
    public string LmmId { get; set; }
    public string SibCount { get; set; }

}
// Classes to find and store LmmIds that have shifted mapping because of the use case to combine LmmIds with similar clinical features.
public class Cd2Map
{
    [JsonProperty("items")]
    public List<Cd2Result> Cd2Results { get; set; }
}
public class Cd2Result
{
    [JsonProperty("conceptId")]
    public string Cd2Sct { get; set; }

    public string LmmId { get; set; }
}

// Classes for the updated map

public class UpdatedMap
{
    public List<UpdatedCd> updatedCds { get; set; }
}

public class UpdatedCd
{
    public string SctId { get; set; }
    public string LmmId { get; set; }
}

// Classes for Pharmaceutical Dose Form properties
public class DoseFormProps
{
    [JsonProperty("items")]
    public List<Relationship> Relationships { get; set; }

    public string BasicDoseForm { get; set; }
    public string IntendedSite { get; set; }
    public string Administration { get; set; }
    public string ReleaseCharacteristics { get; set; }
    public string Transformation { get; set; }
    public void SetProperty(string typeId, string destinationId, Relationship.ConcreteValueProperties concreteValue)
    {
        switch (typeId)
        {
            case "736476002":
                BasicDoseForm = destinationId;
                break;

            case "736474004":
                IntendedSite = destinationId;
                break;

            case "736472000":
                Administration = destinationId;
                break;

            case "736475003":
                ReleaseCharacteristics = destinationId;
                break;

            case "736473005":
                Transformation = destinationId;
                break;
        }
    }
}

// Define classes for listing Clinical Drugs that should be checked for an Inject/Infuse CD2
public class NeedInjInfMap
{
    [JsonProperty("items")]
    public List<NeedInjInfCd> needInjInfCds { get; set; }
}

public class NeedInjInfCd
{
    [JsonProperty("conceptId")]
    public string SctId { get; set; }
    public string LmmId { get; set; }
}

public class FinalCd2Map
{
    public List<FinalCd2> finalCd2s { get; set; }
}

public class FinalCd2
{
    public string SctId { get; set; }
    public string LmmId { get; set; }
}

class Program
{
    // Environmental variables. Can be set in application.json or changed here. Server auth is not set.
    public static string baseUrl = "https://slv.terminologi.ehelse.no/"; //Snowstorm server URL
    public static string refsetBranch = "MAIN/SNOMEDCT-NO/SLVMAPS/VSOPOST/"; //Branch for refsets and content
    public static string cdBranch = "MAIN/SNOMEDCT-NO/"; //Branch for concept search
    public static string vsoPostId = "6021000202106"; //Reference Set ID for automated posting from NOMA
    public static string cd2MapId = "187581000202109"; //Reference Set ID for automatically generated CD-groupers
    public static string lmmCdMapId = "88791000202108"; //Reference Set ID for main set to publish to FAT
    public static string overrideMapId = "187571000202107"; //Reference Set ID for manual override of LMM to CD mappings

    static async Task Main(string[] args)
    {
        // Define lists as necessary
        List<Cd2Result> cd2Results = new();
        List<UpdatedCd> updatedCds = new();
       // List<NeedInjInfCd> needInjInfCds = new();
        List<Cd2Result> injInfCd2Results = new();
        List<FinalCd2> finalCd2s = new();


        List<VsoPostMap> vsoPostMaps = await LookUpVSOPOST();

        string csvExportPathInitial = "c:\\Temp\\intialCdcsvb.csv";
        using (var writer = new StreamWriter(csvExportPathInitial))
        using (var csv = new CsvWriter(writer, new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
        {
            Delimiter = ";"
        }))
        {
            csv.WriteRecords(vsoPostMaps);
        }


        // Add properties for each CD, look for siblings and compile new complete map
        foreach (var vsoPostMap in vsoPostMaps)
        {
            // First step, look-up properties
            CdProperties cdProperties = await LookUpCdProperties(vsoPostMap.VsoSctId);

            //Second step, find siblings
            List<SiblingResult> siblingResults = await LookUpSiblings(vsoPostMap, cdProperties);

            foreach (var siblingResult in siblingResults)
            {
                Console.WriteLine($"LmmId {siblingResult.LmmId} to {siblingResult.SibSctId} has {siblingResult.SibCount} siblings");
            }

            // Using a foreach to only activate the method if siblings are found.
            foreach (var siblingResult in siblingResults)
            {
                //Console.WriteLine("Just checking the method is activated"); // DELETE WHEN WORKING. This is only to check that the method is called and has something to start with.

                List<Cd2Result> currentCd2Results = await LookUpCd2(siblingResult, cdProperties);
                cd2Results.AddRange(currentCd2Results);
            }

            List<UpdatedCd> currentAfterStrengthCds = await CompileAfterStrengtMap(vsoPostMap, cd2Results);
            updatedCds.AddRange(currentAfterStrengthCds);

        }

        Console.WriteLine("This should now print CD2s for drugs that need one based on strength");

        foreach (var cd2Result in cd2Results)
        {
            Console.WriteLine($"CD2 for strength LmmId: {cd2Result.LmmId}, SctId: {cd2Result.Cd2Sct}");
        }

        Console.WriteLine("This should now print the complete mad after changing to CD2 when needed for strength");

        foreach (var updatedCd in updatedCds)
        {
            Console.WriteLine($"AFTER STRENGTH LmmId: {updatedCd.LmmId}, SctId: {updatedCd.SctId}");
            string resultsStrength = $"Map after strength |{updatedCd.LmmId}|{updatedCd.SctId}";
            WriteErrorsToCsv(resultsStrength, "C:\\temp\\afterStrengthMap.csv");
        }

        foreach (var updatedCd in updatedCds)
        {
            CdProperties injInfPropResult = await LookUpInjInfProps(updatedCd);
            DoseFormProps doseFormProps = await LookUpDoseFormProps(injInfPropResult);
            List<SiblingResult> injInfSiblingResults = await LookUpInjInjSiblings(updatedCd, injInfPropResult, doseFormProps);

            foreach (var injInfSiblingResult in injInfSiblingResults)
            {
                List<Cd2Result> currenctInjInfCd2Results = await LookUpInjInfCd2(updatedCd, injInfPropResult, doseFormProps, injInfSiblingResult);
                injInfCd2Results.AddRange(currenctInjInfCd2Results);
            }
        }

        Console.WriteLine("This should now print CD2s for drugs that need one based on inj_inf");

        foreach (var injInfCd2Result in injInfCd2Results)
        {
            Console.WriteLine($"AFTER INJ_INF LmmId: {injInfCd2Result.LmmId}, SctId: {injInfCd2Result.Cd2Sct}");
            string resultsInjInf = $"Siblings based on strength for|{injInfCd2Result.LmmId}|{injInfCd2Result.Cd2Sct}";
            WriteErrorsToCsv(resultsInjInf, "C:\\temp\\changedMapAfterInjInf.csv");
        }

        foreach (var updatedCd in updatedCds)
        {
            List<FinalCd2> currentFinalCd2s = await CompileFinalCd2Map(updatedCd, injInfCd2Results);
            finalCd2s.AddRange(currentFinalCd2s);
        }

        Console.WriteLine("This should now print the final map");

        foreach (var finalCd2 in finalCd2s)
        {
            Console.WriteLine($"FinalCd2Map LmmId: {finalCd2.LmmId}, SctId: {finalCd2.SctId}");
            string final = $"{finalCd2.LmmId}|{finalCd2.SctId}";
            WriteErrorsToCsv(final, "C:\\temp\\finalCd2.csv");

        }

        string csvExportPathFinal = "c:\\Temp\\finalCd2bcsv.csv";
        using (var writer = new StreamWriter(csvExportPathFinal))
        using (var csv = new CsvWriter(writer, new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
        {
            Delimiter = ";"
        }))
        {
            csv.WriteRecords(finalCd2s);
        }


    }

    static void WriteErrorsToCsv(string message, string csvFilePath)
    {
        using (StreamWriter writer = new StreamWriter(csvFilePath, true))
        {
            writer.WriteLine(message);
        }
    }

    // LookUp all CDs from VSOPOST
    static async Task<List<VsoPostMap>> LookUpVSOPOST()
    {
        List<VsoPostMap> vsoPostMaps = new();

        string vsoPostUrl = baseUrl + refsetBranch + "members?referenceSet=" + vsoPostId + "&limit=9999&active=true";
        HashSet<(string, string)> seenPairs = new();

        try
        {
            using HttpClient client = new();

            HttpResponseMessage vsoPostResponse = await client.GetAsync(vsoPostUrl);

            if (vsoPostResponse.IsSuccessStatusCode)
            {
                string jsonVsoPost = await vsoPostResponse.Content.ReadAsStringAsync();

                //Console.WriteLine(jsonVsoPost); // DELETE AFTER TESTING. Only to show that the look-up works

                var vsoPostResult = JsonConvert.DeserializeObject<VsoPost>(jsonVsoPost);
                //Console.WriteLine($"Test: {vsoPostResult}");

                if (vsoPostResult?.VsoPostMaps != null && vsoPostResult.VsoPostMaps.Count > 0)
                {
                    foreach (var vsoPostMap in vsoPostResult.VsoPostMaps)
                    {

                        if (!seenPairs.Contains((vsoPostMap.VsoSctId, vsoPostMap.additionalField.LmmId)))
                        {
                            seenPairs.Add((vsoPostMap.VsoSctId, vsoPostMap.additionalField.LmmId));

                            vsoPostMaps.Add(new VsoPostMap
                            {
                                VsoSctId = vsoPostMap.VsoSctId,
                                LmmId = vsoPostMap.additionalField.LmmId
                            }
                            );
                            Console.WriteLine($"LM-map: {vsoPostMap.additionalField.LmmId} koblet til {vsoPostMap.VsoSctId}");
                            string initial = $"{vsoPostMap.additionalField.LmmId}|{vsoPostMap.VsoSctId}";
                            WriteErrorsToCsv(initial, "c:\\temp\\initialMap.csv");

                        }


                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
        }

        return vsoPostMaps;
    }

    // Lookup properties for each CD
    static async Task<CdProperties> LookUpCdProperties(string vsoSctId)
    {
        CdProperties cdProperties = new();

        // Construct the URL with vsoSctId and perform the HTTP request for additional properties
        string propertiesCdUrl = $"{baseUrl}{refsetBranch}relationships?source={vsoSctId}&active=true";


        //Console.WriteLine($"{propertiesCdUrl}");
        try
        {
            using HttpClient client = new();
            HttpResponseMessage propResponse = await client.GetAsync(propertiesCdUrl);

            if (propResponse.IsSuccessStatusCode)
            {
                string additionalJson = await propResponse.Content.ReadAsStringAsync();
                cdProperties = JsonConvert.DeserializeObject<CdProperties>(additionalJson);

                var relationships = cdProperties.Relationships;

                foreach (var relationship in relationships)
                {
                    if (!cdProperties.GroupedCdProperties.ContainsKey(relationship.GroupId))
                        cdProperties.GroupedCdProperties[relationship.GroupId] = new CdProperties.GroupProperties();

                    relationship.AssignCdProperties(cdProperties.GroupedCdProperties[relationship.GroupId]);
                }

            }
        }

        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
        }

        Console.WriteLine($"Test PAI 1: {cdProperties.GroupedCdProperties[1].PAI}");
        if (cdProperties.GroupedCdProperties.ContainsKey(2))
        {
            Console.WriteLine($"Test PAI 2: {cdProperties.GroupedCdProperties[2].PAI}");
        }
        if (cdProperties.GroupedCdProperties.ContainsKey(3))
        {
            Console.WriteLine($"Test PAI 3: {cdProperties.GroupedCdProperties[3].PAI}");
        }


        return cdProperties;
    }

    // Use property-based ECL-query to look for "siblings". Siblings are clinical drugs with similar clinical properties. The query is filtered for clinical drugs that are sources for a target LmmId
    static async Task<List<SiblingResult>> LookUpSiblings(VsoPostMap vsoPostMaps, CdProperties cdProperties)

    {
        List<SiblingResult> siblingResults = new();

        //LookUp possible siblings for Clinical Drugs with presentation strength
        if (cdProperties.GroupedCdProperties[0].UnitPres != null) // && cdProperties.GroupedCdProperties[0].CountIng == "1") // The second condition restricts the method to process only single substance drugs
        {
            StringBuilder sibUrl = new StringBuilder($"{baseUrl}{refsetBranch}concepts?ecl=((<< 763158003:411116001={cdProperties.GroupedCdProperties[0].ManDoseForm},763032000={cdProperties.GroupedCdProperties[0].UnitPres},1142139005=%23{cdProperties.GroupedCdProperties[0].CountIng},");

            if (cdProperties.GroupedCdProperties.ContainsKey(1))
            {
                sibUrl.Append("{");
                sibUrl.Append($"732943007={cdProperties.GroupedCdProperties[1].BOSS},1142135004=%23{cdProperties.GroupedCdProperties[1].PresStrengthNumVal},732945000={cdProperties.GroupedCdProperties[1].PresStrengthNumUnit},1142136003=%23{cdProperties.GroupedCdProperties[1].PresStrengthDenVal},732947008={cdProperties.GroupedCdProperties[1].PresStrengthDenUnit}");
                sibUrl.Append("}");
            }

            if (cdProperties.GroupedCdProperties.ContainsKey(2))
            {
                sibUrl.Append(",{");
                sibUrl.Append($"732943007={cdProperties.GroupedCdProperties[2].BOSS},1142135004=%23{cdProperties.GroupedCdProperties[2].PresStrengthNumVal},732945000={cdProperties.GroupedCdProperties[2].PresStrengthNumUnit},1142136003=%23{cdProperties.GroupedCdProperties[2].PresStrengthDenVal},732947008={cdProperties.GroupedCdProperties[2].PresStrengthDenUnit}");
                sibUrl.Append("}");
            }

            if (cdProperties.GroupedCdProperties.ContainsKey(3))
            {
                sibUrl.Append(",{");
                sibUrl.Append($"732943007={cdProperties.GroupedCdProperties[3].BOSS},1142135004=%23{cdProperties.GroupedCdProperties[3].PresStrengthNumVal},732945000={cdProperties.GroupedCdProperties[3].PresStrengthNumUnit},1142136003=%23{cdProperties.GroupedCdProperties[3].PresStrengthDenVal},732947008={cdProperties.GroupedCdProperties[3].PresStrengthDenUnit}");
                sibUrl.Append("}");
            }

            sibUrl.Append($") MINUS {vsoPostMaps.VsoSctId}) AND %5E{vsoPostId}");

            string sibUrlString = sibUrl.ToString();

            Console.WriteLine(sibUrlString);

            try
            {
                using HttpClient client = new();
                HttpResponseMessage sibResponse = await client.GetAsync(sibUrlString);

                if (sibResponse.IsSuccessStatusCode)
                {
                    string sibJson = await sibResponse.Content.ReadAsStringAsync();
                    var sibResult = JsonConvert.DeserializeObject<Siblings>(sibJson);

                    if (int.TryParse(sibResult.SibCount, out int sibCount) && sibCount > 0)
                    {
                        siblingResults.Add(new SiblingResult
                        {
                            LmmId = vsoPostMaps.LmmId,
                            SibSctId = vsoPostMaps.VsoSctId,
                            SibCount = sibResult.SibCount
                        }
                            );
                    }

                    Console.WriteLine($"Number of siblings for {vsoPostMaps.LmmId}: {sibResult.SibCount}");
                    string results = $"Siblings based on strength for |{vsoPostMaps.LmmId}|{sibResult.SibCount}|{sibUrlString}";
                    WriteErrorsToCsv(results, "C:\\temp\\siblingsStrength.csv");

                }
                else
                {
                    Console.WriteLine($"Error looking up siblings, lookup for presentation strength was unsuccessful for {sibUrlString}");
                    string errorMessage = $"Server returned unsuccessful trying to lookup siblings for |{vsoPostMaps.LmmId}|{vsoPostMaps.VsoSctId}|{sibUrlString}|{sibResponse.StatusCode}";
                    WriteErrorsToCsv(errorMessage, "C:\\temp\\errorsInSiblingEcl.csv");
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Exception: {ex.Message}");
            }
            //Console.WriteLine(sibUrl);
        }


        //LookUp LMM-ID to Clinical Drug maps with concentration strength
        else if (cdProperties.GroupedCdProperties[0].UnitPres == null) // The second condition restricts the method to process only single substance drugs

        {
            //Console.WriteLine("This drug is given in concentration strength"); // Writeline only for error search

            StringBuilder sibUrl = new StringBuilder($"{baseUrl}{refsetBranch}concepts?ecl=((<< 763158003:411116001={cdProperties.GroupedCdProperties[0].ManDoseForm},1142139005=%23{cdProperties.GroupedCdProperties[0].CountIng},");

            if (cdProperties.GroupedCdProperties.ContainsKey(1))
            {
                sibUrl.Append("{");
                sibUrl.Append($"732943007={cdProperties.GroupedCdProperties[1].BOSS},1142138002=%23{cdProperties.GroupedCdProperties[1].ConcStrengthNumVal},733725009={cdProperties.GroupedCdProperties[1].ConcStrengthNumUnit},1142137007=%23{cdProperties.GroupedCdProperties[1].ConcStrengthDenVal},733722007={cdProperties.GroupedCdProperties[1].ConcStrengthDenUnit}");
                sibUrl.Append("}");
            }

            if (cdProperties.GroupedCdProperties.ContainsKey(2))
            {
                sibUrl.Append(",{");
                sibUrl.Append($"732943007={cdProperties.GroupedCdProperties[2].BOSS},1142138002=%23{cdProperties.GroupedCdProperties[2].ConcStrengthNumVal},733725009={cdProperties.GroupedCdProperties[2].ConcStrengthNumUnit},1142137007=%23{cdProperties.GroupedCdProperties[2].ConcStrengthDenVal},733722007={cdProperties.GroupedCdProperties[2].ConcStrengthDenUnit}");
                sibUrl.Append("}");
            }

            if (cdProperties.GroupedCdProperties.ContainsKey(3))
            {
                sibUrl.Append(",{");
                sibUrl.Append($"732943007={cdProperties.GroupedCdProperties[3].BOSS},1142138002=%23{cdProperties.GroupedCdProperties[3].ConcStrengthNumVal},733725009={cdProperties.GroupedCdProperties[3].ConcStrengthNumUnit},1142137007=%23{cdProperties.GroupedCdProperties[3].ConcStrengthDenVal},733722007={cdProperties.GroupedCdProperties[3].ConcStrengthDenUnit}");
                sibUrl.Append("}");
            }

            sibUrl.Append($") MINUS {vsoPostMaps.VsoSctId}) AND %5E{vsoPostId}");

            string sibUrlString = sibUrl.ToString();

            try
            {
                using HttpClient client = new();
                HttpResponseMessage sibResponse = await client.GetAsync(sibUrlString);

                if (sibResponse.IsSuccessStatusCode)
                {
                    string sibJson = await sibResponse.Content.ReadAsStringAsync();
                    var sibResult = JsonConvert.DeserializeObject<Siblings>(sibJson);

                    if (int.TryParse(sibResult.SibCount, out int sibCount) && sibCount > 0)
                    {
                        siblingResults.Add(new SiblingResult
                        {
                            LmmId = vsoPostMaps.LmmId,
                            SibSctId = vsoPostMaps.VsoSctId,
                            SibCount = sibResult.SibCount
                        }
                            );
                    }

                    Console.WriteLine($"Number of siblings for {vsoPostMaps.LmmId}: {sibResult.SibCount}");
                    string results = $"Siblings based on strength for |{vsoPostMaps.LmmId}|{sibResult.SibCount}|{sibUrlString}";
                    WriteErrorsToCsv(results, "C:\\temp\\siblingsStrength.csv");

                }
                else
                {
                    Console.WriteLine($"Error looking up siblings, lookup for concentration strength was unsuccessful for {sibUrlString}");
                    string errorMessage = $"Server returned unsuccessful trying to lookup siblings for |{vsoPostMaps.LmmId}|{vsoPostMaps.VsoSctId}|{sibUrlString}|{sibResponse.StatusCode}";
                    WriteErrorsToCsv(errorMessage, "C:\\temp\\errorsInSiblingEcl.csv");
                }

            }
            catch (Exception ex)
            {

                Console.WriteLine($"Exception: {ex.Message}");
            }
            Console.WriteLine(sibUrlString);
        }

        else
        {
            Console.WriteLine("The app is unsuccessful in creating a query for siblings");
        }

        // Return siblingResults as maps that need to be moved to a CD2
        return siblingResults;
    }

    // This method is used to find new map for the LmmId to Clinical Drug based on finding a Clinical Drug with identical BOSS and strength, and disregard PAI by setting PAI = BOSS
    static async Task<List<Cd2Result>> LookUpCd2(SiblingResult siblingResult, CdProperties cdProperties)
    {
        List<Cd2Result> cd2Results = new();

        // This section is created to find CD2s for LmmIds which is given with presentation strength and with only one substance

        if (cdProperties.GroupedCdProperties[0].UnitPres != null)

        {
            StringBuilder cd2Url = new StringBuilder($"{baseUrl}{cdBranch}concepts?ecl=(<< 763158003:411116001={cdProperties.GroupedCdProperties[0].ManDoseForm},763032000={cdProperties.GroupedCdProperties[0].UnitPres},1142139005=%23{cdProperties.GroupedCdProperties[0].CountIng},");

            if (cdProperties.GroupedCdProperties.ContainsKey(1))
            {
                cd2Url.Append("{");
                cd2Url.Append($"732943007={cdProperties.GroupedCdProperties[1].BOSS},762949000={cdProperties.GroupedCdProperties[1].BOSS},1142135004=%23{cdProperties.GroupedCdProperties[1].PresStrengthNumVal},732945000={cdProperties.GroupedCdProperties[1].PresStrengthNumUnit},1142136003=%23{cdProperties.GroupedCdProperties[1].PresStrengthDenVal},732947008={cdProperties.GroupedCdProperties[1].PresStrengthDenUnit}");
                cd2Url.Append("}");
            }

            if (cdProperties.GroupedCdProperties.ContainsKey(2))
            {
                cd2Url.Append("{");
                cd2Url.Append($"732943007={cdProperties.GroupedCdProperties[2].BOSS},762949000={cdProperties.GroupedCdProperties[2].BOSS},1142135004=%23{cdProperties.GroupedCdProperties[2].PresStrengthNumVal},732945000={cdProperties.GroupedCdProperties[2].PresStrengthNumUnit},1142136003=%23{cdProperties.GroupedCdProperties[2].PresStrengthDenVal},732947008={cdProperties.GroupedCdProperties[2].PresStrengthDenUnit}");
                cd2Url.Append("}");
            }

            if (cdProperties.GroupedCdProperties.ContainsKey(3))
            {
                cd2Url.Append("{");
                cd2Url.Append($"732943007={cdProperties.GroupedCdProperties[3].BOSS},762949000={cdProperties.GroupedCdProperties[3].BOSS},1142135004=%23{cdProperties.GroupedCdProperties[3].PresStrengthNumVal},732945000={cdProperties.GroupedCdProperties[3].PresStrengthNumUnit},1142136003=%23{cdProperties.GroupedCdProperties[3].PresStrengthDenVal},732947008={cdProperties.GroupedCdProperties[3].PresStrengthDenUnit}");
                cd2Url.Append("}");
            }

            cd2Url.Append($") MINUS {siblingResult.SibSctId}");

            string cd2UrlString = cd2Url.ToString();
           

            //Console.WriteLine(cd2url);
            try
            {
                using HttpClient client = new();
                HttpResponseMessage cd2Response = await client.GetAsync(cd2UrlString);

                if (cd2Response.IsSuccessStatusCode)
                {
                    string cd2Json = await cd2Response.Content.ReadAsStringAsync();
                    var cd2Map = JsonConvert.DeserializeObject<Cd2Map>(cd2Json);

                    if (cd2Map == null || cd2Map.Cd2Results == null || !cd2Map.Cd2Results.Any())
                    {
                        Console.WriteLine($"No LmmId or Cd2Sct pair found for {cd2UrlString}");
                        string errorMessage = $"No LmmId og Cd2Sct pair for for strength CD2 for |{siblingResult.LmmId}|{siblingResult.SibSctId}|{cd2UrlString}";
                        WriteErrorsToCsv(errorMessage, "c:\\temp\\errorsFindingCd2Strength.csv");
                    }
                    else
                    {
                        cd2Results.AddRange(cd2Map.Cd2Results
                            .Select(result => new Cd2Result
                            {
                                LmmId = siblingResult.LmmId,
                                Cd2Sct = result.Cd2Sct
                            }
                            ));


                        foreach (var cd2Result in cd2Results)
                        {
                            Console.WriteLine($"CD2 concept: {cd2Result.Cd2Sct}");

                        }
                    }
                }
                else
                    Console.WriteLine($"Ikke funnet CD2");
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Exception: {ex.Message}");
            }
        }

        // This section is a placeholder for drugs that are not yet possible to process.

        else if (cdProperties.GroupedCdProperties[0].UnitPres != null && cdProperties.GroupedCdProperties[0].CountIng == "4")
        {
            Console.WriteLine("The drug has 4 or more ingredients, and the app is not ready for that.");
        }

        // This section is created to find CD2s for LmmIds which is given with concentration strength and with only one substance

        else if (cdProperties.GroupedCdProperties[0].UnitPres == null)

        {
            StringBuilder cd2Url = new StringBuilder($"{baseUrl}{cdBranch}concepts?ecl=(<< 763158003:411116001={cdProperties.GroupedCdProperties[0].ManDoseForm},1142139005=%23{cdProperties.GroupedCdProperties[0].CountIng},");

            if (cdProperties.GroupedCdProperties.ContainsKey(1))
            {
                cd2Url.Append("{");
                cd2Url.Append($"732943007={cdProperties.GroupedCdProperties[1].BOSS},762949000={cdProperties.GroupedCdProperties[1].BOSS},1142138002=%23{cdProperties.GroupedCdProperties[1].ConcStrengthNumVal},733725009={cdProperties.GroupedCdProperties[1].ConcStrengthNumUnit},1142137007=%23{cdProperties.GroupedCdProperties[1].ConcStrengthDenVal},733722007={cdProperties.GroupedCdProperties[1].ConcStrengthDenUnit}");
                cd2Url.Append("}");
            }

            if (cdProperties.GroupedCdProperties.ContainsKey(2))
            {
                cd2Url.Append("{");
                cd2Url.Append($"732943007={cdProperties.GroupedCdProperties[2].BOSS},762949000={cdProperties.GroupedCdProperties[2].BOSS},1142138002=%23{cdProperties.GroupedCdProperties[2].ConcStrengthNumVal},733725009={cdProperties.GroupedCdProperties[2].ConcStrengthNumUnit},1142137007=%23{cdProperties.GroupedCdProperties[2].ConcStrengthDenVal},733722007={cdProperties.GroupedCdProperties[2].ConcStrengthDenUnit}");
                cd2Url.Append("}");
            }

            if (cdProperties.GroupedCdProperties.ContainsKey(3))
            {
                cd2Url.Append("{");
                cd2Url.Append($"732943007={cdProperties.GroupedCdProperties[3].BOSS},762949000={cdProperties.GroupedCdProperties[3].BOSS},1142138002=%23{cdProperties.GroupedCdProperties[3].ConcStrengthNumVal},733725009={cdProperties.GroupedCdProperties[3].ConcStrengthNumUnit},1142137007=%23{cdProperties.GroupedCdProperties[3].ConcStrengthDenVal},733722007={cdProperties.GroupedCdProperties[3].ConcStrengthDenUnit}");
                cd2Url.Append("}");
            }

            cd2Url.Append($") MINUS {siblingResult.SibSctId}");

            string cd2UrlString = cd2Url.ToString();



            //Console.WriteLine(cd2url);
            try
            {
                using HttpClient client = new();
                HttpResponseMessage cd2Response = await client.GetAsync(cd2UrlString);

                if (cd2Response.IsSuccessStatusCode)
                {
                    string cd2Json = await cd2Response.Content.ReadAsStringAsync();
                    var cd2Map = JsonConvert.DeserializeObject<Cd2Map>(cd2Json);

                    if (cd2Map == null || cd2Map.Cd2Results == null || !cd2Map.Cd2Results.Any())
                    {
                        Console.WriteLine($"No LmmId or Cd2Sct pair found for {cd2UrlString}");
                        string errorMessage = $"No LmmId og Cd2Sct pair for for strength CD2 for |{siblingResult.LmmId}|{siblingResult.SibSctId}|{cd2UrlString}";
                        WriteErrorsToCsv(errorMessage, "c:\\temp\\errorsFindingCd2Strength.csv");
                    }

                    else
                    {
                        cd2Results.AddRange(cd2Map.Cd2Results
                            .Select(result => new Cd2Result
                            {
                                LmmId = siblingResult.LmmId,
                                Cd2Sct = result.Cd2Sct
                            }

                            ));

                        foreach (var cd2Result in cd2Results)
                        {
                            Console.WriteLine($"CD2 concept: {cd2Result.Cd2Sct}");

                        }

                    }


                }
                else
                    Console.WriteLine($"Ikke funnet CD2");
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Exception: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("The app has an unspecified error searching for CD2 based on strength");
            string errorMessage = $"Unspecified error searching for strength CD2 for |{siblingResult.LmmId}|{siblingResult.SibSctId}";
            WriteErrorsToCsv(errorMessage, "c:\\temp\\unableCd2Strength.csv");
        }

        // Finally, new maps for LmmId to Clinical Drug for strength CD is returned

        return cd2Results;

    }

    // This is to create a new updated map where LmmIds which should be moved after strength check. A complete list needs to be compiled because all maps need to be checked for inject/infuse CD2
    static async Task<List<UpdatedCd>> CompileAfterStrengtMap(VsoPostMap map, List<Cd2Result> cd2Results)
    {
        List<UpdatedCd> updatedCds = new();

        // Check if LmmId exists in cd2Results
        var matchingCd2Result = cd2Results.Exists(result => result.LmmId == map.LmmId);

        // Set SctId based on the existence of LmmId in cd2Results
        string sctId = matchingCd2Result ? cd2Results.First(result => result.LmmId.Equals(map.LmmId)).Cd2Sct : map.VsoSctId;

        // Create UpdateCd object and add it to updatedCds
        updatedCds.Add(new UpdatedCd
        {
            SctId = sctId,
            LmmId = map.LmmId
        });

        return updatedCds;
    }

    // This is to lookup all concepts for properties

    static async Task<CdProperties> LookUpInjInfProps(UpdatedCd updatedCds)
    {
        CdProperties injInfCdProperties = new();

        string injInfPropUrl = $"{baseUrl}{refsetBranch}relationships?source={updatedCds.SctId}&active=true";
        //Console.WriteLine(injInfPropUrl);

        try
        {
            using HttpClient injInfPropClient = new();
            HttpResponseMessage injInfPropResponse = await injInfPropClient.GetAsync(injInfPropUrl);


            if (injInfPropResponse.IsSuccessStatusCode)
            {
                string additionalJson = await injInfPropResponse.Content.ReadAsStringAsync();
                injInfCdProperties = JsonConvert.DeserializeObject<CdProperties>(additionalJson);

                var relationships = injInfCdProperties.Relationships;

                foreach (var relationship in relationships)
                {
                    if (!injInfCdProperties.GroupedCdProperties.ContainsKey(relationship.GroupId))
                        injInfCdProperties.GroupedCdProperties[relationship.GroupId] = new CdProperties.GroupProperties();

                    relationship.AssignCdProperties(injInfCdProperties.GroupedCdProperties[relationship.GroupId]);
                }

            }


        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
        }
        return injInfCdProperties;

    }
    // Second step is to lookup dose form properties

    static async Task<DoseFormProps> LookUpDoseFormProps(CdProperties injInfCdProperties)
    {
        DoseFormProps doseFormProps = new();

        string doseFormUrl = $"{baseUrl}{refsetBranch}relationships?source={injInfCdProperties.GroupedCdProperties[0].ManDoseForm}&active=true";

        //Console.WriteLine(doseFormUrl);

        try
        {
            using HttpClient doseFormClient = new();
            HttpResponseMessage doseFormResponse = await doseFormClient.GetAsync(doseFormUrl);

            if (doseFormResponse.IsSuccessStatusCode)
            {
                string doseFormJson = await doseFormResponse.Content.ReadAsStringAsync();
                doseFormProps = JsonConvert.DeserializeObject<DoseFormProps>(doseFormJson);

                if (doseFormProps != null)
                {
                    foreach (var relationship in doseFormProps.Relationships)
                    {
                        doseFormProps.SetProperty(relationship.TypeId, relationship.DestinationId, relationship.ConcreteValue);
                    }
                }

                Console.WriteLine($"Administration method for {injInfCdProperties.GroupedCdProperties[0].ManDoseForm}: {doseFormProps.Administration}");
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
        }
        return doseFormProps;
    }
    // Final step is to lookup any siblings based on inject or infuse

    static async Task<List<SiblingResult>> LookUpInjInjSiblings(UpdatedCd updatedCds, CdProperties injInfCdProperties, DoseFormProps doseFormProps)
    {
        List<SiblingResult> injInfSiblingResults = new();

        if (doseFormProps.Administration == "740685003" || doseFormProps.Administration == "764794000" || doseFormProps.Administration == "1007331000202106")
        {

            if (injInfCdProperties.GroupedCdProperties[0].UnitPres != null) // URL for presentation strength

            {
                StringBuilder injInfSibUrl = new StringBuilder($"{baseUrl}{refsetBranch}concepts?ecl=((<<763158003:411116001=(<736542009:736472000=(740685003 OR 764794000 OR 1007331000202106),736474004={doseFormProps.IntendedSite},736476002={doseFormProps.BasicDoseForm},736473005={doseFormProps.Transformation},736475003={doseFormProps.ReleaseCharacteristics}),763032000={injInfCdProperties.GroupedCdProperties[0].UnitPres},1142139005=%23{injInfCdProperties.GroupedCdProperties[0].CountIng},");
                
                if (injInfCdProperties.GroupedCdProperties.ContainsKey(1))
                {
                    injInfSibUrl.Append("{");
                    injInfSibUrl.Append($"732943007={injInfCdProperties.GroupedCdProperties[1].BOSS},762949000={injInfCdProperties.GroupedCdProperties[1].PAI},1142135004=%23{injInfCdProperties.GroupedCdProperties[1].PresStrengthNumVal},732945000={injInfCdProperties.GroupedCdProperties[1].PresStrengthNumUnit},1142136003=%23{injInfCdProperties.GroupedCdProperties[1].PresStrengthDenVal},732947008={injInfCdProperties.GroupedCdProperties[1].PresStrengthDenUnit}");
                    injInfSibUrl.Append("}");
                }

                if (injInfCdProperties.GroupedCdProperties.ContainsKey(2))
                {
                    injInfSibUrl.Append(",{");
                    injInfSibUrl.Append($"732943007={injInfCdProperties.GroupedCdProperties[2].BOSS},762949000={injInfCdProperties.GroupedCdProperties[2].PAI},1142135004=%23{injInfCdProperties.GroupedCdProperties[2].PresStrengthNumVal},732945000={injInfCdProperties.GroupedCdProperties[2].PresStrengthNumUnit},1142136003=%23{injInfCdProperties.GroupedCdProperties[2].PresStrengthDenVal},732947008={injInfCdProperties.GroupedCdProperties[2].PresStrengthDenUnit}");
                    injInfSibUrl.Append("}");
                }

                if (injInfCdProperties.GroupedCdProperties.ContainsKey(3))
                {
                    injInfSibUrl.Append(",{");
                    injInfSibUrl.Append($"732943007={injInfCdProperties.GroupedCdProperties[3].BOSS},762949000={injInfCdProperties.GroupedCdProperties[3].PAI},1142135004=%23{injInfCdProperties.GroupedCdProperties[3].PresStrengthNumVal},732945000={injInfCdProperties.GroupedCdProperties[3].PresStrengthNumUnit},1142136003=%23{injInfCdProperties.GroupedCdProperties[3].PresStrengthDenVal},732947008={injInfCdProperties.GroupedCdProperties[3].PresStrengthDenUnit}");
                    injInfSibUrl.Append("}");
                }

                injInfSibUrl.Append($") MINUS {updatedCds.SctId}) AND %5E{vsoPostId}");

                string injInfSibUrlString = injInfSibUrl.ToString();

                //Console.WriteLine(injInfSiblingUrl);

                try
                {
                    using HttpClient infInfSiblingclient = new();
                    HttpResponseMessage injInfSibResponse = await infInfSiblingclient.GetAsync(injInfSibUrlString);

                    if (injInfSibResponse.IsSuccessStatusCode)
                    {
                        string injInfSibJson = await injInfSibResponse.Content.ReadAsStringAsync();
                        var injInfSibResult = JsonConvert.DeserializeObject<Siblings>(injInfSibJson);

                        if (int.TryParse(injInfSibResult.SibCount, out int sibCount) && sibCount > 0)
                        {
                            injInfSiblingResults.Add(new SiblingResult
                            {
                                LmmId = updatedCds.LmmId,
                                SibSctId = updatedCds.SctId,
                                SibCount = injInfSibResult.SibCount
                            }
                                );
                        }

                        Console.WriteLine($"Number of siblings based on inj/inf for {updatedCds.SctId}: {injInfSibResult.SibCount}");
                        string resultsinj = $"Number of siblings based on inj/inf for |{updatedCds.SctId}|{injInfSibResult.SibCount}|{injInfSibUrlString}";
                        WriteErrorsToCsv(resultsinj, "C:\\temp\\siblingsForInjInf.csv");


                        if (sibCount > 0)
                        {
                            Console.WriteLine($"This ECL gave at least one sibling: {injInfSibResult.SibCount}");
                        }

                    }
                    else
                        Console.WriteLine($"Error lookup siblings for {injInfSibUrlString}");
                }
                catch (Exception ex)
                {

                    Console.WriteLine($"Exception: {ex.Message}");
                }


            }

            else if (injInfCdProperties.GroupedCdProperties[0].UnitPres == null) // URL for concentration strength
            {
                StringBuilder injInfSibUrl = new StringBuilder($"{baseUrl}{refsetBranch}concepts?ecl=((<<763158003:411116001=(<736542009:736472000=(740685003 OR 764794000 OR 1007331000202106),736474004={doseFormProps.IntendedSite},736476002={doseFormProps.BasicDoseForm},736473005={doseFormProps.Transformation},736475003={doseFormProps.ReleaseCharacteristics}),1142139005=%23{injInfCdProperties.GroupedCdProperties[0].CountIng},");

                if (injInfCdProperties.GroupedCdProperties.ContainsKey(1))
                {
                    injInfSibUrl.Append("{");
                    injInfSibUrl.Append($"732943007={injInfCdProperties.GroupedCdProperties[1].BOSS},762949000={injInfCdProperties.GroupedCdProperties[1].PAI},1142138002=%23{injInfCdProperties.GroupedCdProperties[1].ConcStrengthNumVal},733725009={injInfCdProperties.GroupedCdProperties[1].ConcStrengthNumUnit},1142137007=%23{injInfCdProperties.GroupedCdProperties[1].ConcStrengthDenVal},733722007={injInfCdProperties.GroupedCdProperties[1].ConcStrengthDenUnit}");
                    injInfSibUrl.Append("}");
                }

                if (injInfCdProperties.GroupedCdProperties.ContainsKey(2))
                {
                    injInfSibUrl.Append(",{");
                    injInfSibUrl.Append($"732943007={injInfCdProperties.GroupedCdProperties[2].BOSS},762949000={injInfCdProperties.GroupedCdProperties[2].PAI},1142138002=%23{injInfCdProperties.GroupedCdProperties[2].ConcStrengthNumVal},733725009={injInfCdProperties.GroupedCdProperties[2].ConcStrengthNumUnit},1142137007=%23{injInfCdProperties.GroupedCdProperties[2].ConcStrengthDenVal},733722007={injInfCdProperties.GroupedCdProperties[2].ConcStrengthDenUnit}");
                    injInfSibUrl.Append("}");
                }

                if (injInfCdProperties.GroupedCdProperties.ContainsKey(3))
                {
                    injInfSibUrl.Append(",{");
                    injInfSibUrl.Append($"732943007={injInfCdProperties.GroupedCdProperties[3].BOSS},762949000={injInfCdProperties.GroupedCdProperties[3].PAI},1142138002=%23{injInfCdProperties.GroupedCdProperties[3].ConcStrengthNumVal},733725009={injInfCdProperties.GroupedCdProperties[3].ConcStrengthNumUnit},1142137007=%23{injInfCdProperties.GroupedCdProperties[3].ConcStrengthDenVal},733722007={injInfCdProperties.GroupedCdProperties[3].ConcStrengthDenUnit}");
                    injInfSibUrl.Append("}");
                }

                injInfSibUrl.Append($") MINUS {updatedCds.SctId}) AND %5E{vsoPostId}");

                string injInfSibUrlString = injInfSibUrl.ToString();


                //Console.WriteLine(injInfSiblingUrl);


                try
                {
                    using HttpClient infInfSiblingclient = new();
                    HttpResponseMessage injInfSibResponse = await infInfSiblingclient.GetAsync(injInfSibUrlString);

                    if (injInfSibResponse.IsSuccessStatusCode)
                    {
                        string injInfSibJson = await injInfSibResponse.Content.ReadAsStringAsync();
                        var injInfSibResult = JsonConvert.DeserializeObject<Siblings>(injInfSibJson);

                        if (int.TryParse(injInfSibResult.SibCount, out int sibCount) && sibCount > 0)
                        {
                            injInfSiblingResults.Add(new SiblingResult
                            {
                                LmmId = updatedCds.LmmId,
                                SibSctId = updatedCds.SctId,
                                SibCount = injInfSibResult.SibCount
                            }
                                );
                        }

                        Console.WriteLine($"Number of siblings for {updatedCds.SctId}: {injInfSibResult.SibCount}");
                        string results = $"Number of siblings based on inj/inf for |{updatedCds.SctId}|{injInfSibResult.SibCount}|{injInfSibUrlString}";
                        WriteErrorsToCsv(results, "C:\\temp\\siblingsForInjInf.csv");

                        if (sibCount > 0)
                        {
                            Console.WriteLine($"This ECL gave at least one sibling: {injInfSibResult.SibCount}");
                        }
                    }
                    else
                        Console.WriteLine($"Error lookup siblings for {injInfSibUrlString}");
                }
                catch (Exception ex)
                {

                    Console.WriteLine($"Exception: {ex.Message}");
                }

            }
        }
        return injInfSiblingResults;
    }
    // This section is to look-up correct CD2 for inject/infuse

    static async Task<List<Cd2Result>> LookUpInjInfCd2(UpdatedCd updatedCd, CdProperties injInfCdProperties, DoseFormProps doseFormProps, SiblingResult injInfSiblingResults)

    {
        List<Cd2Result> injInfCd2Results = new();

        Console.WriteLine("Just checking if this is activated at all");

        if (injInfCdProperties.GroupedCdProperties[0].UnitPres != null && injInfSiblingResults.SibCount != "0")
        {
            StringBuilder injInfCd2Url = new StringBuilder($"{baseUrl}{refsetBranch}concepts?ecl=(<<763158003:411116001=(<736542009:736472000=(740685003 AND 764794000),736474004={doseFormProps.IntendedSite},736476002={doseFormProps.BasicDoseForm},736473005={doseFormProps.Transformation},736475003={doseFormProps.ReleaseCharacteristics}),763032000={injInfCdProperties.GroupedCdProperties[0].UnitPres},1142139005=%23{injInfCdProperties.GroupedCdProperties[0].CountIng},");

            if (injInfCdProperties.GroupedCdProperties.ContainsKey(1))
            {
                injInfCd2Url.Append("{");
                injInfCd2Url.Append($"732943007={injInfCdProperties.GroupedCdProperties[1].BOSS},762949000={injInfCdProperties.GroupedCdProperties[1].PAI},1142135004=%23{injInfCdProperties.GroupedCdProperties[1].PresStrengthNumVal},732945000={injInfCdProperties.GroupedCdProperties[1].PresStrengthNumUnit},1142136003=%23{injInfCdProperties.GroupedCdProperties[1].PresStrengthDenVal},732947008={injInfCdProperties.GroupedCdProperties[1].PresStrengthDenUnit}");
                injInfCd2Url.Append("}");
            }

            if (injInfCdProperties.GroupedCdProperties.ContainsKey(2))
            {
                injInfCd2Url.Append(",{");
                injInfCd2Url.Append($"732943007={injInfCdProperties.GroupedCdProperties[2].BOSS},762949000={injInfCdProperties.GroupedCdProperties[2].PAI},1142135004=%23{injInfCdProperties.GroupedCdProperties[2].PresStrengthNumVal},732945000={injInfCdProperties.GroupedCdProperties[2].PresStrengthNumUnit},1142136003=%23{injInfCdProperties.GroupedCdProperties[2].PresStrengthDenVal},732947008={injInfCdProperties.GroupedCdProperties[2].PresStrengthDenUnit}");
                injInfCd2Url.Append("}");
            }

            if (injInfCdProperties.GroupedCdProperties.ContainsKey(3))
            {
                injInfCd2Url.Append(",{");
                injInfCd2Url.Append($"732943007={injInfCdProperties.GroupedCdProperties[3].BOSS},762949000={injInfCdProperties.GroupedCdProperties[3].PAI},1142135004=%23{injInfCdProperties.GroupedCdProperties[3].PresStrengthNumVal},732945000={injInfCdProperties.GroupedCdProperties[3].PresStrengthNumUnit},1142136003=%23{injInfCdProperties.GroupedCdProperties[3].PresStrengthDenVal},732947008={injInfCdProperties.GroupedCdProperties[3].PresStrengthDenUnit}");
                injInfCd2Url.Append("}");
            }

            injInfCd2Url.Append($") MINUS {updatedCd.SctId}");

            string injInfCd2UrlString = injInfCd2Url.ToString();


            //Console.WriteLine(injInfCd2Url);

            try
            {
                using HttpClient injInfCd2Client = new();
                HttpResponseMessage injInfCd2Response = await injInfCd2Client.GetAsync(injInfCd2UrlString);

                Console.WriteLine(injInfCd2Response.StatusCode);

                if (injInfCd2Response.IsSuccessStatusCode)
                {
                    string injInfCd2Json = await injInfCd2Response.Content.ReadAsStringAsync();
                    var injInfCd2Map = JsonConvert.DeserializeObject<Cd2Map>(injInfCd2Json);

                    if (injInfCd2Map != null && injInfCd2Map.Cd2Results != null && !injInfCd2Map.Cd2Results.Any())
                    {
                        Console.WriteLine($"No LmmId or Cd2Sct pair found for {injInfCd2UrlString}");
                        string errorMessage = $"No LmmId or Cd2Sct pair found for|{injInfCd2UrlString}|{updatedCd.LmmId}|{updatedCd.SctId}";
                        WriteErrorsToCsv(errorMessage, "c:\\temp\\injInfCd2Errors.csv");
                    }

                    else if (injInfCd2Map != null && injInfCd2Map.Cd2Results != null)
                    {
                        injInfCd2Results.AddRange(injInfCd2Map.Cd2Results.Select(result => new Cd2Result
                        {
                            LmmId = updatedCd.LmmId,
                            Cd2Sct = result.Cd2Sct
                        }
                            ));
                    }

                    else
                    {
                        Console.WriteLine($"The app was unable to find a CD2 for {injInfCd2UrlString}");
                    }
                }
                else
                {
                    Console.WriteLine($"The app received an unsuccessful code trying to find a CD2 for {injInfCd2UrlString}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }

        else if (injInfCdProperties.GroupedCdProperties[0].UnitPres == null && injInfCdProperties.GroupedCdProperties[0].CountIng == "1")
        {
            StringBuilder injInfCd2Url = new StringBuilder($"{baseUrl}{refsetBranch}concepts?ecl=(<<763158003:411116001=(<736542009:736472000=(740685003 AND 764794000),736474004={doseFormProps.IntendedSite},736476002={doseFormProps.BasicDoseForm},736473005={doseFormProps.Transformation},736475003={doseFormProps.ReleaseCharacteristics}),1142139005=%23{injInfCdProperties.GroupedCdProperties[0].CountIng},");

            if (injInfCdProperties.GroupedCdProperties.ContainsKey(1))
            {
                injInfCd2Url.Append("{");
                injInfCd2Url.Append($"732943007={injInfCdProperties.GroupedCdProperties[1].BOSS},762949000={injInfCdProperties.GroupedCdProperties[1].PAI},1142138002=%23{injInfCdProperties.GroupedCdProperties[1].ConcStrengthNumVal},733725009={injInfCdProperties.GroupedCdProperties[1].ConcStrengthNumUnit},1142137007=%23{injInfCdProperties.GroupedCdProperties[1].ConcStrengthDenVal},733722007={injInfCdProperties.GroupedCdProperties[1].ConcStrengthDenUnit}");
                injInfCd2Url.Append("}");
            }

            if (injInfCdProperties.GroupedCdProperties.ContainsKey(2))
            {
                injInfCd2Url.Append(",{");
                injInfCd2Url.Append($"732943007={injInfCdProperties.GroupedCdProperties[2].BOSS},762949000={injInfCdProperties.GroupedCdProperties[2].PAI},1142138002=%23{injInfCdProperties.GroupedCdProperties[2].ConcStrengthNumVal},733725009={injInfCdProperties.GroupedCdProperties[2].ConcStrengthNumUnit},1142137007=%23{injInfCdProperties.GroupedCdProperties[2].ConcStrengthDenVal},733722007={injInfCdProperties.GroupedCdProperties[2].ConcStrengthDenUnit}");
                injInfCd2Url.Append("}");
            }

            if (injInfCdProperties.GroupedCdProperties.ContainsKey(3))
            {
                injInfCd2Url.Append(",{");
                injInfCd2Url.Append($"732943007={injInfCdProperties.GroupedCdProperties[3].BOSS},762949000={injInfCdProperties.GroupedCdProperties[3].PAI},1142138002=%23{injInfCdProperties.GroupedCdProperties[3].ConcStrengthNumVal},733725009={injInfCdProperties.GroupedCdProperties[3].ConcStrengthNumUnit},1142137007=%23{injInfCdProperties.GroupedCdProperties[3].ConcStrengthDenVal},733722007={injInfCdProperties.GroupedCdProperties[3].ConcStrengthDenUnit}");
                injInfCd2Url.Append("}");
            }

            injInfCd2Url.Append($") MINUS {updatedCd.SctId}");

            string injInfCd2UrlString = injInfCd2Url.ToString();

            //Console.WriteLine(injInfCd2Url);

            try
            {
                using HttpClient injInfCd2Client = new();
                HttpResponseMessage injInfCd2Response = await injInfCd2Client.GetAsync(injInfCd2UrlString);
                Console.WriteLine(injInfCd2Response.StatusCode);

                if (injInfCd2Response.IsSuccessStatusCode)
                {
                    string injInfCd2Json = await injInfCd2Response.Content.ReadAsStringAsync();
                    var injInfCd2Map = JsonConvert.DeserializeObject<Cd2Map>(injInfCd2Json);

                    if (injInfCd2Map != null && injInfCd2Map.Cd2Results != null && !injInfCd2Map.Cd2Results.Any())
                    {
                        Console.WriteLine($"No LmmId or Cd2Sct pair found for {injInfCd2UrlString}");
                        string errorMessage = $"No LmmId or Cd2Sct pair found for|{injInfCd2UrlString}|{updatedCd.LmmId}|{updatedCd.SctId}";
                        WriteErrorsToCsv(errorMessage, "c:\\temp\\injInfCd2Errors.csv");

                    }

                    else if (injInfCd2Map != null && injInfCd2Map.Cd2Results != null)
                    {
                        injInfCd2Results.AddRange(injInfCd2Map.Cd2Results.Select(result => new Cd2Result
                        {
                            LmmId = updatedCd.LmmId,
                            Cd2Sct = result.Cd2Sct
                        }
                            ));
                    }

                    else
                    {
                        Console.WriteLine($"The app was unable to find a CD2 for {injInfCd2UrlString}");
                    }

                }
                else
                {
                    Console.WriteLine($"The app received an unsuccessful code trying to find a CD2 for {injInfCd2UrlString}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }

        Console.WriteLine($"Just testing if anything is written {injInfCd2Results.Count}");
        return injInfCd2Results;

    }


    static async Task<List<FinalCd2>> CompileFinalCd2Map(UpdatedCd updatedCds, List<Cd2Result> injInfCd2Results)
    {
        List<FinalCd2> finalCd2s = new();

        // Check if LmmId exists in injInfCd2Results
        var matchingFinalCd2Result = injInfCd2Results.Exists(result => result.LmmId == updatedCds.LmmId);

        // Set SctId based on the existence of LmmId in cd2Results
        string sctId = matchingFinalCd2Result ? injInfCd2Results.First(result => result.LmmId.Equals(updatedCds.LmmId)).Cd2Sct : updatedCds.SctId;

        // Create UpdateCd object and add it to updatedCds
        finalCd2s.Add(new FinalCd2
        {
            LmmId = updatedCds.LmmId,
            SctId = sctId
        });

        return finalCd2s;
    }
}