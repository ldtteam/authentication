namespace LDTTeam.Authentication.PatreonApiUtils.Model.Requests;

public class PatreonResponseBase
{
    public required List<IncludedItem> Included { get; set; }
    public Meta? Meta { get; set; }
}

public class PatreonResponseBase<T> : PatreonResponseBase
{
    public required T Data { get; set; }
}

public class CampaignMembersResponse : PatreonResponseBase<List<Member>>;

public class CampaignMemberResponse : PatreonResponseBase<Member>;

public class Member
{
    public required string Id { get; set; }
    public required string Type { get; set; }
    public required MemberAttributes Attributes { get; set; }
    public required MemberRelationships Relationships { get; set; }
}

public class MemberAttributes
{
    public required string PatronStatus { get; set; }
    public int CampaignLifetimeSupportCents { get; set; }
    public int CurrentlyEntitledAmountCents { get; set; }
    public string? LastChargeDate { get; set; }
    public string? LastChargeStatus { get; set; }
    public int WillPayAmountCents { get; set; }
    public bool IsGifted { get; set; }
}

public class MemberRelationships
{
    public required CurrentlyEntitledTiers CurrentlyEntitledTiers { get; set; }
    public UserRelationship? User { get; set; }
}

public class CurrentlyEntitledTiers
{
    public required List<IncludedDataReference> Data { get; set; }
}

public class IncludedDataReference : IEquatable<IncludedDataReference>
{
    public required string Id { get; set; }
    public required string Type { get; set; }

    public bool Equals(IncludedDataReference? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id && Type == other.Type;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((IncludedDataReference)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, Type);
    }

    public static bool operator ==(IncludedDataReference? left, IncludedDataReference? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(IncludedDataReference? left, IncludedDataReference? right)
    {
        return !Equals(left, right);
    }
}

public class UserRelationship
{
    public UserData? Data { get; set; }
}

public class UserData
{
    public string? Id { get; set; }
}


public class IncludedItem
{
    public required string Id { get; set; }
    public required string Type { get; set; }
    public required Dictionary<string, object> Attributes { get; set; }
}

public class Meta
{
    public required Pagination Pagination { get; set; }
}

public class Pagination
{
    public int Total { get; set; }
    public required Cursors? Cursors { get; set; }
}

public class Cursors
{
    public required string? Next { get; set; }
}
