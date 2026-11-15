```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method               | Mean         | Error     | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.996 ns | 0.0195 ns | 0.0291 ns |   0.46 |    0.02 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.698 ns | 0.2215 ns | 0.3315 ns |   1.00 |    0.05 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   780.069 ns | 5.0010 ns | 7.4853 ns |  89.80 |    3.44 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,220.303 ns | 5.1845 ns | 7.7599 ns | 140.49 |    5.29 | 0.0286 |     504 B |       12.60 |
