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
| CompareFilePositions |     3.978 ns |   0.2044 ns |  0.0112 ns |   0.50 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     7.948 ns |   2.0654 ns |  0.1132 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   789.449 ns | 293.7906 ns | 16.1037 ns |  99.35 |    2.14 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,153.719 ns |  83.0892 ns |  4.5544 ns | 145.19 |    1.87 | 0.0286 |     504 B |       12.60 |
