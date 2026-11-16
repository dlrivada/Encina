```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.47GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   1.588 ns |  0.3709 ns | 0.0203 ns |   0.34 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |   4.698 ns |  0.9609 ns | 0.0527 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 409.882 ns | 10.8610 ns | 0.5953 ns |  87.26 |    0.86 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 610.104 ns | 72.1807 ns | 3.9565 ns | 129.89 |    1.46 | 0.0296 |     504 B |       12.60 |
