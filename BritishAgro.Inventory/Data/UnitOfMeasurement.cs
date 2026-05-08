using System.ComponentModel.DataAnnotations;

namespace BritishAgro.Inventory.Data;

public enum UnitOfMeasurement
{
    [Display(Name = "Piece")]
    Piece = 1,

    [Display(Name = "Unit")]
    Unit = 2,

    [Display(Name = "Box")]
    Box = 3,

    [Display(Name = "Packet")]
    Packet = 4,

    [Display(Name = "Set")]
    Set = 5,

    [Display(Name = "Pair")]
    Pair = 6,

    [Display(Name = "Dozen")]
    Dozen = 7,

    [Display(Name = "Gross")]
    Gross = 8,

    [Display(Name = "Kilogram")]
    Kilogram = 9,

    [Display(Name = "Gram")]
    Gram = 10,

    [Display(Name = "Milligram")]
    Milligram = 11,

    [Display(Name = "Quintal")]
    Quintal = 12,

    [Display(Name = "Tonne")]
    Tonne = 13,

    [Display(Name = "Liter")]
    Liter = 14,

    [Display(Name = "Milliliter")]
    Milliliter = 15,

    [Display(Name = "Gallon")]
    Gallon = 16,

    [Display(Name = "Pint")]
    Pint = 17,

    [Display(Name = "Ounce")]
    Ounce = 18,

    [Display(Name = "Pound")]
    Pound = 19,

    [Display(Name = "Meter")]
    Meter = 20,

    [Display(Name = "Centimeter")]
    Centimeter = 21,

    [Display(Name = "Millimeter")]
    Millimeter = 22,

    [Display(Name = "Inch")]
    Inch = 23,

    [Display(Name = "Foot")]
    Foot = 24,

    [Display(Name = "Yard")]
    Yard = 25,

    [Display(Name = "Square Meter")]
    SquareMeter = 26,

    [Display(Name = "Square Foot")]
    SquareFoot = 27,

    [Display(Name = "Cubic Meter")]
    CubicMeter = 28,

    [Display(Name = "Cubic Foot")]
    CubicFoot = 29,

    [Display(Name = "Tablet")]
    Tablet = 30,

    [Display(Name = "Strip")]
    Strip = 31,

    [Display(Name = "Bottle")]
    Bottle = 32,

    [Display(Name = "Can")]
    Can = 33,

    [Display(Name = "Roll")]
    Roll = 34,

    [Display(Name = "Sheet")]
    Sheet = 35,

    [Display(Name = "Bundle")]
    Bundle = 36,

    [Display(Name = "Carton")]
    Carton = 37,

    [Display(Name = "Sachet")]
    Sachet = 38,

    [Display(Name = "Barrel")]
    Barrel = 39
}
