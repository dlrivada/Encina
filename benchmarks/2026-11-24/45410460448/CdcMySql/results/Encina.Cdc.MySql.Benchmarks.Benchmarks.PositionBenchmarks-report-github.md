```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.17GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   1.681 ns |   0.3174 ns | 0.0174 ns |   0.35 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   4.741 ns |   1.2435 ns | 0.0682 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 466.479 ns | 114.7818 ns | 6.2916 ns |  98.40 |    1.67 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 673.396 ns |  87.6946 ns | 4.8068 ns | 142.05 |    1.96 | 0.0296 |     504 B |       12.60 |
