```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.655 ns |   0.0291 ns | 0.0016 ns |   0.39 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |   6.738 ns |   0.2266 ns | 0.0124 ns |   1.00 |    0.00 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 568.057 ns | 139.2716 ns | 7.6339 ns |  84.30 |    0.99 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 844.761 ns | 100.1685 ns | 5.4906 ns | 125.37 |    0.73 | 0.0296 |     504 B |       12.60 |
