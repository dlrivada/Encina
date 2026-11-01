```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                  | Mean     | Error   | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------------ |---------:|--------:|--------:|------:|----------:|------------:|
| GetCurrentPositionAsync | 278.7 μs | 1.19 μs | 1.70 μs |  1.00 |   2.29 KB |        1.00 |
