namespace Fluid.Avalonia.Demo.Models;

/// <summary>
/// Sample address rows, taken once from Code for America's Ohana API sample data
/// (https://github.com/codeforamerica/ohana-api, data/sample-csv/addresses.csv) and embedded so the
/// demo has no runtime dependency. Used to fill the DataGrid example.
/// </summary>
public static class SampleAddresses
{
    public record Address(int Id, string Street, string Suite, string City, string State, string Zip);

    public static readonly Address[] All =
    {
        new(1,  "2600 Middlefield Road", "",          "Redwood City",         "CA", "94063"),
        new(2,  "24 Second Avenue",      "",          "San Mateo",            "CA", "94401"),
        new(3,  "24 Second Avenue",      "",          "San Mateo",            "CA", "94403"),
        new(4,  "24 Second Avenue",      "",          "San Mateo",            "CA", "94401"),
        new(5,  "24 Second Avenue",      "",          "San Mateo",            "CA", "94401"),
        new(6,  "800 Middle Avenue",     "",          "Menlo Park",           "CA", "94025-9881"),
        new(7,  "500 Arbor Road",        "",          "Menlo Park",           "CA", "94025"),
        new(8,  "800 Middle Avenue",     "",          "Menlo Park",           "CA", "94025-9881"),
        new(9,  "2510 Middlefield Road", "",          "Redwood City",         "CA", "94063"),
        new(10, "1044 Middlefield Road", "",          "Redwood City",         "CA", "94063"),
        new(11, "2140 Euclid Avenue",    "",          "Redwood City",         "CA", "94061"),
        new(12, "1044 Middlefield Road", "2nd Floor", "Redwood City",         "CA", "94063"),
        new(13, "399 Marine Parkway",    "",          "Redwood City",         "CA", "94065"),
        new(14, "660 Veterans Blvd",     "",          "Redwood City",         "CA", "94063"),
        new(15, "1500 Valencia Street",  "",          "San Francisco",        "CA", "94110"),
        new(16, "1161 South Bernardo",   "",          "Sunnyvale",            "CA", "94087"),
        new(17, "409 South Spruce Avenue", "",        "South San Francisco",  "CA", "94080"),
        new(18, "114 Fifth Avenue",      "",          "Redwood City",         "CA", "94063"),
        new(19, "19 West 39th Avenue",   "",          "San Mateo",            "CA", "94403"),
        new(20, "123 El Camino Real",    "",          "Belmont",              "CA", "94002"),
        new(21, "2013 Avenue of the Fellows", "Suite 100", "San Francisco",   "CA", "94103"),
    };
}
