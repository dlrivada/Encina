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
| CompareFilePositions |     3.974 ns |  0.0377 ns | 0.0021 ns |   0.48 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.321 ns |  4.6855 ns | 0.2568 ns |   1.00 |    0.04 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   830.999 ns | 82.5535 ns | 4.5250 ns |  99.93 |    2.68 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,164.239 ns | 42.6415 ns | 2.3373 ns | 140.01 |    3.70 | 0.0286 |     504 B |       12.60 |
