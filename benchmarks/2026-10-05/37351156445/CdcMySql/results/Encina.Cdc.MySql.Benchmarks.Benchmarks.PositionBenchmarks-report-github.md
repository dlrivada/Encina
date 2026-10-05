```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error      | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|-----------:|----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.977 ns |  0.0520 ns | 0.0028 ns |   0.45 |    0.00 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.740 ns |  0.5526 ns | 0.0303 ns |   1.00 |    0.00 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   794.423 ns | 55.9224 ns | 3.0653 ns |  90.89 |    0.41 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,200.373 ns | 37.5549 ns | 2.0585 ns | 137.34 |    0.46 | 0.0286 |     504 B |       12.60 |
