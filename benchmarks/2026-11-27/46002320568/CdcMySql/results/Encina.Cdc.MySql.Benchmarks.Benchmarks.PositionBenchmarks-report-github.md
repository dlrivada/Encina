```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.17GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     4.001 ns |   0.5292 ns | 0.0290 ns |   0.51 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     7.886 ns |   2.0936 ns | 0.1148 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   769.615 ns |  71.6911 ns | 3.9296 ns |  97.60 |    1.31 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,140.923 ns | 103.8772 ns | 5.6939 ns | 144.69 |    1.93 | 0.0286 |     504 B |       12.60 |
