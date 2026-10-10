```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error         | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|--------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.975 ns |     0.0431 ns |  0.0024 ns |   0.43 |    0.02 |      - |         - |        0.00 |
| CreateGtidPosition   |     9.151 ns |     7.1878 ns |  0.3940 ns |   1.00 |    0.05 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   777.593 ns |    63.6827 ns |  3.4907 ns |  85.08 |    3.27 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,197.180 ns | 1,351.0408 ns | 74.0551 ns | 131.00 |    8.62 | 0.0286 |     504 B |       12.60 |
