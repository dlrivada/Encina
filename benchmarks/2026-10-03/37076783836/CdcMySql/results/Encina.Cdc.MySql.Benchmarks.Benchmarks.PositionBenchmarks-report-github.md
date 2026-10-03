```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.972 ns |   0.1132 ns |  0.0062 ns |   0.46 |    0.02 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.682 ns |   6.5038 ns |  0.3565 ns |   1.00 |    0.05 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   798.449 ns | 140.1637 ns |  7.6828 ns |  92.07 |    3.29 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,178.675 ns | 245.1889 ns | 13.4396 ns | 135.91 |    4.91 | 0.0286 |     504 B |       12.60 |
