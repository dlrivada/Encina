```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.11GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.984 ns |   0.3585 ns | 0.0196 ns |   0.49 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.198 ns |   3.4444 ns | 0.1888 ns |   1.00 |    0.03 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   754.877 ns |  38.3609 ns | 2.1027 ns |  92.11 |    1.88 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,146.254 ns | 125.1288 ns | 6.8587 ns | 139.87 |    2.92 | 0.0286 |     504 B |       12.60 |
