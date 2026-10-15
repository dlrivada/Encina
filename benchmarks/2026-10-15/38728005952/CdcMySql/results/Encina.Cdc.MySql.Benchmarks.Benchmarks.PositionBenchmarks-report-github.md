```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.976 ns |  0.0341 ns | 0.0019 ns |   0.46 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.625 ns |  3.7947 ns | 0.2080 ns |   1.00 |    0.03 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   764.073 ns | 95.8144 ns | 5.2519 ns |  88.62 |    1.94 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,166.598 ns | 29.2843 ns | 1.6052 ns | 135.31 |    2.86 | 0.0286 |     504 B |       12.60 |
