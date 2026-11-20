namespace MyLab.OpenSearch.HealthCheck;

enum NodeTouchResult
{
    Undefined,
    Ok,
    ConnectionError,
    RightsNotEnough,
    AnotherProblem
}