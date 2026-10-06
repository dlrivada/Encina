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
| CompareFilePositions |     3.972 ns |  0.0294 ns | 0.0016 ns |   0.48 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.217 ns |  3.6789 ns | 0.2017 ns |   1.00 |    0.03 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   773.172 ns | 78.7228 ns | 4.3151 ns |  94.13 |    2.03 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,162.668 ns | 61.5880 ns | 3.3758 ns | 141.55 |    3.00 | 0.0286 |     504 B |       12.60 |
