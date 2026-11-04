```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.27GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   1.785 ns |   1.536 ns |  0.0842 ns |   0.36 |    0.02 |      - |         - |        0.00 |
| CreateGtidPosition   |   4.984 ns |   3.190 ns |  0.1748 ns |   1.00 |    0.04 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 431.433 ns |  14.573 ns |  0.7988 ns |  86.63 |    2.59 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 718.432 ns | 387.098 ns | 21.2182 ns | 144.25 |    5.68 | 0.0296 |     504 B |       12.60 |
