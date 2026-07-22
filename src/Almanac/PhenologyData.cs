namespace Almanac;

/// <summary>
/// Curated seasonal notes keyed by half-month, written for the temperate
/// Northern Hemisphere. Southern-hemisphere installs get no note (v1).
/// </summary>
public static class PhenologyData
{
    private static readonly Dictionary<string, string> Notes = new()
    {
        ["01a"] = "Deep winter quiet. Citrus and hardy greens are in season.",
        ["01b"] = "The days are noticeably lengthening. Listen for owls calling at dusk.",
        ["02a"] = "Sap begins to rise in the maples. Forced bulbs brighten windowsills.",
        ["02b"] = "The first snowdrops and crocuses push through. Root vegetables hold the table.",
        ["03a"] = "Red-winged blackbirds return to the marshes. Spring greens are starting.",
        ["03b"] = "Spring peepers begin calling. Asparagus season opens.",
        ["04a"] = "Trees leaf out and the first butterflies are on the wing. Rhubarb and radishes are in.",
        ["04b"] = "Lilacs and apple blossoms open. Morels appear in the woods.",
        ["05a"] = "Warblers move through in waves. Strawberries begin in warm spots.",
        ["05b"] = "Peonies and irises bloom. Fresh peas and lettuce are in season.",
        ["06a"] = "Fledglings everywhere and roses at their peak. Strawberries are at their best.",
        ["06b"] = "The longest days of the year. Cherries and early blueberries arrive.",
        ["07a"] = "Fireflies at dusk. Blueberries and sweet cherries are in season.",
        ["07b"] = "Cicadas sing in the heat. Tomatoes and sweet corn are coming in.",
        ["08a"] = "Goldenrod begins to bloom. Peaches and melons are at their peak.",
        ["08b"] = "Swallows gather on the wires. Tomatoes, corn, and peppers overflow.",
        ["09a"] = "First hints of color in the maples. Apples and grapes begin.",
        ["09b"] = "Monarchs drift south. Apple season is in full swing.",
        ["10a"] = "Fall color nears its peak. Pumpkins and winter squash arrive.",
        ["10b"] = "Geese move south in long skeins. Late apples and cider pressing.",
        ["11a"] = "The last leaves fall and frost comes most mornings. Brussels sprouts sweeten.",
        ["11b"] = "The woods go quiet and open. Cranberries and hardy kale are in season.",
        ["12a"] = "Dusk comes early; juncos gather at the feeder. Citrus season begins.",
        ["12b"] = "The shortest days — and the light begins its return. Pomegranates brighten the table.",
    };

    public static string? NoteFor(DateOnly date, bool isNorthern)
    {
        if (!isNorthern) return null;
        var key = $"{date.Month:D2}{(date.Day <= 15 ? "a" : "b")}";
        return Notes.GetValueOrDefault(key);
    }
}
