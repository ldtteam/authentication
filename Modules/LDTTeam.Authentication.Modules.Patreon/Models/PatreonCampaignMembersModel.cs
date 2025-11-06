using System.Collections.Generic;

namespace LDTTeam.Authentication.Modules.Patreon.Models;

public class PatreonRoot
{
    public List<Member> Data { get; set; }
    public List<IncludedItem> Included { get; set; }
    public Links Links { get; set; }
    public Meta Meta { get; set; }
}

public class Member
{
    public string Id { get; set; }
    public string Type { get; set; }
    public MemberAttributes Attributes { get; set; }
    public MemberRelationships Relationships { get; set; }
}

public class MemberAttributes
{
    public string PatronStatus { get; set; }
    public int CampaignLifetimeSupportCents { get; set; }
    public int CurrentlyEntitledAmountCents { get; set; }
    public string LastChargeDate { get; set; }
    public string LastChargeStatus { get; set; }
    public int WillPayAmountCents { get; set; }
    public bool IsGifted { get; set; }
}

public class MemberRelationships
{
    public CurrentlyEntitledTiers CurrentlyEntitledTiers { get; set; }
    public UserRelationship User { get; set; }
}

public class CurrentlyEntitledTiers
{
    public List<IncludedDataReference> Data { get; set; }
}

public class IncludedDataReference
{
    public string Id { get; set; }
    public string Type { get; set; }
}

public class UserRelationship
{
    public UserData Data { get; set; }
    public UserLinks Links { get; set; }
}

public class UserData
{
    public string Id { get; set; }
    public string Type { get; set; }
}

public class UserLinks
{
    public string Related { get; set; }
}

public class IncludedItem
{
    public string Id { get; set; }
    public string Type { get; set; }
    public Dictionary<string, object> Attributes { get; set; }
}

public class Links
{
    public string Next { get; set; }
}

public class Meta
{
    public Pagination Pagination { get; set; }
}

public class Pagination
{
    public int Total { get; set; }
    public Cursors Cursors { get; set; }
}

public class Cursors
{
    public string Next { get; set; }
}
