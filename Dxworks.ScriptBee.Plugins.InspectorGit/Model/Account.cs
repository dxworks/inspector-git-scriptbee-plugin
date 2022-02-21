using System.Collections.Generic;

namespace Dxworks.ScriptBee.Plugins.InspectorGit.Model;

public class Account : ReferenceEntity
{
    public AccountId AccountId { get; set; }
    public List<Commit> Commits { get; set; }
    public Repository Repository { get; set; }

    protected bool Equals(Account other)
    {
        return Equals(AccountId, other.AccountId);
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Account) obj);
    }

    public override int GetHashCode()
    {
        return (AccountId != null ? AccountId.GetHashCode() : 0);
    }
}