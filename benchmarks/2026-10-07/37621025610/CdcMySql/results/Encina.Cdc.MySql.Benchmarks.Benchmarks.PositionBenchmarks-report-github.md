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
| CompareFilePositions |     3.975 ns |  0.0263 ns | 0.0014 ns |   0.47 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.389 ns |  3.2318 ns | 0.1771 ns |   1.00 |    0.03 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   780.178 ns | 48.2325 ns | 2.6438 ns |  93.03 |    1.72 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,149.919 ns | 17.7230 ns | 0.9715 ns | 137.12 |    2.51 | 0.0286 |     504 B |       12.60 |
