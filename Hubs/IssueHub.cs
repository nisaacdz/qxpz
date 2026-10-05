using Microsoft.AspNetCore.SignalR;
using CampFix.Models;

namespace CampFix.Hubs;

public class IssueHub : Hub
{
    // Clients can join specific building or category groups if desired
    public async Task JoinBuildingGroup(string buildingName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, buildingName);
    }

    public async Task LeaveBuildingGroup(string buildingName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, buildingName);
    }
}
