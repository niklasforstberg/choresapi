namespace ChoresApi.Models.DTOs.Household;

public record CreateHouseholdDto(string Name);

public record JoinHouseholdDto(string InviteCode);

public record HouseholdMemberDto(int Id, string Name);

public record HouseholdDto(int Id, string Name, string InviteCode, List<HouseholdMemberDto> Members);
