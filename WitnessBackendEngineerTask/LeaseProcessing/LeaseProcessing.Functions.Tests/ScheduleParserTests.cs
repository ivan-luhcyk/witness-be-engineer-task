using LeaseProcessing.Functions.Models;
using LeaseProcessing.Functions.Services;
using Xunit;

namespace LeaseProcessing.Functions.Tests;

public sealed class ScheduleParserTests
{
    private IScheduleParser CreateSut() => new ScheduleParser();

    [Fact]
    public void Parse_Entry1_MultiLinePlanRef_GoesToRegistration_NotPropertyOrTerm()
    {
        var sut = CreateSut();

        var raw = new RawScheduleNoticeOfLease
        {
            EntryNumber = "1",
            EntryType = "Schedule of Notices of Leases",
            EntryText =
            [
                "09.07.2009      Endeavour House, 47 Cuba      06.07.2009      EGL557357  ",
                "Edged and       Street, London                125 years from             ",
                "numbered 2 in                                 1.1.2009                   ",
                "blue (part of)"
            ]
        };

        var parsed = sut.Parse([raw])[0];

        Assert.Equal(1, parsed.EntryNumber);
        Assert.Null(parsed.EntryDate);
        Assert.Equal("09.07.2009 Edged and numbered 2 in blue (part of)", parsed.RegistrationDateAndPlanRef);
        Assert.Equal("Endeavour House, 47 Cuba Street, London", parsed.PropertyDescription);
        Assert.Equal("06.07.2009 125 years from 1.1.2009", parsed.DateOfLeaseAndTerm);
        Assert.Equal("EGL557357", parsed.LesseesTitle);
        Assert.Empty(parsed.Notes);
    }

    [Fact]
    public void Parse_Entry4_NotesAndPlanRef_AreParsedCorrectly()
    {
        var sut = CreateSut();

        var raw = new RawScheduleNoticeOfLease
        {
            EntryNumber = "4",
            EntryType = "Schedule of Notices of Leases",
            EntryText =
            [
                "24.07.1989      17 Ashworth Close (Ground     01.06.1989      TGL24029   ",
                "Edged and       and First Floor Flat)         125 years from             ",
                "numbered 19                                   1.6.1989                   ",
                "(Part of) in                                                             ",
                "brown                                                                    ",
                "NOTE 1: A Deed of Rectification dated 7 September 1992 made between (1) Orbit Housing Association and (2) John Joseph McMahon Nellie Helen McMahon and John George McMahon is supplemental to the Lease dated 1 June 1989.",
                "NOTE 2: By a Deed dated 23 May 1996 made between (1) Orbit Housing Association (2) John Joseph McMahon Nellie Helen McMahon and John George McMahon and (3) Britannia Building Society the terms of the lease were varied.",
                "NOTE 3: A Deed dated 13 February 1997 made between (1) Orbit Housing Association (2) John Joseph McMahon and others and (3) Britannia Building Society is supplemental to the lease."
            ]
        };

        var parsed = sut.Parse([raw])[0];

        Assert.Equal("24.07.1989 Edged and numbered 19 (Part of) in brown", parsed.RegistrationDateAndPlanRef);
        Assert.Equal("17 Ashworth Close (Ground and First Floor Flat)", parsed.PropertyDescription);
        Assert.Equal("01.06.1989 125 years from 1.6.1989", parsed.DateOfLeaseAndTerm);
        Assert.Equal("TGL24029", parsed.LesseesTitle);
        Assert.Equal(3, parsed.Notes.Count);
        Assert.StartsWith("NOTE 1:", parsed.Notes[0]);
        Assert.StartsWith("NOTE 2:", parsed.Notes[1]);
        Assert.StartsWith("NOTE 3:", parsed.Notes[2]);
    }

    [Fact]
    public void Parse_Entry2_PlanRefMustNotLeakIntoProperty()
    {
        var sut = CreateSut();

        var raw = new RawScheduleNoticeOfLease
        {
            EntryNumber = "2",
            EntryType = "Schedule of Notices of Leases",
            EntryText =
            [
                "15.11.2018      Ground Floor Premises         10.10.2018      TGL513556  ",
                "Edged and                                     from 10                    ",
                "numbered 2 in                                 October 2018               ",
                "blue (part of)                                to and                     ",
                "including 19               ",
                "April 2028"
            ]
        };

        var parsed = sut.Parse([raw])[0];

        Assert.Equal("15.11.2018 Edged and numbered 2 in blue (part of)", parsed.RegistrationDateAndPlanRef);
        Assert.Equal("Ground Floor Premises", parsed.PropertyDescription);
        Assert.Equal("10.10.2018 from 10 October 2018 to and including 19 April 2028", parsed.DateOfLeaseAndTerm);
        Assert.Equal("TGL513556", parsed.LesseesTitle);
        Assert.Empty(parsed.Notes);
    }

    [Fact]
    public void Parse_Entry3_WrappedTermLines_AreJoinedIntoDateOfLeaseAndTerm()
    {
        var sut = CreateSut();

        var raw = new RawScheduleNoticeOfLease
        {
            EntryNumber = "3",
            EntryType = "Schedule of Notices of Leases",
            EntryText =
            [
                "16.08.2013      21 Sheen Road (Ground floor   06.08.2013      TGL383606  ",
                "shop)                         Beginning on               ",
                "and including              ",
                "6.8.2013 and               ",
                "ending on and              ",
                "including                  ",
                "6.8.2023"
            ]
        };

        var parsed = sut.Parse([raw])[0];

        Assert.Equal("16.08.2013", parsed.RegistrationDateAndPlanRef);
        Assert.Equal("21 Sheen Road (Ground floor shop)", parsed.PropertyDescription);
        Assert.Equal("06.08.2013 Beginning on and including 6.8.2013 and ending on and including 6.8.2023", parsed.DateOfLeaseAndTerm);
        Assert.Equal("TGL383606", parsed.LesseesTitle);
        Assert.Empty(parsed.Notes);
    }

    [Fact]
    public void Parse_Entry5_PlanRefNumbered25PartOfInBrown_MustStayInRegistration()
    {
        var sut = CreateSut();

        var raw = new RawScheduleNoticeOfLease
        {
            EntryNumber = "5",
            EntryType = "Schedule of Notices of Leases",
            EntryText =
            [
                "19.09.1989      12 Harbord Close (Ground      01.09.1989      TGL27196   ",
                "Edged and       and First Floor Flat)         125 years from             ",
                "numbered 25                                   1.9.1989                   ",
                "(Part of) in                                                             ",
                "brown                                                                    ",
                "NOTE: By a Deed dated 20 July 1995 made between (1) Orbit Housing Association and (2) Clifford Ronald Mitchell the terms of the Lease were varied.  (Copy Deed filed under TGL27169)"
            ]
        };

        var parsed = sut.Parse([raw])[0];

        Assert.Equal("19.09.1989 Edged and numbered 25 (Part of) in brown", parsed.RegistrationDateAndPlanRef);
        Assert.Equal("12 Harbord Close (Ground and First Floor Flat)", parsed.PropertyDescription);
        Assert.Equal("01.09.1989 125 years from 1.9.1989", parsed.DateOfLeaseAndTerm);
        Assert.Equal("TGL27196", parsed.LesseesTitle);
        Assert.Single(parsed.Notes);
        Assert.StartsWith("NOTE:", parsed.Notes[0]);
    }

    [Fact]
    public void Parse_MultipleEntries_OrderIsPreserved()
    {
        var sut = CreateSut();

        var raws = new List<RawScheduleNoticeOfLease>
        {
            new()
            {
                EntryNumber = "2",
                EntryText =
                [
                    "15.11.2018      Ground Floor Premises         10.10.2018      TGL513556  ",
                    "Edged and                                     from 10                    ",
                    "numbered 2 in                                 October 2018               ",
                    "blue (part of)                                to and                     ",
                    "including 19               ",
                    "April 2028"
                ]
            },
            new()
            {
                EntryNumber = "1",
                EntryText =
                [
                    "09.07.2009      Endeavour House, 47 Cuba      06.07.2009      EGL557357  ",
                    "Edged and       Street, London                125 years from             ",
                    "numbered 2 in                                 1.1.2009                   ",
                    "blue (part of)"
                ]
            }
        };

        var parsed = sut.Parse(raws);

        Assert.Equal(2, parsed[0].EntryNumber);
        Assert.Equal(1, parsed[1].EntryNumber);
        Assert.Equal("TGL513556", parsed[0].LesseesTitle);
        Assert.Equal("EGL557357", parsed[1].LesseesTitle);
    }

    [Fact]
    public void Parse_TitleFallback_IfNotInLastColumn_StillExtractedFromAnyLine()
    {
        var sut = CreateSut();

        var raw = new RawScheduleNoticeOfLease
        {
            EntryNumber = "99",
            EntryText =
            [
                "01.01.2001      Some Property                02.02.2002      ",
                "Random text TGL999999 scattered"
            ]
        };

        var parsed = sut.Parse([raw])[0];
        Assert.Equal("TGL999999", parsed.LesseesTitle);
    }

    [Fact]
    public void Parse_EntryDate_IsParsedWhenValid()
    {
        var sut = CreateSut();

        var raw = new RawScheduleNoticeOfLease
        {
            EntryNumber = "7",
            EntryDate = "24.07.1989",
            EntryText =
            [
                "24.07.1989      Some Property                  01.01.1990      TGL12345"
            ]
        };

        var parsed = sut.Parse([raw])[0];
        Assert.Equal(new DateOnly(1989, 7, 24), parsed.EntryDate);
    }

    [Fact]
    public void Parse_EntryDate_InvalidValue_ReturnsNull()
    {
        var sut = CreateSut();

        var raw = new RawScheduleNoticeOfLease
        {
            EntryNumber = "8",
            EntryDate = "invalid",
            EntryText =
            [
                "24.07.1989      Some Property                  01.01.1990      TGL12345"
            ]
        };

        var parsed = sut.Parse([raw])[0];
        Assert.Null(parsed.EntryDate);
    }
}
