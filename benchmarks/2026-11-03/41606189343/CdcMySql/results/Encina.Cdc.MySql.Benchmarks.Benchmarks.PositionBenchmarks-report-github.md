```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.20GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   1.671 ns |  0.4829 ns | 0.0265 ns |   0.34 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |   4.892 ns |  1.1729 ns | 0.0643 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 429.789 ns | 23.7389 ns | 1.3012 ns |  87.86 |    1.02 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 656.336 ns | 71.3067 ns | 3.9086 ns | 134.17 |    1.67 | 0.0296 |     504 B |       12.60 |
