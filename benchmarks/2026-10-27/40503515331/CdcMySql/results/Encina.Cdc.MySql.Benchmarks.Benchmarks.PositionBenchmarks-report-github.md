```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method               | Mean         | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| CompareFilePositions |     3.973 ns |   0.0497 ns |  0.0027 ns |   0.46 |    0.01 |      - |         - |        0.00 |
| CreateGtidPosition   |     8.565 ns |   2.9106 ns |  0.1595 ns |   1.00 |    0.02 | 0.0024 |      40 B |        1.00 |
| FromBytes            |   770.733 ns | 243.9433 ns | 13.3714 ns |  90.00 |    2.00 | 0.0410 |     688 B |       17.20 |
| ToBytes              | 1,143.305 ns |  50.0453 ns |  2.7431 ns | 133.51 |    2.19 | 0.0286 |     504 B |       12.60 |
