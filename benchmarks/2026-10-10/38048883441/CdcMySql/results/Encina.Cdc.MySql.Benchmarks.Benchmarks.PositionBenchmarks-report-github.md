```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.970 ns |   0.0529 ns | 0.0029 ns |   0.51 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     7.855 ns |   2.5565 ns | 0.1401 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   783.318 ns | 169.0146 ns | 9.2643 ns |  99.74 |    1.84 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,164.498 ns | 126.9474 ns | 6.9584 ns | 148.28 |    2.39 | 0.0286 |     504 B |       12.60 |
