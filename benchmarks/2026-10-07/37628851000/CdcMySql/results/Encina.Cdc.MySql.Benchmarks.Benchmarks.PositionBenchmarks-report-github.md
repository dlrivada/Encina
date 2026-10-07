```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean       | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |   2.689 ns |   0.4879 ns |  0.0267 ns |   0.37 |    0.02 |      - |         - |        0.00 |
| CreateGtidPosition   |   7.351 ns |   6.2711 ns |  0.3437 ns |   1.00 |    0.06 | 0.0024 |      40 B |        1.00 |
| FromBytes            | 577.258 ns |   9.0977 ns |  0.4987 ns |  78.64 |    3.15 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 858.870 ns | 323.9991 ns | 17.7595 ns | 117.00 |    5.14 | 0.0296 |     504 B |       12.60 |
