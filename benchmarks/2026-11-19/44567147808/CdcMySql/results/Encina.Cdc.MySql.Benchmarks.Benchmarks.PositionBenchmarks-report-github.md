```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error     | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     4.024 ns |  1.596 ns | 0.0875 ns |   0.50 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.045 ns |  1.773 ns | 0.0972 ns |   1.00 |    0.01 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   776.854 ns | 24.070 ns | 1.3193 ns |  96.57 |    1.02 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,173.734 ns | 56.490 ns | 3.0964 ns | 145.91 |    1.56 | 0.0286 |     504 B |       12.60 |
