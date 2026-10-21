```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.976 ns |  0.0520 ns | 0.0029 ns |   0.41 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     9.647 ns |  2.3860 ns | 0.1308 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   792.603 ns | 51.3487 ns | 2.8146 ns |  82.17 |    1.00 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,198.566 ns | 41.7961 ns | 2.2910 ns | 124.26 |    1.48 | 0.0286 |     504 B |       12.60 |
