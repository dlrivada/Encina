```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                  | Mean     | Error   | StdDev  | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------ |---------:|--------:|--------:|------:|--------:|----------:|------------:|
| GetCurrentPositionAsync | 429.6 μs | 5.63 μs | 8.43 μs |  1.00 |    0.03 |  15.64 KB |        1.00 |
