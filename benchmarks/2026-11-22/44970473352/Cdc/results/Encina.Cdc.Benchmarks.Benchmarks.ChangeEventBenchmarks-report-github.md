```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                     | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 23.862 ns | 0.4627 ns | 0.6925 ns |  0.72 |    0.02 | 0.0016 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.463 ns | 0.0039 ns | 0.0053 ns |  0.04 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression |  9.755 ns | 0.4836 ns | 0.6935 ns |  0.29 |    0.02 | 0.0007 |      56 B |        1.00 |
| CreateChangeMetadata       | 33.173 ns | 0.3866 ns | 0.5786 ns |  1.00 |    0.02 | 0.0007 |      56 B |        1.00 |
